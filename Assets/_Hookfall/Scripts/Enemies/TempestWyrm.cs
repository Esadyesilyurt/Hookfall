// Hookfall - the second stage boss, the Tempest Wyrm (2026-11-11): a storm serpent that takes every second stage.
// Where the Colossus stands still, the Wyrm never stops: it circles the arena with its body trailing behind it,
// spits fans of slow-homing lightning, and every few seconds turns on the Warden, glows red for a moment (the short
// warning, GDD 5.6) and dives straight through where they were. Its head is the target; touching the head or any
// part of the body hurts and throws the Warden off. It shares the Colossus's health bar, escort calls, rage and
// entrance (Boss); only the fight and the animation are its own.
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Audio;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class TempestWyrm : Boss
    {
        private enum Mode { Circle, Aim, Dive }

        private static readonly Color StormColor = new Color(0.45f, 0.9f, 1f);

        [Header("Body (set by the prefab builder from the model)")]
        [SerializeField] private Transform segmentTemplate;
        [SerializeField] private Transform tail;
        [SerializeField] private int segments = 9;
        [Tooltip("Metres between the pieces of the body.")]
        [SerializeField] private float spacing = 6f;
        [SerializeField] private float headRadius = 5f;
        [SerializeField] private float segmentRadius = 4f;

        [Header("Circling")]
        [SerializeField] private float circleRadius = 40f;
        [SerializeField] private float circleHeight = 10f;
        [SerializeField] private float circleBob = 8f;
        [SerializeField] private float circleSpeed = 20f;
        [SerializeField] private float turnRate = 150f;
        [SerializeField] private float spitInterval = 3.5f;
        [SerializeField] private float spitRange = 150f;

        [Header("Dive")]
        [SerializeField] private float diveInterval = 9f;
        [SerializeField] private float diveWarning = 0.5f;
        [SerializeField] private float diveSpeed = 62f;
        [Tooltip("Metres the dive carries on past where the Warden was.")]
        [SerializeField] private float diveOvershoot = 28f;
        [SerializeField] private float diveDamage = 32f;

        private Mode mode;
        private Vector3 home, diveDirection;
        private float angle, spitTimer, diveTimer, wait, diveLeft;
        private Transform bodyRoot;
        private Transform[] pieces;
        private Vector3[] chain;   // where each piece is on the chain, without its sway

        protected override Color ShardTint => StormColor;
        protected override string CallLine => "The Wyrm calls the storm!";

        protected override void OnSpawned()
        {
            base.OnSpawned();
            mode = Mode.Circle;
            spitTimer = 2f;
            diveTimer = diveInterval * 0.6f;
            BuildBody();
        }

        /// <summary>The body: copies of the model's one segment and then the tail, in the world, not under the head.</summary>
        private void BuildBody()
        {
            if (bodyRoot != null || segmentTemplate == null) return;
            bodyRoot = new GameObject("Wyrm Body").transform;
            bodyRoot.SetParent(effects, false);   // the Boss destroys its effects when it dies or the scene ends
            pieces = new Transform[segments + (tail != null ? 1 : 0)];
            // Their own scale, not lossyScale: Visual is small at this moment (EnemyBase pops a spawning enemy in).
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform piece = i < segments ? Instantiate(segmentTemplate, bodyRoot) : tail;
                piece.SetParent(bodyRoot, false);
                // Thinner towards the tail.
                piece.localScale = (i < segments ? segmentTemplate.localScale : tail.localScale) * Mathf.Lerp(1f, 0.6f, i / (float)segments);
                pieces[i] = piece;
            }
            segmentTemplate.gameObject.SetActive(false);
            chain = new Vector3[pieces.Length];
            Trail();
        }

        // ---------- The entrance: it does not fall like the Colossus, it comes up out of the deep in a closing spiral ----------

        public override float EntranceStart => 0.2f;
        public override float EntranceTime => 2.4f;
        public override bool LandsHeavy => false;

        private Quaternion? faceAfter;

        public override void Descend(Vector3 rest, float t)
        {
            if (t <= 0f) faceAfter = transform.rotation;   // the cutscene has just turned it to the camera
            float left = (1f - t) * (1f - t);              // fast at first, slowing as it arrives
            float turn = left * Mathf.PI * 2.6f;
            // A wide, almost level circle that closes on its place, so the whole body streams out behind the head.
            // It ends 16 m over the arena's middle: clear of the islands the camera looks past from below.
            Vector3 at = rest + new Vector3(Mathf.Cos(turn) * 110f * left, 16f - 26f * left, Mathf.Sin(turn) * 110f * left);
            if (Visual != null) Visual.localScale = Vector3.one;   // time stands still in the cutscene: no spawn pop
            Vector3 step = at - transform.position;
            Quaternion along = step.sqrMagnitude > 0.0001f && t > 0f ? Quaternion.LookRotation(step) : transform.rotation;
            transform.SetPositionAndRotation(at, Quaternion.Slerp(along, faceAfter ?? transform.rotation, Mathf.SmoothStep(0f, 1f, (t - 0.8f) / 0.2f)));
            if (t <= 0f) Trail();
        }

        /// <summary>Lays the body out straight behind the head (the start of the entrance).</summary>
        private void Trail()
        {
            if (chain == null) return;
            for (int i = 0; i < chain.Length; i++)
                chain[i] = transform.position - transform.forward * spacing * (i + 1);
        }

        public override void BeginFight()
        {
            base.BeginFight();
            home = transform.position;
            angle = Mathf.Atan2(transform.forward.z, transform.forward.x);
        }

        protected override void Fight(float deltaTime, float speedUp)
        {
            CheckCalls();
            Touch(deltaTime);
            Vector3 toPlayer = Player.Body.position - transform.position;
            switch (mode)
            {
                case Mode.Circle:
                    angle += circleSpeed / circleRadius * deltaTime * (Raging ? 1.25f : 1f);
                    Vector3 target = home + new Vector3(Mathf.Cos(angle) * circleRadius,
                        circleHeight + Mathf.Sin(age * 0.7f) * circleBob, Mathf.Sin(angle) * circleRadius);
                    Advance(target - transform.position, circleSpeed * 1.4f, turnRate, deltaTime);
                    spitTimer -= deltaTime * speedUp;
                    diveTimer -= deltaTime * speedUp;
                    if (diveTimer <= 0f)
                    {
                        mode = Mode.Aim;
                        wait = diveWarning;
                        SetWarning(true);
                    }
                    else if (spitTimer <= 0f && toPlayer.magnitude <= spitRange)
                    {
                        spitTimer = spitInterval;
                        flare = 1f;
                        FireVolley();
                    }
                    break;

                case Mode.Aim:
                    // It hangs in the air and turns its head on the Warden: the warning.
                    if (toPlayer.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPlayer), 400f * deltaTime);
                    wait -= deltaTime;
                    if (wait > 0f) break;
                    SetWarning(false);
                    mode = Mode.Dive;
                    diveDirection = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
                    diveLeft = toPlayer.magnitude + diveOvershoot;
                    if (audioManager != null) audioManager.Play(SoundId.BossRoar, transform.position);
                    break;

                case Mode.Dive:
                    float step = diveSpeed * deltaTime;
                    transform.position += diveDirection * step;
                    transform.rotation = Quaternion.LookRotation(diveDirection);
                    diveLeft -= step;
                    if (diveLeft > 0f) break;
                    mode = Mode.Circle;
                    diveTimer = diveInterval;
                    // Back to the circle from wherever the dive ended.
                    Vector3 from = transform.position - home;
                    angle = Mathf.Atan2(from.z, from.x);
                    break;
            }
        }

        /// <summary>Flies towards a point, turning its head the way it goes.</summary>
        private void Advance(Vector3 to, float speed, float turn, float dt)
        {
            if (to.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turn * dt);
            transform.position += transform.forward * Mathf.Min(speed * dt, to.magnitude);
        }

        /// <summary>The head and every piece of the body hurt, and throw the Warden off; a dive hurts more.</summary>
        private void Touch(float dt)
        {
            bodyTimer -= dt;
            Vector3 p = Player.Body.position;
            Vector3 hit = transform.position;
            bool touched = (p - hit).sqrMagnitude <= headRadius * headRadius;
            if (!touched && pieces != null)
                foreach (Transform piece in pieces)
                    if ((p - piece.position).sqrMagnitude <= segmentRadius * segmentRadius) { hit = piece.position; touched = true; break; }
            if (!touched) return;
            Vector3 away = p - hit;
            Player.Body.linearVelocity = (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.up) * bodyPush + Vector3.up * 5f;
            if (bodyTimer > 0f) return;
            bodyTimer = 0.8f;
            PlayerStats.TakeDamageFrom((mode == Mode.Dive ? diveDamage : bodyDamage) * DamageScale, hit);
        }

        /// <summary>The Colossus's head, jaw and gem, and the body following the head like a chain.</summary>
        public override void Animate(float dt)
        {
            base.Animate(dt);
            if (jaw != null) jaw.localRotation = Quaternion.Euler(32f * roarPose + 22f * flare + (mode == Mode.Dive ? 26f : 0f), 0f, 0f);
            if (pieces == null) return;
            Vector3 lead = transform.position;
            for (int i = 0; i < pieces.Length; i++)
            {
                Vector3 back = chain[i] - lead;
                if (back.sqrMagnitude < 0.0001f) back = -transform.forward;
                // Each piece keeps its distance to the one before it; the sway is only drawn, never fed back into the chain.
                chain[i] = lead + back.normalized * spacing;
                Vector3 shown = chain[i] + Vector3.up * (Mathf.Sin(age * 2.2f - i * 0.8f) * 0.6f);
                pieces[i].SetPositionAndRotation(shown, Quaternion.LookRotation(lead - chain[i], Vector3.up));
                lead = chain[i];
            }
        }

        protected override void OnDied(bool killed)
        {
            if (pieces != null && killed && vfx != null)
                foreach (Transform piece in pieces) vfx.Burst(piece.position, 24, StormColor, 12f, 0.4f, 0.9f);
            bodyRoot = null;
            pieces = null;
            chain = null;
            base.OnDied(killed);
        }
    }
}
