// Hookfall - shared enemy logic: health, rewards, steering (pursuit, separation, island avoidance), think rate,
// pooling, hit flash, elites and the fallbacks in GDD 5.7.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 for steering, think LOD, pooling and elites, and given catch-up speed and the school statuses
// (burn, chill, freeze, shock, bleed) on 2026-11-08; hooks for the stage boss, the boss zone that keeps the horde
// below the boss arena and the stage reward scale were added the same day; on 2026-11-09 the horde was made to keep
// up with a fast Warden, and a flyer lost behind islands or left behind comes back ahead of them. Prompts and changes
// are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.Progression;
using UnityEngine;

namespace Hookfall.Enemies
{
    public abstract class EnemyBase : MonoBehaviour
    {
        /// <summary>Every living enemy. Items and steering read this instead of physics queries.</summary>
        public static readonly List<EnemyBase> Active = new List<EnemyBase>();

        [Header("Base stats (scaled by the EnemyDirector over time)")]
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float contactDamage = 5f;
        [SerializeField] private float xpReward = 1f;
        [SerializeField] private int goldReward = 1;
        [Tooltip("Distance from the centre at which the enemy counts as touched by weapons.")]
        [SerializeField] private float hitRadius = 0.6f;

        [Header("Steering (GDD 5.1)")]
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float separationRadius = 1.6f;
        [SerializeField] private float separationWeight = 1.2f;
        [SerializeField] private float avoidLookAhead = 0.5f;
        [SerializeField] private float avoidWeight = 2.5f;

        [Header("Catch-up (2026-11-08: a flying Warden left every bat behind)")]
        [Tooltip("When chasing, the enemy flies at least this share of the player's speed. 0.8 until 2026-11-09: at full " +
                 "speed the horde trailed 30 m behind, out of view and out of reach of short-range items.")]
        [SerializeField] private float playerSpeedMatch = 1f;
        [Tooltip("Extra speed for every metre beyond catchUpDistance, so stragglers close in (0.4 until 2026-11-09).")]
        [SerializeField] private float catchUpPerMetre = 0.8f;
        [Tooltip("15 m until 2026-11-09. A horde matching the Warden's speed settles this far away: 2.5 m is inside the " +
                 "Orbit Blades' ring (3.9 m) but outside a bite (1.5 m), so flying fast hits them without being bitten.")]
        [SerializeField] private float catchUpDistance = 2.5f;

        [Header("Thinking (GDD 5.2)")]
        [SerializeField] private float nearDistance = 60f;
        [SerializeField] private float nearThinkInterval = 0.1f;
        [SerializeField] private float farThinkInterval = 0.5f;

        [Header("Fallbacks (GDD 5.7)")]
        [SerializeField] private float despawnDistance = 120f;
        [SerializeField] private float stuckCheckInterval = 3f;
        [SerializeField] private float stuckMinDistance = 1f;
        [Tooltip("A flyer that has not come within engageDistance of the Warden for this many seconds is lost (behind " +
                 "an island, left behind) and comes back ahead of them (2026-11-09: on the doubled map lost flyers filled the cap).")]
        [SerializeField] private float lostAfter = 10f;
        [SerializeField] private float engageDistance = 30f;

