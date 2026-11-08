// Hookfall - shared enemy logic: health, rewards, steering (pursuit, separation, island avoidance), think rate,
// pooling, hit flash, elites and the fallbacks in GDD 5.7.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 for steering, think LOD, pooling and elites, and given catch-up speed on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Tooltip("When chasing, the enemy flies at least this share of the player's speed.")]
        [SerializeField] private float playerSpeedMatch = 0.8f;
        [Tooltip("Extra speed for every metre beyond catchUpDistance, so stragglers close in.")]
        [SerializeField] private float catchUpPerMetre = 0.4f;
        [SerializeField] private float catchUpDistance = 15f;

        [Header("Thinking (GDD 5.2)")]
        [SerializeField] private float nearDistance = 60f;
        [SerializeField] private float nearThinkInterval = 0.1f;
        [SerializeField] private float farThinkInterval = 0.5f;

        [Header("Fallbacks (GDD 5.7)")]
        [SerializeField] private float despawnDistance = 120f;
        [SerializeField] private float stuckCheckInterval = 3f;
        [SerializeField] private float stuckMinDistance = 1f;

        [Header("Visuals")]
        [Tooltip("Shader colour property used for the hit flash, elite glow and attack warning (toon Shader Graph).")]
        [SerializeField] private string glowProperty = "_EmissionColor";
        [SerializeField] private float flashDuration = 0.08f;
        [SerializeField] private Transform wingLeft;
        [SerializeField] private Transform wingRight;
        [SerializeField] private float flapSpeed = 14f;
        [SerializeField] private float flapAngle = 45f;

        [Header("Game feel")]
        [Tooltip("Speed added away from the player on every hit (golems ignore it).")]
        [SerializeField] private float knockback = 6f;
        [Tooltip("The body swells by this much on a hit and settles back.")]
        [SerializeField] private float hitPunch = 0.3f;

        private readonly StateMachine machine = new StateMachine();
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private int glowId;
        private Color baseGlow = Color.black;
        private bool warning;
        private float flashTimer;
        private float healthMultiplier = 1f, damageMultiplier = 1f, rewardMultiplier = 1f;
        private float thinkTimer, thinkElapsed;
        private float stuckTimer;
        private int stuckStrikes;
        private Vector3 lastCheckedPosition;
        private Vector3 desiredVelocity;
        private Vector3 baseScale;
        private float flapPhase;
        private bool built;
        private Transform visual;
        private float punch;

        public string DisplayName => displayName;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth * healthMultiplier;
        public bool IsDead { get; private set; }
        public bool IsElite { get; private set; }
        public float MoveSpeed => moveSpeed;
        public float Damage => contactDamage * damageMultiplier;
        /// <summary>Time and elite scaling for damage that is not contact damage (boulders, slams).</summary>
        public float DamageScale => damageMultiplier;
        public float HitRadius => hitRadius * transform.localScale.x / Mathf.Max(0.01f, baseScale.x);
        public Vector3 Velocity { get; private set; }
        public string CurrentStateName => machine.Current?.Name ?? "None";
        public EnemyBase SourcePrefab { get; set; }

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
            flapPhase = Random.value * 10f;
        }

        /// <summary>Called by the EnemyDirector every time the enemy is taken from the pool.</summary>
        public void Init(EnemyDirector director, PlayerController player, float healthMult, float damageMult)
        {
            Director = director;
            Player = player;
            PlayerStats = player.GetComponent<PlayerStats>();

            healthMultiplier = healthMult;
            damageMultiplier = damageMult;
            rewardMultiplier = 1f;
            IsElite = false;
            IsDead = false;
            transform.localScale = baseScale;
            Health = MaxHealth;
            Velocity = desiredVelocity = Vector3.zero;
            lastCheckedPosition = transform.position;
            stuckTimer = 0f;
            stuckStrikes = 0;
            thinkTimer = Random.value * nearThinkInterval;   // spread thinking over frames
            baseGlow = Color.black;
            warning = false;
            flashTimer = 0f;
            punch = 0f;
            if (visual != null) visual.localScale = Vector3.one;
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
        public void MakeElite(float healthMult, float damageMult, float scale, float rewardMult, Color glow)
        {
            IsElite = true;
            healthMultiplier *= healthMult;
            damageMultiplier *= damageMult;
            rewardMultiplier = rewardMult;
            Health = MaxHealth;
            transform.localScale = baseScale * scale;
            baseGlow = glow;
            ApplyGlow();
        }

        protected abstract void BuildStates();
        protected abstract IEnemyState DefaultState { get; }
        protected virtual void OnSpawned() { }
        /// <summary>Runs every frame, for things that cannot wait for the next think (a fast dash).</summary>
        protected virtual void FrameUpdate(float deltaTime) { }

        public void ChangeState(IEnemyState next) => machine.ChangeState(next);

        protected virtual void Update()
        {
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

            Velocity = Vector3.MoveTowards(Velocity, desiredVelocity, acceleration * dt);
            transform.position += Velocity * dt;
            if (Velocity.sqrMagnitude > 0.5f) transform.rotation = Quaternion.LookRotation(Velocity);

            FrameUpdate(dt);
            Animate(dt);
        }

        // ---------- Health and rewards ----------

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (PlayerStats != null && PlayerStats.CritChance > 0f && Random.value < PlayerStats.CritChance) amount *= 2f;   // Hunter's Eye
            Health -= amount;
            flashTimer = flashDuration;
            punch = hitPunch;
            if (knockback > 0f && Player != null)
            {
                Vector3 away = transform.position - Player.transform.position;
                if (away.sqrMagnitude > 0.01f) Velocity += away.normalized * knockback;
            }
            ApplyGlow();
            EnemyDirector.RaiseHit(this, amount);
            if (Health <= 0f) Die(true);
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
                    drops.DropXP(transform.position, xpReward * rewardMultiplier);
                    drops.DropGold(transform.position, Mathf.RoundToInt(goldReward * rewardMultiplier));
                }
            }
            OnDied(giveReward);
            if (Director != null) Director.Release(this, giveReward);
            else Destroy(gameObject);
        }

        protected virtual void OnDied(bool killed) { }

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

        private void Animate(float dt)
        {
            if (visual != null && punch > 0f)
            {
                punch = Mathf.MoveTowards(punch, 0f, dt * 2.5f);
                visual.localScale = new Vector3(1f + punch, 1f - punch * 0.5f, 1f + punch);
            }
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) ApplyGlow();
            }
            if (wingLeft == null || wingRight == null) return;
            float angle = Mathf.Sin((Time.time + flapPhase) * flapSpeed) * flapAngle;
            wingLeft.localRotation = Quaternion.Euler(0f, 0f, angle);
            wingRight.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void ApplyGlow()
        {
            if (renderers == null) return;
            Color glow = flashTimer > 0f ? Color.white : warning ? new Color(1.2f, 0.1f, 0.05f) : baseGlow;
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
            if (Director != null && transform.position.y < Director.AbyssHeight)
            {
                Die(false);   // fell into the abyss: no reward
                return;
            }

            if (DistanceToPlayer > despawnDistance)
            {
                Director?.Relocate(this);   // too far: respawn closer, out of view
                ResetStuck();
                return;
            }

            stuckTimer += elapsed;
            if (stuckTimer < stuckCheckInterval || !CanGetStuck) return;
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
            stuckTimer = 0f;
            stuckStrikes = 0;
        }
    }
}
