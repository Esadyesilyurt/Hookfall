// Hookfall - the Golem: ranged tank on an island. Idle, Aim (0.4 s red aim line), Throw boulder, Cooldown,
// and Ground slam when the player lands on its island (GDD 5.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using Hookfall.Player;
using Hookfall.World;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Golem : EnemyBase
    {
        [Header("Golem")]
        [SerializeField] private float range = 35f;
        [SerializeField] private float aimTime = 0.4f;
        [SerializeField] private float throwCooldown = 1.5f;
        [Tooltip("Random aim error in metres, so the golem feels imperfect (GDD 5.5).")]
        [SerializeField] private float aimError = 2f;
        [SerializeField] private float boulderSpeed = 25f;
        [SerializeField] private float boulderDamage = 20f;
        [SerializeField] private float slamWindUp = 0.4f;
        [SerializeField] private float slamRadius = 3f;
        [SerializeField] private float slamDamage = 25f;
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private Material warningMaterial;
        [Tooltip("Optional arm transforms raised while aiming and slamming.")]
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;

        private LineRenderer aimLine;
        private LineRenderer slamRing;

        public GolemIdleState Idle { get; private set; }
        public GolemAimState Aim { get; private set; }
        public GolemCooldownState Cooldown { get; private set; }
        public GolemSlamState Slam { get; private set; }

        public Island HomeIsland { get; set; }
        public float Range => range;
        public float AimTime => aimTime;
        public float ThrowCooldown => throwCooldown;
        public float SlamWindUp => slamWindUp;
        public Vector3 Hand => transform.position + transform.rotation * Vector3.Scale(handOffset, transform.localScale);
        public float ArmRaise { get; set; }

        protected override IEnemyState DefaultState => Idle;
        protected override bool CanGetStuck => false;

        protected override void Awake()
        {
            base.Awake();
            aimLine = CreateLine("Aim Line", 0.12f, false);
            slamRing = CreateLine("Slam Ring", 0.15f, true);
            slamRing.positionCount = 32;
        }

        protected override void BuildStates()
        {
            Idle = new GolemIdleState(this);
            Aim = new GolemAimState(this);
            Cooldown = new GolemCooldownState(this);
            Slam = new GolemSlamState(this);
        }

        protected override void OnSpawned()
        {
            ShowAimLine(false, Vector3.zero);
            ShowSlamRing(false);
            ArmRaise = 0f;
        }

        protected override void OnDied(bool killed)
        {
            if (HomeIsland != null && HomeIsland.Occupant == this) HomeIsland.Occupant = null;
            HomeIsland = null;
            ShowAimLine(false, Vector3.zero);
            ShowSlamRing(false);
            if (killed && Hookfall.World.PowerUpSpawner.Instance != null) Hookfall.World.PowerUpSpawner.Instance.GolemDrop(transform.position);
        }

        protected override void FrameUpdate(float deltaTime)
        {
            Vector3 flat = Player.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), deltaTime * 6f);

            float angle = Mathf.Lerp(0f, -150f, ArmRaise);
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(angle, 0f, 0f);
            if (armRight != null) armRight.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        public bool PlayerOnMyIsland => HomeIsland != null && Player.CurrentIsland == HomeIsland;

        public bool PlayerInRangeAndSight()
        {
            if (DistanceToPlayer > range) return false;
            Vector3 from = Hand;
            Vector3 to = Player.transform.position;
            if (!Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<PlayerController>() != null;
        }

        /// <summary>Where the boulder should go: the predicted player position plus up to 2 m of error.</summary>
        public Vector3 AimPoint()
        {
            float travel = Vector3.Distance(Hand, Player.transform.position) / boulderSpeed;
            return PredictedPlayerPosition(travel) + Random.insideUnitSphere * aimError;
        }

        public void Throw(Vector3 target)
        {
            Vector3 direction = (target - Hand).normalized;
            Director?.SpawnBoulder(Hand, direction * boulderSpeed, boulderDamage * DamageScale);
        }

        public void DoSlam()
        {
            Vector3 offset = Player.transform.position - transform.position;
            if (offset.magnitude <= slamRadius * transform.localScale.x && PlayerStats != null)
                PlayerStats.TakeDamage(slamDamage * DamageScale);
        }

        public void ShowAimLine(bool show, Vector3 target)
        {
            aimLine.enabled = show;
            if (!show) return;
            aimLine.SetPosition(0, Hand);
            aimLine.SetPosition(1, target);
        }

        public void ShowSlamRing(bool show)
        {
            slamRing.enabled = show;
            if (!show) return;
            float r = slamRadius * transform.localScale.x;
            for (int i = 0; i < slamRing.positionCount; i++)
            {
                float a = i / (float)slamRing.positionCount * Mathf.PI * 2f;
                slamRing.SetPosition(i, transform.position + new Vector3(Mathf.Cos(a) * r, 0.15f, Mathf.Sin(a) * r));
            }
        }

        private LineRenderer CreateLine(string name, float width, bool loop)
        {
            var go = new GameObject(name);
            go.layer = Hookfall.Core.Layers.NoOutline;
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.sharedMaterial = warningMaterial;
            line.startColor = line.endColor = new Color(1f, 0.15f, 0.1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
            return line;
        }
    }
}