        [Header("Visuals")]
        [Tooltip("Shader colour property used for the hit flash, elite glow and attack warning (toon Shader Graph).")]
        [SerializeField] private string glowProperty = "_EmissionColor";
        [SerializeField] private float flashDuration = 0.08f;
        [Tooltip("Hit flash colour; the boss uses a dim one, or its many hits would strobe.")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Transform wingLeft;
        [SerializeField] private Transform wingRight;
        [SerializeField] private float flapSpeed = 14f;
        [SerializeField] private float flapAngle = 45f;

        [Header("Game feel")]
        [Tooltip("Speed added away from the player on every hit (golems ignore it).")]
        [SerializeField] private float knockback = 6f;
        [Tooltip("The body swells by this much on a hit and settles back.")]
        [SerializeField] private float hitPunch = 0.3f;

        [Header("Motion (2026-11-10: flyers bank into turns, bob, pop in and pop out)")]
        [Tooltip("The most a flyer rolls into a turn, in degrees.")]
        [SerializeField] private float bankAngle = 30f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 3.2f;
        [Tooltip("Seconds a fresh enemy takes to grow to its size.")]
        [SerializeField] private float spawnTime = 0.25f;
        [Tooltip("Seconds a killed enemy takes to burst and vanish before it returns to the pool.")]
        [SerializeField] private float deathTime = 0.16f;

        [Header("Elite traits (2026-11-10)")]
        [Tooltip("Shielded: a barrier worth this share of the elite's health takes the first blows.")]
        [SerializeField] private float shieldShare = 0.6f;
        [Tooltip("Swift: speed multiplier.")]
        [SerializeField] private float swiftFactor = 1.45f;

        private readonly StateMachine machine = new StateMachine();
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private int glowId;
        private Color baseGlow = Color.black;
        private bool warning;
        private float flashTimer;
        private float healthMultiplier = 1f, damageMultiplier = 1f, rewardMultiplier = 1f;
        private float thinkTimer, thinkElapsed;
        private float stuckTimer, lostTimer;
        private int stuckStrikes;
        private Vector3 lastCheckedPosition;
        private Vector3 desiredVelocity;
        private Vector3 baseScale;
        private float flapPhase;
        private bool built;
        private Transform visual;
        private Quaternion visualRotation = Quaternion.identity;
        private Vector3 visualPosition;
        private float punch, bank, pitch, jab, spawnTimer, dying;
        private float flapClock, wingSpread = 1f, wingLift;
        // School statuses (SynergySystem applies them): timers in seconds, damage per second.
        private float burnTimer, burnDps, bleedTimer, bleedDps, chillTimer, chillSlow, shockTimer, freezeTimer, dotTimer;
        private Hookfall.Combat.SynergySystem synergy;
        private Color statusGlow = Color.black;
        private Color unshieldedGlow = Color.black;

        public string DisplayName => displayName;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth * healthMultiplier;
        public bool IsDead { get; private set; }
        public bool IsElite { get; private set; }
        /// <summary>What sets this elite apart (None for the plain horde).</summary>
        public EliteAffix Affix { get; private set; }
        /// <summary>What is left of a Shielded elite's barrier; hits wear it down before they reach its health.</summary>
        public float Shield { get; private set; }
        /// <summary>The speed multiplier of the enemy's trait (a Swift elite), 1 otherwise.</summary>
        public float SpeedTrait => Affix == EliteAffix.Swift ? swiftFactor : 1f;
        public float MoveSpeed => moveSpeed * SpeedTrait;
        /// <summary>How much bigger than its prefab the enemy is (elites) or smaller (a jelly's young).</summary>
        public float SizeFactor => transform.localScale.x / Mathf.Max(0.01f, baseScale.x);
        public bool Burning => burnTimer > 0f;
        public bool Bleeding => bleedTimer > 0f;
        public bool Frozen => freezeTimer > 0f;
        public bool Chilled => chillTimer > 0f || Frozen;
        public bool Shocked => shockTimer > 0f;
        public float BurnDps => burnDps;
        private bool AnyStatus => Burning || Bleeding || Chilled || Shocked;
        private float SpeedFactor => Frozen ? 0f : chillTimer > 0f ? 1f - chillSlow : 1f;
        public float Damage => contactDamage * damageMultiplier;
        /// <summary>Time and elite scaling for damage that is not contact damage (boulders, slams).</summary>
        public float DamageScale => damageMultiplier;
        public float HitRadius => hitRadius * SizeFactor;
        public Vector3 Velocity { get; private set; }
        public string CurrentStateName => machine.Current?.Name ?? "None";
        public EnemyBase SourcePrefab { get; set; }
        /// <summary>The stage boss: mass kills (Heartstone Nova, the tower cache) do not destroy it.</summary>
        public virtual bool IsBoss => false;
        /// <summary>Whether it may fly up into the boss arena; the plain horde stays below it (StageDirector).</summary>
        public virtual bool CanEnterBossZone => IsElite;
        /// <summary>Takes no damage (the boss during its entrance).</summary>
        protected virtual bool Invulnerable => false;
        protected virtual bool ImmuneToFreeze => false;
        /// <summary>Never moved by the fallbacks (too far, stuck, below the abyss): the boss stays where it rose.</summary>
        protected virtual bool Anchored => false;
        /// <summary>Flyers bank and bob; the ones that stand (golems, the boss) do not.</summary>
        protected virtual bool Flies => true;
        /// <summary>Seconds of the death pop; 0 for an enemy that stages its own death (the boss).</summary>
        protected virtual float DeathTime => deathTime;
        /// <summary>The model under the enemy, for subclasses that pose it.</summary>
        protected Transform Visual => visual;

        public EnemyDirector Director { get; private set; }
        public PlayerController Player { get; private set; }
        public PlayerStats PlayerStats { get; private set; }

        protected virtual void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            glowId = Shader.PropertyToID(glowProperty);
            baseScale = transform.localScale;
            visual = transform.Find("Visual");
            if (visual != null)
            {
                visualRotation = visual.localRotation;   // the model's own import pose: motion is layered on it
                visualPosition = visual.localPosition;
            }
            flapPhase = Random.value * 10f;
        }

