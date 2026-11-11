// Hookfall - procedural animation for the Warden model's parts (no rig): every character shares the same parts
// (Head, Scarf, Arm_L, Arm_R, Leg_L, Leg_R, Cape), and this moves them from what the Warden is doing.
//   On an island   a walk cycle with a bob, a lean into the run and a twist of the body; standing, it breathes.
//   In the air     the body leans with speed; rising the legs trail, falling they split and the free arm goes out.
//   On the rope    the body hangs from the anchor, the legs kick through the swing, both hands hold on in a Zip.
//   Moments        a squash on landing and a stretch on a jump, a tuck on the double jump, a streamlined dash,
//                  a flinch when hurt, the arms pushing up on a ledge climb.
//   Always         the head looks where the player aims; the cape and scarf follow on springs.
// Flying fast with no rope out, the Warden does tricks: front flips, back flips, barrel rolls (added 2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; rebuilt on 2026-11-10 (poses for every move, squash
// and stretch, springs, head look). Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Player
{
    public class WardenAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private GrappleHook hook;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform hookArm;
        [SerializeField] private Transform otherArm;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform cape;
        [SerializeField] private Transform scarf;
        [SerializeField] private Transform head;

        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float walkCycleSpeed = 9f;
        [SerializeField] private float walkSwing = 36f;
        [SerializeField] private float maxLean = 35f;
        [Tooltip("How far the body follows the rope while swinging (0 stays upright, 1 hangs straight from the anchor).")]
        [SerializeField, Range(0f, 1f)] private float ropeHang = 0.55f;
        [Tooltip("Squash on landing per m/s of fall speed, and its ceiling.")]
        [SerializeField] private float landSquash = 0.03f;
        [SerializeField] private float maxSquash = 0.35f;
        [Tooltip("How far the head turns to follow the aim, in degrees (side, up and down).")]
        [SerializeField] private Vector2 headTurn = new Vector2(55f, 30f);

        [Header("Air tricks")]
        [Tooltip("Tricks start above this speed, in the air, with no rope out.")]
        [SerializeField] private float trickSpeed = 24f;
        [SerializeField] private float trickStartDelay = 0.2f;
        [SerializeField] private float trickTurnRate = 620f;
        [Tooltip("Distance from the model's feet to its middle, the point it spins around.")]
        [SerializeField] private float pivotHeight = 1f;

        private PlayerStats stats;
        private float walkPhase, lean, twist, idleTime;
        private Quaternion bodyRotation = Quaternion.identity;
        private float trickAngle, trickTimer;
        private Vector3 trickAxis = Vector3.right;
        private bool tricking, wasAirborne = true;
        // Squash (positive) and stretch (negative) on a spring; the cape on another.
        private float squash, squashVelocity, capeAngle, capeVelocity;
        // Short poses, 1 when they start and falling to 0.
        private float dashPose, tuckPose, hurtPose, climbPose, throwPose;
        private Vector2 headLook;

        public event System.Action TrickStarted;

        private void Awake()
        {
            if (controller != null) stats = controller.GetComponent<PlayerStats>();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Landed += OnLanded;
                controller.AirJumped += OnTuck;
                controller.WallJumped += OnTuck;
                controller.Dashed += OnDash;
                controller.LedgeClimbed += OnClimb;
            }
            if (hook != null) hook.Thrown += OnThrow;
            if (stats != null) stats.Damaged += OnHurt;
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Landed -= OnLanded;
                controller.AirJumped -= OnTuck;
                controller.WallJumped -= OnTuck;
                controller.Dashed -= OnDash;
                controller.LedgeClimbed -= OnClimb;
            }
            if (hook != null) hook.Thrown -= OnThrow;
            if (stats != null) stats.Damaged -= OnHurt;
        }

        private void OnLanded(float fallSpeed) => squashVelocity += Mathf.Min(maxSquash, fallSpeed * landSquash) * 22f;
        private void OnTuck() { tuckPose = 1f; squashVelocity -= 4f; }
        private void OnDash() => dashPose = 1f;
        private void OnClimb() => climbPose = 1f;
        private void OnThrow() => throwPose = 1f;
        private void OnHurt(float amount) => hurtPose = 1f;

        private void LateUpdate()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // paused: hold the pose
            Vector3 velocity = controller.Body.linearVelocity;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float speed = velocity.magnitude;
            bool airborne = controller.IsAirborne;
            bool roped = hook != null && (hook.IsAttached || hook.IsZipping);
            bool zipping = hook != null && hook.IsZipping;

            dashPose = Mathf.MoveTowards(dashPose, 0f, dt / 0.3f);
            tuckPose = Mathf.MoveTowards(tuckPose, 0f, dt / 0.45f);
            hurtPose = Mathf.MoveTowards(hurtPose, 0f, dt / 0.25f);
            climbPose = Mathf.MoveTowards(climbPose, 0f, dt / 0.45f);
            throwPose = Mathf.MoveTowards(throwPose, 0f, dt / 0.2f);
            if (wasAirborne && !airborne) idleTime = 0f;
            if (!wasAirborne && airborne && velocity.y > 2f) squashVelocity -= 5f;   // a jump: stretch
            wasAirborne = airborne;

            // ---- Body: face the way of travel, hang from the rope, lean with speed.
            Vector3 up = Vector3.up;
            if (roped)
            {
                Vector3 toAnchor = hook.AnchorPoint - controller.transform.position;
                if (toAnchor.sqrMagnitude > 1f) up = Vector3.Slerp(Vector3.up, toAnchor.normalized, zipping ? Mathf.Min(1f, ropeHang + 0.2f) : ropeHang);
            }
            Vector3 face = flat.sqrMagnitude > 1f ? flat : cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up) : transform.forward;
            face = Vector3.ProjectOnPlane(face, up);
            float walk = !airborne ? Mathf.Clamp01(flat.magnitude / 7f) : 0f;
            if (face.sqrMagnitude > 0.001f)
            {
                float targetLean = roped ? 0f : airborne ? Mathf.Clamp(flat.magnitude * 1.2f, 0f, maxLean) : walk * 9f;
                targetLean = Mathf.Lerp(targetLean, 72f, dashPose) - hurtPose * 16f;
                lean = Mathf.Lerp(lean, targetLean, dt * 7f);
                twist = Mathf.Lerp(twist, Mathf.Sin(walkPhase) * 7f * walk, dt * 12f);
                Quaternion target = Quaternion.LookRotation(face.normalized, up) * Quaternion.Euler(lean, twist, 0f);
                bodyRotation = Quaternion.Slerp(bodyRotation, target, dt * turnSpeed);
            }
            UpdateTrick(dt, airborne, speed);

            // ---- Squash and stretch on a spring; the model's origin is at its feet, so they stay planted.
            squashVelocity += (-squash * 190f - squashVelocity * 16f) * dt;
            squash = Mathf.Clamp(squash + squashVelocity * dt, -0.3f, maxSquash);
            float breathe = !airborne && walk < 0.05f ? Mathf.Sin(Time.time * 2.1f) * 0.012f : 0f;
            transform.localScale = new Vector3(1f + squash * 0.5f, 1f - squash + breathe, 1f + squash * 0.5f);

            // Spin around the middle of the body, not the feet; bob with the steps.
            float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.07f * walk;
            Quaternion spin = bodyRotation * Quaternion.AngleAxis(trickAngle, trickAxis);
            transform.rotation = spin;
            transform.localPosition = -(spin * Vector3.up) * pivotHeight + Vector3.up * bob;

            // ---- Legs.
            walkPhase += dt * walkCycleSpeed * Mathf.Max(walk, 0.0001f);
            float stride = Mathf.Sin(walkPhase) * walkSwing * walk;
            float legL, legR;
            if (!airborne) { legL = stride; legR = -stride; }
            else if (roped)
            {
                // Kick through the bottom of the swing: forward speed throws the legs ahead.
                float kick = Mathf.Clamp(Vector3.Dot(velocity, face.normalized) * 1.5f, -30f, 48f);
                legL = -kick;
                legR = -kick * 0.6f + 10f;
            }
            else if (velocity.y < -6f) { legL = 34f; legR = -22f; }   // falling: a split, one ahead and one behind
            else { legL = 26f; legR = 15f; }                          // rising or gliding: trailing
            legL = Mathf.Lerp(Mathf.Lerp(legL, 58f, dashPose), -72f, tuckPose);
            legR = Mathf.Lerp(Mathf.Lerp(legR, 52f, dashPose), -58f, tuckPose);
            Pose(legLeft, legL, 0f, dt);
            Pose(legRight, legR, 0f, dt);

            // ---- Arms: the hook arm points at the anchor while the hook is out; the other balances.
            float armSwing = stride * 0.9f;
            if (hookArm != null)
            {
                if (hook != null && hook.IsOut) PointAt(hookArm, hook.AnchorPoint, dt);
                else Pose(hookArm, Mathf.Lerp(Mathf.Lerp(-armSwing + (airborne ? -22f : 0f), 62f, dashPose), -165f, climbPose) - throwPose * 40f, 0f, dt);
            }
            if (otherArm != null)
            {
                if (zipping) PointAt(otherArm, hook.AnchorPoint, dt);   // both hands on the rope
                else
                {
                    bool falling = airborne && !roped && velocity.y < -6f;
                    float swing = !airborne ? armSwing : falling ? -18f : roped ? 24f : -50f;
                    float spread = falling ? 62f : roped ? 26f : 0f;
                    Pose(otherArm, Mathf.Lerp(Mathf.Lerp(swing, 64f, dashPose), -165f, climbPose), Mathf.Lerp(spread, 0f, Mathf.Max(dashPose, climbPose)), dt);
                }
            }

            // ---- Cape and scarf: a spring towards an angle set by speed and by rising or falling.
            if (cape != null)
            {
                float flutter = Mathf.Sin(Time.time * (6f + speed * 0.3f)) * Mathf.Min(10f, speed * 0.5f);
                float capeTarget = -Mathf.Clamp(8f + speed * 2.2f - velocity.y * 1.2f, 0f, 96f) + flutter;
                capeVelocity += ((capeTarget - capeAngle) * 60f - capeVelocity * 9f) * dt;
                capeAngle += capeVelocity * dt;
                cape.localRotation = Quaternion.Euler(capeAngle, 0f, 0f);
                if (scarf != null) scarf.localRotation = Quaternion.Euler(capeAngle * 0.8f + Mathf.Sin(Time.time * 11f) * Mathf.Min(14f, speed), 0f, 0f);
            }

            // ---- Head: looks where the player aims, within what a neck allows; standing idle it glances about.
            if (head != null)
            {
                idleTime = !airborne && walk < 0.05f ? idleTime + dt : 0f;
                Vector2 look = Vector2.zero;
                if (cameraTransform != null)
                {
                    Vector3 local = Quaternion.Inverse(bodyRotation) * cameraTransform.forward;
                    look.x = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -headTurn.x, headTurn.x);
                    look.y = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -headTurn.y, headTurn.y);
                }
                if (idleTime > 1.5f) look.x += Mathf.Sin(Time.time * 0.7f) * 16f;
                if (tricking) look = Vector2.zero;
                headLook = Vector2.Lerp(headLook, look, dt * 8f);
                head.localRotation = Quaternion.Euler(headLook.y - hurtPose * 14f, headLook.x, 0f);
            }
        }

        private void UpdateTrick(float dt, bool airborne, float speed)
        {
            bool canTrick = airborne && speed >= trickSpeed && (hook == null || !hook.IsOut);
            trickTimer = canTrick ? trickTimer + dt : 0f;
            if (!tricking && trickTimer >= trickStartDelay)
            {
                tricking = true;
                int kind = Random.Range(0, 3);   // front flip, back flip, barrel roll
                trickAxis = kind == 0 ? Vector3.right : kind == 1 ? Vector3.left : Vector3.forward;
                TrickStarted?.Invoke();
            }
            if (tricking)
            {
                trickAngle += trickTurnRate * dt;
                // Stop only at a whole turn, so the Warden never snaps out of a flip.
                if (!canTrick && trickAngle >= 360f) tricking = false;
                if (trickAngle >= 360f) trickAngle -= 360f;
                if (!tricking) trickAngle = 0f;
            }
            else if (trickAngle != 0f)
            {
                trickAngle = 0f;
            }
        }

        /// <summary>Eases a limb towards a swing (about X: forward is negative) and a spread (about Z, away from the body).</summary>
        private void Pose(Transform limb, float swing, float spread, float dt)
        {
            if (limb == null) return;
            // The left limbs spread the other way round.
            float side = limb == otherArm || limb == legLeft ? 1f : -1f;
            limb.localRotation = Quaternion.Slerp(limb.localRotation, Quaternion.Euler(swing, 0f, spread * side), dt * 18f);
        }

        /// <summary>Points an arm (which hangs along its own -Y) at a world position.</summary>
        private static void PointAt(Transform arm, Vector3 target, float dt)
        {
            Vector3 dir = target - arm.position;
            if (dir.sqrMagnitude < 0.01f) return;
            arm.rotation = Quaternion.Slerp(arm.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(-90f, 0f, 0f), dt * 20f);
        }
    }
}
