// Hookfall - the stage boss, the Abyss Colossus (2026-11-08): a stone titan that rises over the tower once the stage's
// gold target is reached (StageDirector). It never moves. It turns to face the Warden, throws fans of slow-homing
// crystal shards (islands block them), slams a shockwave ring when the Warden comes close (fly over or under it),
// calls wyverns and elite bats at 70% and 35% health and rages below 30%. Its attacks come faster while the Warden
// stands on an island (Frenzy, GDD 5). Touching its body hurts and throws the Warden off. Telegraphs are short:
// the core flares before a volley, the body glows red before a slam. The CutsceneDirector plays its entrance.
// Since 2026-11-11 it is also the base of the second boss (TempestWyrm), which replaces Fight and Animate.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Audio;
using Hookfall.Core;
using Hookfall.Enemies.States;
using Hookfall.Feel;
using Hookfall.UI;
using Hookfall.VFX;
using Hookfall.World;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Boss : EnemyBase
    {
        private enum Attack { None, Volley, Slam }

        private static readonly Color ShardColor = new Color(1f, 0.3f, 0.85f);
        private static readonly Color RingColor = new Color(1f, 0.25f, 0.45f);

        [Header("Parts (set by the prefab builder from the model)")]
        [SerializeField] protected Transform head;
        [SerializeField] protected Transform jaw;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [SerializeField] protected Transform core;
        [SerializeField] private Transform[] orbitRocks;
        [SerializeField] private Material shardMaterial;
        [Tooltip("The particle material: shard trails and the shockwave ring.")]
        [SerializeField] private Material effectMaterial;

        [Header("Body")]
        [SerializeField] private float turnSpeed = 70f;
        [Tooltip("The body as a vertical capsule around the origin (the core): radius and half height.")]
        [SerializeField] private float bodyRadius = 5.5f;
        [SerializeField] private float bodyHalfHeight = 8.5f;
        [SerializeField] protected float bodyDamage = 20f;
        [SerializeField] protected float bodyPush = 22f;

        [Header("Shard volley")]
        [SerializeField] private float volleyInterval = 3.0f;
        [SerializeField] private float volleyWindUp = 0.5f;
        [SerializeField] private int volleyShards = 6;
        [SerializeField] private float volleySpread = 30f;
        [SerializeField] private float shardSpeed = 34f;
        [Tooltip("Degrees per second the shards turn towards the Warden: enough to follow, not enough to catch a swing.")]
        [SerializeField] private float shardTurnRate = 38f;
        [SerializeField] private float shardDamage = 16f;
        [SerializeField] private float shardLife = 5.5f;
        [SerializeField] private float volleyRange = 150f;

        [Header("Shockwave slam")]
        [SerializeField] private float slamRange = 24f;
        [SerializeField] private float slamInterval = 6.5f;
        [SerializeField] private float slamWindUp = 0.7f;
        [SerializeField] private float slamDamage = 36f;
        [SerializeField] private float ringSpeed = 32f;
        [SerializeField] private float ringMaxRadius = 46f;
        [SerializeField] private float ringThickness = 2.6f;
        [Tooltip("The ring hits within this height of its plane: fly over or under it.")]
        [SerializeField] private float ringHeight = 3.2f;

        [Header("Calls and rage")]
        [SerializeField] private float[] callAt = { 0.7f, 0.35f };
        [SerializeField] private int calledWyverns = 5;
        [SerializeField] private int calledBats = 10;
        [SerializeField] private float rageBelow = 0.3f;
        [SerializeField] private float rageSpeedUp = 1.45f;
        [SerializeField] private int rageExtraShards = 2;
        [Tooltip("Attacks come this much faster while the Warden stands on an island (Frenzy, GDD 5).")]
        [SerializeField] private float frenzySpeedUp = 1.6f;

        private BossState state;
        private bool fighting;
        private Attack pending;
        private float windUp, volleyTimer, slamTimer;
        protected float bodyTimer;
        private int nextCall;
        protected Transform effects;
        private LineRenderer ring;
        private bool ringActive, ringHit;
        private float ringRadius;
        private Vector3 ringCentre;
        private float volleyPose, slamPose, roarTimer;
        protected float age, roarPose, flare;
        private int volleyArm;
        private Vector3[] orbitStart;
        private Vector3 coreScale = Vector3.one;
        protected VfxManager vfx;
        protected AudioManager audioManager;
        protected FeelDirector feel;

        /// <summary>The colour of its shots and bursts, and what it shouts when it calls its escort.</summary>
        protected virtual Color ShardTint => ShardColor;
        protected virtual string CallLine => "The Colossus calls the sky!";

        public override bool IsBoss => true;
        public override bool CanEnterBossZone => true;
        protected override bool Invulnerable => !fighting;
        protected override bool ImmuneToFreeze => true;
        protected override bool Anchored => true;
        protected override bool CanGetStuck => false;
        protected override bool Flies => false;         // it stands: no banking or bobbing
        protected override float DeathTime => 0f;       // it stages its own fall (StageDirector.BossDied)
        protected override IEnemyState DefaultState => state;

        public bool Fighting => fighting;
        /// <summary>The entrance cutscene: when its arrival starts, how long it takes, and whether it lands with a crash.</summary>
        public virtual float EntranceStart => 0.8f;
        public virtual float EntranceTime => 1.5f;
        public virtual bool LandsHeavy => true;
        public bool Raging => Health < MaxHealth * rageBelow;
        /// <summary>The weak point the items see: the crystal core in the chest.</summary>
        public Vector3 CorePosition => core != null ? core.position : transform.position;

        protected override void Awake()
        {
            base.Awake();
            if (core != null) coreScale = core.localScale;
            if (orbitRocks != null)
            {
                orbitStart = new Vector3[orbitRocks.Length];
                for (int i = 0; i < orbitRocks.Length; i++) orbitStart[i] = orbitRocks[i] != null ? orbitRocks[i].localPosition : Vector3.zero;
            }
        }

        protected override void BuildStates() => state = new BossState(this);

        /// <summary>The scene services the boss uses for its effects (a prefab cannot point at them).</summary>
        public void Bind(VfxManager effectsManager, AudioManager sound, FeelDirector feelDirector)
        {
            vfx = effectsManager;
            audioManager = sound;
            feel = feelDirector;
        }

        protected override void OnSpawned()
        {
            fighting = false;
            pending = Attack.None;
            nextCall = 0;
            volleyTimer = 1.6f;
            slamTimer = slamInterval * 0.5f;
            bodyTimer = 0f;
            roarTimer = 0f;
            if (effects == null)
            {
                effects = new GameObject("Boss Effects").transform;   // not a child: the boss turns, its shots and ring do not
                var ringGo = new GameObject("Shockwave") { layer = Layers.NoOutline };
                ringGo.transform.SetParent(effects, false);
                ring = ringGo.AddComponent<LineRenderer>();
                ring.useWorldSpace = true;
                ring.loop = true;
                ring.positionCount = 64;
                ring.widthMultiplier = 1.1f;
                if (effectMaterial != null)
                {
                    ring.material = new Material(effectMaterial);
                    ring.material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                }
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.enabled = false;
            }
        }

        // ---------- The entrance (driven by the CutsceneDirector in unscaled time) ----------

        /// <summary>Somewhere on the way down to its place: t 0 is high in the sky, 1 is landed.</summary>
        public virtual void Descend(Vector3 rest, float t)
        {
            float fall = 1f - t * t;   // speeds up like something heavy falling
            transform.position = rest + Vector3.up * (55f * fall);
            transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, t);
        }

        /// <summary>Faces a point at once (the Warden, when it arrives).</summary>
        public void Face(Vector3 point)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
        }

        /// <summary>A roar of this many seconds: head back, jaw open, arms out.</summary>
        public void Roar(float seconds)
        {
            roarTimer = seconds;
            if (audioManager != null) audioManager.Play(SoundId.BossRoar, transform.position);
        }

        public virtual void BeginFight()
        {
            fighting = true;
            transform.localScale = Vector3.one;
        }

        // ---------- The fight ----------

        protected override void FrameUpdate(float deltaTime)
        {
            Animate(deltaTime);
            if (!fighting || deltaTime <= 0f || Player == null) return;

            Fight(deltaTime, (Raging ? rageSpeedUp : 1f) * (Player.IsAirborne ? 1f : frenzySpeedUp));
        }

        /// <summary>One frame of the fight; speedUp is rage and Frenzy together.</summary>
        protected virtual void Fight(float deltaTime, float speedUp)
        {
            TurnTowardsPlayer(deltaTime);
            CheckCalls();
            TouchDamage(deltaTime);
            UpdateRing(deltaTime);

            if (pending != Attack.None)
            {
                windUp -= deltaTime;
                if (windUp <= 0f) Release();
                return;
            }
            volleyTimer -= deltaTime * speedUp;
            slamTimer -= deltaTime * speedUp;
            float distance = Vector3.Distance(Player.Body.position, transform.position);
            if (slamTimer <= 0f && distance <= slamRange + HitRadius) Begin(Attack.Slam, slamWindUp);
            else if (volleyTimer <= 0f && distance <= volleyRange) Begin(Attack.Volley, volleyWindUp / Mathf.Max(1f, speedUp * 0.8f));
        }

        private void TurnTowardsPlayer(float dt)
        {
            Vector3 flat = Player.Body.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * dt);
        }

        private void Begin(Attack attack, float seconds)
        {
            pending = attack;
            windUp = seconds;
            if (attack == Attack.Slam) SetWarning(true);   // red: the short warning (GDD 5.6)
            else flare = 1f;                              // the core flares before a volley
        }

        private void Release()
        {
            Attack attack = pending;
            pending = Attack.None;
            if (attack == Attack.Volley) FireVolley();
            else Slam();
        }

        protected void FireVolley()
        {
            volleyTimer = volleyInterval;
            volleyPose = 1f;
            volleyArm = 1 - volleyArm;
            Vector3 from = CorePosition + transform.forward * 2.5f;
            float lead = Vector3.Distance(from, Player.Body.position) / Mathf.Max(1f, shardSpeed) * 0.5f;
            Vector3 aim = (Player.Body.position + Player.Body.linearVelocity * lead - from).normalized;
            int count = volleyShards + (Raging ? rageExtraShards : 0);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-volleySpread * 0.5f, volleySpread * 0.5f, i / (float)(count - 1)) : 0f;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                BossShard shard = BossShard.Create(effects, shardMaterial, effectMaterial, ShardTint);
                shard.Launch(Player, from + dir * 1.5f, dir * shardSpeed, shardDamage * DamageScale, shardTurnRate, shardLife, ShardBroke);
            }
            if (audioManager != null) audioManager.Play(SoundId.BossVolley, from);
            if (vfx != null) vfx.Burst(from, 18, ShardTint, 9f, 0.3f, 0.5f);
        }

        private void ShardBroke(Vector3 at)
        {
            if (vfx != null) vfx.Burst(at, 10, ShardTint, 6f, 0.22f, 0.4f);
        }

        private void Slam()
        {
            SetWarning(false);
            slamTimer = slamInterval;
            slamPose = 1f;
            ringActive = true;
            ringHit = false;
            ringRadius = bodyRadius;
            ringCentre = transform.position;
            ring.enabled = true;
            if (audioManager != null) audioManager.Play(SoundId.BossSlam, transform.position);
            if (feel != null) feel.Shake(0.7f);
            if (vfx != null) vfx.Ring(transform.position, 60, RingColor, ringSpeed * 0.8f);
        }

        private void UpdateRing(float dt)
        {
            if (!ringActive) return;
            ringRadius += ringSpeed * dt;
            if (ringRadius > ringMaxRadius)
            {
                ringActive = false;
                ring.enabled = false;
                return;
            }
            for (int i = 0; i < ring.positionCount; i++)
            {
                float a = i / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(i, ringCentre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius);
            }
            Color c = RingColor;
            c.a = 1f - ringRadius / ringMaxRadius * 0.7f;
            ring.startColor = ring.endColor = c;
            ring.widthMultiplier = Mathf.Lerp(1.6f, 0.6f, ringRadius / ringMaxRadius);

            if (ringHit) return;
            Vector3 p = Player.Body.position;
            float flat = new Vector2(p.x - ringCentre.x, p.z - ringCentre.z).magnitude;
            if (Mathf.Abs(flat - ringRadius) > ringThickness || Mathf.Abs(p.y - ringCentre.y) > ringHeight) return;
            ringHit = true;
            PlayerStats.TakeDamageFrom(slamDamage * DamageScale, ringCentre);
            Vector3 away = new Vector3(p.x - ringCentre.x, 0f, p.z - ringCentre.z).normalized;
            Player.Body.linearVelocity += away * 14f + Vector3.up * 6f;
        }

        /// <summary>The body hurts: the Warden is thrown off it instead of resting against it.</summary>
        private void TouchDamage(float dt)
        {
            bodyTimer -= dt;
            Vector3 p = Player.Body.position;
            Vector3 local = p - transform.position;
            Vector3 closest = transform.position + Vector3.up * Mathf.Clamp(local.y, -bodyHalfHeight, bodyHalfHeight);
            Vector3 away = p - closest;
            if (away.sqrMagnitude > (bodyRadius + 1.6f) * (bodyRadius + 1.6f)) return;
            Vector3 push = away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward;
            Player.Body.linearVelocity = push * bodyPush + Vector3.up * 5f;
            if (bodyTimer > 0f) return;
            bodyTimer = 0.8f;
            PlayerStats.TakeDamageFrom(bodyDamage * DamageScale, transform.position);
        }

        protected void CheckCalls()
        {
            if (callAt == null || nextCall >= callAt.Length || Health > MaxHealth * callAt[nextCall]) return;
            nextCall++;
            bool elite = nextCall >= callAt.Length;
            Roar(1.4f);
            if (feel != null) feel.Shake(0.5f);
            HUD.Announce(Loc.T(CallLine), ShardTint);
            if (Director == null) return;
            for (int i = 0; i < calledWyverns; i++)
                Director.SpawnEscort(true, transform.position + Around(i, calledWyverns, 22f), elite);
            for (int i = 0; i < calledBats; i++)
                Director.SpawnEscort(false, transform.position + Around(i, calledBats, 16f), true);
        }

        private static Vector3 Around(int i, int count, float radius)
        {
            float a = (i + Random.value * 0.5f) / count * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(a) * radius, Random.Range(-6f, 6f), Mathf.Sin(a) * radius);
        }

        // ---------- Procedural animation ----------

        /// <summary>Breathing, orbiting rocks and the poses; also run by the entrance cutscene while time stands still.</summary>
        public virtual void Animate(float dt)
        {
            age += dt;
            roarTimer = Mathf.Max(0f, roarTimer - dt);
            float roar = roarTimer > 0f ? Mathf.Clamp01(Mathf.Min(roarTimer, 0.35f) / 0.35f) : 0f;
            roarPose = Mathf.MoveTowards(roarPose, roar, dt * 5f);
            volleyPose = Mathf.MoveTowards(volleyPose, 0f, dt * 2.2f);
            slamPose = Mathf.MoveTowards(slamPose, 0f, dt * 1.6f);
            flare = Mathf.MoveTowards(flare, 0f, dt * 1.8f);
            float lift = pending == Attack.Slam ? 1f - Mathf.Clamp01(windUp / Mathf.Max(0.01f, slamWindUp)) : 0f;

            float sway = Mathf.Sin(age * 1.3f) * 4f;
            ArmPose(armLeft, sway, volleyArm == 0, lift);
            ArmPose(armRight, -sway, volleyArm == 1, lift);
            if (head != null)
            {
                float look = 0f;
                if (Player != null)
                {
                    Vector3 local = transform.InverseTransformPoint(Player.transform.position);
                    look = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -30f, 30f);
                }
                head.localRotation = Quaternion.Euler(-28f * roarPose + Mathf.Sin(age * 0.9f) * 2f, look, 0f);
            }
            if (jaw != null) jaw.localRotation = Quaternion.Euler(32f * roarPose + 6f * lift, 0f, 0f);
            if (core != null)
            {
                core.localScale = coreScale * (1f + 0.07f * Mathf.Sin(age * 3.2f) + 0.35f * flare + 0.15f * roarPose);
                core.Rotate(0f, 40f * dt, 0f, Space.Self);
            }
            if (orbitRocks != null && orbitStart != null)
                for (int i = 0; i < orbitRocks.Length; i++)
                {
                    if (orbitRocks[i] == null) continue;
                    orbitRocks[i].localPosition = Quaternion.Euler(0f, age * 18f, 0f) * orbitStart[i] + Vector3.up * (Mathf.Sin(age * 1.4f + i * 2f) * 0.6f);
                    orbitRocks[i].Rotate(10f * dt, 25f * dt, 0f, Space.Self);
                }
        }

        private void ArmPose(Transform arm, float sway, bool throwing, float lift)
        {
            if (arm == null) return;
            float side = Mathf.Sign(arm.localPosition.x == 0f ? 1f : arm.localPosition.x);
            // Negative pitch swings the hanging arm forward and up; positive swings it back.
            float pitch = sway - 75f * (throwing ? volleyPose : 0f) - 160f * lift + 30f * slamPose - 35f * roarPose;
            float roll = side * (50f * roarPose + 10f * lift);
            arm.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        // ---------- Death ----------

        protected override void OnDied(bool killed)
        {
            SetWarning(false);
            Transform remains = transform.Find("Visual");
            if (remains != null && killed) remains.SetParent(null, true);   // the StageDirector lets it crumble
            else remains = null;
            if (effects != null) Destroy(effects.gameObject);
            effects = null;
            if (StageDirector.Instance != null) StageDirector.Instance.BossDied(this, killed, remains);
            else if (remains != null) Destroy(remains.gameObject);
        }

        private void OnDestroy()
        {
            if (effects != null) Destroy(effects.gameObject);
        }
    }
}