        /// <summary>Called by the EnemyDirector every time the enemy is taken from the pool.</summary>
        public void Init(EnemyDirector director, PlayerController player, float healthMult, float damageMult)
        {
            Director = director;
            Player = player;
            PlayerStats = player.GetComponent<PlayerStats>();
            synergy = player.GetComponent<Hookfall.Combat.SynergySystem>();
            burnTimer = bleedTimer = chillTimer = shockTimer = freezeTimer = dotTimer = 0f;
            statusGlow = Color.black;

            healthMultiplier = healthMult;
            damageMultiplier = damageMult;
            rewardMultiplier = 1f;
            IsElite = false;
            Affix = EliteAffix.None;
            Shield = 0f;
            IsDead = false;
            transform.localScale = baseScale;
            Health = MaxHealth;
            Velocity = desiredVelocity = Vector3.zero;
            ResetStuck();
            thinkTimer = Random.value * nearThinkInterval;   // spread thinking over frames
            baseGlow = Color.black;
            warning = false;
            flashTimer = 0f;
            punch = bank = pitch = jab = dying = 0f;
            spawnTimer = spawnTime;
            if (visual != null)
            {
                visual.localScale = spawnTime > 0f ? Vector3.one * 0.2f : Vector3.one;
                visual.localRotation = visualRotation;
                visual.localPosition = visualPosition;
            }
            ApplyGlow();

            if (!built)
            {
                BuildStates();
                built = true;
            }
            machine.Reset();
            machine.ChangeState(DefaultState);
            OnSpawned();
            Active.Add(this);
        }

        /// <summary>Elite version: bigger, tougher, more reward, glowing (GDD 4.8, 7.3).</summary>
        /// <param name="plainGlow">The glow a Shielded elite falls back to once its barrier is broken.</param>
        public void MakeElite(float healthMult, float damageMult, float scale, float rewardMult, Color glow, EliteAffix affix = EliteAffix.None,
            Color plainGlow = default)
        {
            IsElite = true;
            Affix = affix;
            unshieldedGlow = plainGlow == default ? glow : plainGlow;
            healthMultiplier *= healthMult;
            damageMultiplier *= damageMult;
            rewardMultiplier = rewardMult;
            Health = MaxHealth;
            Shield = affix == EliteAffix.Shielded ? MaxHealth * shieldShare : 0f;
            transform.localScale = baseScale * scale;
            baseGlow = glow;
            ApplyGlow();
        }

        /// <summary>A smaller, weaker copy (what a split leaves, a jelly's young): scaled health, size and rewards.</summary>
        public void MakeLesser(float healthMult, float scale, float rewardMult)
        {
            healthMultiplier *= healthMult;
            rewardMultiplier = rewardMult;
            Health = MaxHealth;
            transform.localScale = baseScale * scale;
        }

        protected abstract void BuildStates();
        protected abstract IEnemyState DefaultState { get; }
        protected virtual void OnSpawned() { }
        /// <summary>Runs every frame, for things that cannot wait for the next think (a fast dash).</summary>
        protected virtual void FrameUpdate(float deltaTime) { }

        public void ChangeState(IEnemyState next) => machine.ChangeState(next);

        protected virtual void Update()
        {
            if (dying > 0f) { TickDeath(Time.deltaTime); return; }
            if (IsDead || Player == null) return;
            float dt = Time.deltaTime;

            thinkTimer -= dt;
            thinkElapsed += dt;
            if (thinkTimer <= 0f)
            {
                bool near = (transform.position - Player.transform.position).sqrMagnitude < nearDistance * nearDistance;
                thinkTimer = near ? nearThinkInterval : farThinkInterval;
                float elapsed = thinkElapsed;
                thinkElapsed = 0f;

                // Fallback: an invalid state resets to the default one.
                if (machine.Current == null) machine.ChangeState(DefaultState);
                machine.Tick(elapsed);
                if (IsDead) return;
                RunFallbacks(elapsed);
                if (IsDead) return;
            }

            if (AnyStatus) TickStatuses(dt);
            if (IsDead) return;

            Velocity = Vector3.MoveTowards(Velocity, desiredVelocity, acceleration * dt);
            transform.position += Velocity * (dt * SpeedFactor);
            if (!CanEnterBossZone) Hookfall.World.StageDirector.KeepBelowZone(transform);
            if (Velocity.sqrMagnitude > 0.5f) transform.rotation = Quaternion.LookRotation(Velocity);

            FrameUpdate(dt);
            Animate(dt);
        }

        // ---------- Health and rewards ----------

        /// <param name="dot">A burn or bleed tick: no crit, flash or knockback.</param>
        public virtual void TakeDamage(float amount, bool dot = false)
        {
            if (IsDead || Invulnerable) return;
            if (!dot && PlayerStats != null && PlayerStats.CritChance > 0f && Random.value < PlayerStats.CritChance) amount *= 2f;   // Hunter's Eye
            if (synergy != null) amount *= 1f + (Shocked ? synergy.ShockAmp : 0f) + (Chilled ? synergy.ChillAmp : 0f);
            float dealt = amount;
            bool shielded = Shield > 0f;
            if (shielded)
            {
                // The barrier takes the blow first; what is left over goes through.
                float absorbed = Mathf.Min(Shield, amount);
                Shield -= absorbed;
                amount -= absorbed;
                if (Shield <= 0f)
                {
                    baseGlow = unshieldedGlow;
                    EnemyDirector.RaiseShieldBroken(this);
                }
            }
            Health -= amount;
            if (!dot)
            {
                flashTimer = flashDuration;
                punch = hitPunch;
                if (knockback > 0f && Player != null && !shielded)
                {
                    Vector3 away = transform.position - Player.transform.position;
                    if (away.sqrMagnitude > 0.01f) Velocity += away.normalized * knockback;
                }
                ApplyGlow();
            }
            EnemyDirector.RaiseHit(this, dealt);
            if (Health <= 0f) Die(true);
        }

        // ---------- School statuses ----------

        public void Burn(float dps, float duration)
        {
            if (IsDead || dps <= 0f) return;
            burnDps = Burning ? Mathf.Max(burnDps, dps) : dps;
            burnTimer = Mathf.Max(burnTimer, duration);
            RefreshStatusGlow();
        }

        public void Bleed(float dps, float duration)
        {
            if (IsDead || dps <= 0f) return;
            bleedDps = Bleeding ? Mathf.Max(bleedDps, dps) : dps;
            bleedTimer = Mathf.Max(bleedTimer, duration);
            RefreshStatusGlow();
        }

        public void Chill(float slow, float duration)
        {
            if (IsDead) return;
            chillSlow = chillTimer > 0f ? Mathf.Max(chillSlow, slow) : slow;
            chillTimer = Mathf.Max(chillTimer, duration);
            RefreshStatusGlow();
        }

        public void Freeze(float duration)
        {
            if (IsDead || ImmuneToFreeze) return;
            freezeTimer = Mathf.Max(freezeTimer, duration);
            RefreshStatusGlow();
        }

        public void Shock(float duration)
        {
            if (IsDead) return;
            shockTimer = Mathf.Max(shockTimer, duration);
            RefreshStatusGlow();
        }

        private void TickStatuses(float dt)
        {
            burnTimer -= dt;
            bleedTimer -= dt;
            chillTimer -= dt;
            shockTimer -= dt;
            freezeTimer -= dt;
            if (Burning || Bleeding)
            {
                dotTimer -= dt;
                if (dotTimer <= 0f)
                {
                    dotTimer = 0.5f;
                    float burn = Burning ? burnDps * (Shocked && synergy != null && synergy.HasDuo(Hookfall.Combat.Duo.Plasma) ? 2f : 1f) : 0f;
                    float dot = burn + (Bleeding ? bleedDps : 0f);
                    if (Burning && Bleeding && synergy != null && synergy.HasDuo(Hookfall.Combat.Duo.BoilingBlood)) dot *= 1.5f;
                    TakeDamage(dot * 0.5f, true);
                    if (IsDead) return;
                }
            }
            RefreshStatusGlow();
        }

        /// <summary>The strongest status tints the enemy (frozen, shocked, burning, chilled, bleeding).</summary>
        private void RefreshStatusGlow()
        {
            Color glow = Frozen ? new Color(0.6f, 0.95f, 1.4f) : Shocked ? new Color(1.2f, 1.05f, 0.25f)
                : Burning ? new Color(1.4f, 0.45f, 0.08f) : Chilled ? new Color(0.25f, 0.6f, 1.1f)
                : Bleeding ? new Color(1f, 0.05f, 0.12f) : Color.black;
            if (glow == statusGlow) return;
            statusGlow = glow;
            ApplyGlow();
        }

        public void Die(bool giveReward)
        {
            if (IsDead) return;
            IsDead = true;
            Active.Remove(this);
            machine.ChangeState(null);
            if (giveReward)
            {
                if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
                PickupSpawner drops = PickupSpawner.Instance;
                if (drops != null)
                {
                    float stage = Hookfall.World.StageDirector.RewardScale;   // later stages pay more
                    drops.DropXP(transform.position, xpReward * rewardMultiplier * stage);
                    drops.DropGold(transform.position, Mathf.RoundToInt(goldReward * rewardMultiplier * stage));
                }
            }
            OnDied(giveReward);
            if (Director == null) { Destroy(gameObject); return; }
            if (giveReward && Affix == EliteAffix.Explosive) Director.EliteBurst(this);
            else if (giveReward && Affix == EliteAffix.Splitting) Director.Split(this);
            Director.NotifyDeath(this, giveReward);
            // A killed enemy bursts for a moment before it goes back to the pool; one that only leaves goes at once.
            if (giveReward && visual != null && DeathTime > 0f) dying = DeathTime;
            else Director.Recycle(this);
        }

        protected virtual void OnDied(bool killed) { }

        /// <summary>The death pop: the body swells, spins and shrinks to nothing, then the enemy returns to its pool.</summary>
        private void TickDeath(float dt)
        {
            dying -= dt;
            float t = 1f - Mathf.Clamp01(dying / Mathf.Max(0.01f, DeathTime));
            visual.localScale = Vector3.one * ((1f + 0.45f * Mathf.Sin(t * Mathf.PI)) * (1f - t * t));
            visual.Rotate(0f, 0f, 900f * dt, Space.Self);
            if (dying > 0f) return;
            dying = 0f;
            Director.Recycle(this);
        }

        // ---------- Steering ----------

        /// <summary>Pursuit: aim at where the player will be, not where they are.</summary>
        public Vector3 PredictedPlayerPosition(float lookAheadSeconds) =>
            Player.transform.position + Player.Body.linearVelocity * lookAheadSeconds;

        /// <summary>Seek with separation from neighbours and short raycasts around islands (GDD 5.1).</summary>
        public void Steer(Vector3 target, float speed)
        {
            Vector3 toTarget = target - transform.position;
            Vector3 seek = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
            Vector3 steer = seek + Separation() * separationWeight + Avoidance(seek, speed) * avoidWeight;
            desiredVelocity = steer.sqrMagnitude > 0.0001f ? steer.normalized * speed : Vector3.zero;
        }

        /// <summary>Chase speed: the given speed, at least a share of the player's speed, and faster the further behind.</summary>
        public float ChaseSpeed(float speed) =>
            Mathf.Max(speed, Player.Speed * playerSpeedMatch) + Mathf.Max(0f, DistanceToPlayer - catchUpDistance) * catchUpPerMetre;

        public void Stop() => desiredVelocity = Vector3.zero;

        /// <summary>Sets the velocity at once, with no acceleration (dashes).</summary>
        public void SetVelocityNow(Vector3 velocity) => Velocity = desiredVelocity = velocity;

        // shortcut: O(n) per think over all enemies; switch to a spatial grid if the 300-enemy profile shows it.
        private Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            float r2 = separationRadius * separationRadius;
            Vector3 p = transform.position;
            for (int i = 0; i < Active.Count; i++)
            {
                EnemyBase other = Active[i];
                if (other == this) continue;
                Vector3 away = p - other.transform.position;
                float d2 = away.sqrMagnitude;
                if (d2 > r2 || d2 < 0.0001f) continue;
                push += away / d2;
            }
            return push;
        }

        private Vector3 Avoidance(Vector3 direction, float speed)
        {
            if (direction == Vector3.zero) return Vector3.zero;
            float distance = Mathf.Max(2f, speed * avoidLookAhead);
            if (!Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                return Vector3.zero;
            if (hit.collider.GetComponentInParent<PlayerController>() != null) return Vector3.zero;
            // Slide along the surface and lift over it: islands hang downwards, so up is usually the way round.
            return (Vector3.ProjectOnPlane(direction, hit.normal) + hit.normal + Vector3.up).normalized;
        }

        public float DistanceToPlayer => Vector3.Distance(transform.position, Player.transform.position);

        // ---------- Visuals ----------

        /// <summary>Red glow for a short attack warning (GDD 5.6). Only Wyverns and Golems use it.</summary>
        public void SetWarning(bool on)
        {
            warning = on;
            ApplyGlow();
        }

        /// <summary>A quick lunge of the body towards the player and back (a bite, a sting).</summary>
        public void Jab(float distance = 0.6f) => jab = distance;

        /// <summary>Squashes the body for a moment (a stomp, a heavy landing), like a hit does.</summary>
        public void Squash(float amount) => punch = Mathf.Max(punch, amount);

        /// <summary>How wide the wings beat right now (1 = the prefab's flap; a dash holds them almost still).</summary>
        protected virtual float FlapScale => 1f;
        /// <summary>Degrees both wings are raised on top of the beat (swept up in a dash).</summary>
        protected virtual float WingRest => 0f;
        /// <summary>Degrees the body pitches (negative rears up).</summary>
        protected virtual float Pitch => 0f;

        private void Animate(float dt)
        {
            if (visual != null)
            {
                // Size: the spawn pop (a little overshoot) times the swell of a hit.
                if (spawnTimer > 0f || punch > 0f)
                {
                    float grow = 1f;
                    if (spawnTimer > 0f)
                    {
                        spawnTimer -= dt;
                        float t = 1f - Mathf.Clamp01(spawnTimer / Mathf.Max(0.01f, spawnTime)), e = t - 1f;
                        grow = Mathf.Max(0.2f, 1f + e * e * (2.70158f * e + 1.70158f));   // back-out easing
                    }
                    punch = Mathf.MoveTowards(punch, 0f, dt * 2.5f);
                    visual.localScale = new Vector3(1f + punch, 1f - punch * 0.5f, 1f + punch) * grow;
                }
                if (Flies)
                {
                    // Roll into the turn, bob on the air, lunge on a bite.
                    Vector3 turn = transform.InverseTransformDirection(desiredVelocity - Velocity);
                    bank = Mathf.Lerp(bank, Mathf.Clamp(-turn.x * 2.4f, -bankAngle, bankAngle), dt * 6f);
                    jab = Mathf.MoveTowards(jab, 0f, dt * 3.5f);
                    pitch = Mathf.Lerp(pitch, Pitch, dt * 10f);
                    visual.localRotation = Quaternion.Euler(pitch, 0f, bank) * visualRotation;
                    visual.localPosition = visualPosition + new Vector3(0f, Mathf.Sin((Time.time + flapPhase) * bobSpeed) * bobHeight, jab);
                }
            }
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) ApplyGlow();
            }
            if (wingLeft == null || wingRight == null) return;
            // The beat quickens with speed; the phase is summed so a change of pace never makes the wings jump.
            flapClock += dt * flapSpeed * Mathf.Lerp(0.8f, 1.5f, Velocity.magnitude / Mathf.Max(1f, moveSpeed * 1.6f));
            wingSpread = Mathf.Lerp(wingSpread, FlapScale, dt * 12f);
            wingLift = Mathf.Lerp(wingLift, WingRest, dt * 12f);
            float angle = Mathf.Sin(flapClock + flapPhase) * flapAngle * wingSpread + wingLift;
            wingLeft.localRotation = Quaternion.Euler(0f, 0f, angle);
            wingRight.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void ApplyGlow()
        {
            if (renderers == null) return;
            Color glow = flashTimer > 0f ? flashColor : warning ? new Color(1.2f, 0.1f, 0.05f) : statusGlow != Color.black ? statusGlow : baseGlow;
            foreach (Renderer r in renderers)
            {
                if (r == null || r is LineRenderer) continue;
                if (glow == Color.black)
                {
                    r.SetPropertyBlock(null);   // keeps the SRP Batcher fast for the plain horde
                    continue;
                }
                r.GetPropertyBlock(block);
                block.SetColor(glowId, glow);
                r.SetPropertyBlock(block);
            }
        }

        // ---------- Fallbacks (GDD 5.7) ----------

        private void RunFallbacks(float elapsed)
        {
            if (Anchored) return;
            if (Director != null && transform.position.y < Director.AbyssHeight)
            {
                Die(false);   // fell into the abyss: no reward
                return;
            }

            float distance = DistanceToPlayer;
            if (distance > despawnDistance)
            {
                Director?.Relocate(this);   // too far: respawn closer, out of view
                ResetStuck();
                return;
            }

            if (!CanGetStuck) return;
            lostTimer = distance < engageDistance ? 0f : lostTimer + elapsed;
            if (lostTimer > lostAfter)
            {
                Director?.Relocate(this);   // never reached the Warden: back ahead of them
                ResetStuck();
                return;
            }

            stuckTimer += elapsed;
            if (stuckTimer < stuckCheckInterval) return;
            if (Vector3.Distance(transform.position, lastCheckedPosition) < stuckMinDistance)
            {
                stuckStrikes++;
                if (stuckStrikes >= 2) { Director?.Relocate(this); ResetStuck(); return; }
                transform.position += Vector3.up * 3f;   // stuck: short upward escape
            }
            else stuckStrikes = 0;
            lastCheckedPosition = transform.position;
            stuckTimer = 0f;
        }

        /// <summary>Flyers can get stuck behind islands; golems stand still on purpose.</summary>
        protected virtual bool CanGetStuck => true;

        private void ResetStuck()
        {
            lastCheckedPosition = transform.position;
            stuckTimer = lostTimer = 0f;
            stuckStrikes = 0;
        }
    }
}
