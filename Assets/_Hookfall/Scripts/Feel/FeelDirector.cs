// Hookfall - game feel: listens to the game's events and answers them with camera shake (Cinemachine Impulse),
// hit-stop, field-of-view kicks, post-processing pulses (vignette, chromatic aberration) and effect bursts.
// Added on 2026-11-07 after the first playtest found the game had no feedback at all.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections;
using Hookfall.Combat;
using Hookfall.Combat.Items;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.VFX;
using Hookfall.World;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hookfall.Feel
{
    public class FeelDirector : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GrappleHook hook;
        [SerializeField] private CombatSystem combat;
        [SerializeField] private LevelUpSystem levelUp;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private CinemachineImpulseSource impulse;
        [SerializeField] private Volume volume;
        [SerializeField] private VfxManager vfx;

        [Header("Shake strength")]
        [SerializeField] private float hookShake = 0.15f;
        [SerializeField] private float zipShake = 0.35f;
        [SerializeField] private float hurtShake = 0.6f;
        [SerializeField] private float bigKillShake = 0.4f;
        [SerializeField] private float slamShakeRange = 20f;

        [Header("Hit-stop (seconds of near-frozen time)")]
        [SerializeField] private float hurtStop = 0.06f;
        [SerializeField] private float bigKillStop = 0.04f;

        [Header("Field of view")]
        [SerializeField] private float zipKick = 14f;
        [SerializeField] private float landKick = -4f;

        [Header("Post-processing")]
        [SerializeField] private float baseVignette = 0.2f;
        [SerializeField] private float lowHealthVignette = 0.45f;
        [SerializeField, Range(0f, 1f)] private float lowHealthBelow = 0.3f;
        [SerializeField] private Color hurtColor = new Color(0.6f, 0.02f, 0.05f);
        [SerializeField] private Color stormColor = new Color(0.1f, 0.14f, 0.3f);

        private PlayerStats stats;
        private Vignette vignette;
        private ChromaticAberration chromatic;
        private float hurtFlash;
        private float aberration;
        private Coroutine stopRoutine;

        private void Awake()
        {
            if (player != null) stats = player.GetComponent<PlayerStats>();
            if (volume != null && volume.profile != null)
            {
                volume.profile.TryGet(out vignette);
                volume.profile.TryGet(out chromatic);
            }
        }

        private void OnEnable()
        {
            if (hook != null)
            {
                hook.Attached += OnHookAttached;
                hook.ZipStarted += OnZipStarted;
                hook.ZipEnded += OnZipEnded;
            }
            if (player != null) player.Landed += OnLanded;
            if (player != null) { player.WallJumped += OnWallJump; player.AirJumped += OnWallJump; player.LedgeClimbed += OnWallJump; player.Dashed += OnDash; }
            if (stats != null) stats.Damaged += OnHurt;
            if (combat != null) combat.AttacksChanged += OnAttacksChanged;
            if (levelUp != null) levelUp.OfferReady += OnLevelUp;
            EnemyDirector.Died += OnEnemyDied;
            EnemyDirector.Slammed += OnSlam;
            TetherCoil.ShockwaveFired += OnShockwave;
            HeartstoneCore.Pulsed += OnShockwave;
            SynergySystem.ChainFired += OnChain;
            if (stats != null) { stats.ShieldBlocked += OnShieldBlocked; stats.Revived += OnRevived; }
        }

        private void OnDisable()
        {
            if (hook != null)
            {
                hook.Attached -= OnHookAttached;
                hook.ZipStarted -= OnZipStarted;
                hook.ZipEnded -= OnZipEnded;
            }
            if (player != null) player.Landed -= OnLanded;
            if (player != null) { player.WallJumped -= OnWallJump; player.AirJumped -= OnWallJump; player.LedgeClimbed -= OnWallJump; player.Dashed -= OnDash; }
            if (stats != null) stats.Damaged -= OnHurt;
            if (combat != null) combat.AttacksChanged -= OnAttacksChanged;
            if (levelUp != null) levelUp.OfferReady -= OnLevelUp;
            EnemyDirector.Died -= OnEnemyDied;
            EnemyDirector.Slammed -= OnSlam;
            TetherCoil.ShockwaveFired -= OnShockwave;
            HeartstoneCore.Pulsed -= OnShockwave;
            SynergySystem.ChainFired -= OnChain;
            if (stats != null) { stats.ShieldBlocked -= OnShieldBlocked; stats.Revived -= OnRevived; }
            if (Time.timeScale > 0f && Time.timeScale < 1f) Time.timeScale = 1f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hurtFlash = Mathf.MoveTowards(hurtFlash, 0f, dt * 3f);
            aberration = Mathf.MoveTowards(aberration, 0f, dt * 2.5f);

            float health = stats != null ? stats.CurrentHealth / Mathf.Max(1f, stats.MaxHealth) : 1f;
            float low = health < lowHealthBelow ? (1f - health / lowHealthBelow) * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f)) : 0f;
            float storm = StormWall.Pressure;

            if (vignette != null)
            {
                vignette.intensity.value = baseVignette + Mathf.Max(low * (lowHealthVignette - baseVignette), hurtFlash * 0.3f) + storm * 0.25f;
                Color c = Color.Lerp(Color.black, hurtColor, Mathf.Max(low, hurtFlash));
                vignette.color.value = Color.Lerp(c, stormColor, storm);
            }
            if (chromatic != null) chromatic.intensity.value = Mathf.Clamp01(aberration + storm * 0.4f);
        }

        // ---------- Events ----------

        private void OnHookAttached()
        {
            Shake(hookShake);
            if (vfx != null) vfx.Burst(hook.AnchorPoint, 10, new Color(0.4f, 0.95f, 1f), 6f, 0.18f, 0.3f);
        }

        private void OnZipStarted()
        {
            Shake(zipShake);
            if (cameraRig != null) cameraRig.Kick(zipKick);
            aberration = 0.8f;
        }

        private void OnZipEnded()
        {
            if (cameraRig != null) cameraRig.Kick(-zipKick * 0.4f);
        }

        private void OnLanded(float fallSpeed)
        {
            if (fallSpeed < 6f) return;
            float strength = Mathf.InverseLerp(6f, 30f, fallSpeed);
            Shake(0.1f + strength * 0.4f);
            if (cameraRig != null) cameraRig.Kick(landKick * strength);
            if (vfx != null) vfx.Ring(player.transform.position + Vector3.down * 0.9f, 18, new Color(0.85f, 0.85f, 0.8f), 4f + strength * 6f);
        }

        private void OnWallJump()
        {
            Shake(0.15f);
            if (cameraRig != null) cameraRig.Kick(5f);
            if (vfx != null) vfx.Ring(player.transform.position, 14, new Color(0.85f, 0.85f, 0.8f), 5f);
        }

        private void OnDash()
        {
            Shake(0.12f);
            if (cameraRig != null) cameraRig.Kick(9f);
            if (vfx != null) vfx.Ring(player.transform.position, 16, new Color(0.6f, 0.9f, 1f), 6f);
        }

        private void OnHurt(float amount)
        {
            Shake(hurtShake);
            HitStop(hurtStop);
            hurtFlash = 1f;
            aberration = Mathf.Max(aberration, 0.6f);
        }

        private void OnEnemyDied(EnemyBase enemy, bool killed)
        {
            if (!killed || !(enemy.IsElite || enemy is Golem)) return;
            Shake(bigKillShake);
            HitStop(bigKillStop);
        }

        private void OnSlam(Golem golem)
        {
            float d = Vector3.Distance(golem.transform.position, player.transform.position);
            if (d < slamShakeRange) Shake(0.5f * (1f - d / slamShakeRange));
        }

        private void OnShockwave(Vector3 position, float radius)
        {
            Shake(0.5f);
            if (vfx != null) vfx.Ring(position, 32, new Color(0.4f, 0.95f, 1f), radius * 3f);
        }

        private void OnChain(Vector3 position, float radius, School school)
        {
            if (vfx != null) vfx.Ring(position, 24, SchoolInfo.Color(school), radius * 2.5f);
        }

        private void OnShieldBlocked()
        {
            Shake(0.2f);
            if (vfx != null) vfx.Ring(player.transform.position, 24, new Color(0.4f, 0.95f, 1f), 7f);
        }

        private void OnRevived()
        {
            Shake(0.7f);
            if (vfx != null) vfx.Burst(player.transform.position, 60, new Color(1f, 0.62f, 0.15f), 10f, 0.35f, 1.2f);
        }

        private void OnAttacksChanged(bool on)
        {
            if (on && vfx != null) vfx.Ring(player.transform.position, 20, new Color(0.4f, 0.95f, 1f), 7f);
        }

        private void OnLevelUp()
        {
            if (vfx != null) vfx.Burst(player.transform.position, 40, new Color(0.4f, 0.95f, 1f), 9f, 0.25f, 0.8f);
        }

        // ---------- Tools ----------

        public void Shake(float force)
        {
            if (impulse == null || force <= 0f) return;
            impulse.GenerateImpulseWithVelocity(Random.onUnitSphere * force);
        }

        /// <summary>Freezes time for a moment so a hit lands; never fights a pause or a level-up screen.</summary>
        public void HitStop(float seconds)
        {
            GameManager gm = GameManager.Instance;
            if (seconds <= 0f || (gm != null && gm.State != RunState.Playing) || Time.timeScale < 1f) return;
            if (stopRoutine != null) StopCoroutine(stopRoutine);
            stopRoutine = StartCoroutine(Stop(seconds));
        }

        private IEnumerator Stop(float seconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State == RunState.Playing) Time.timeScale = 1f;
            stopRoutine = null;
        }
    }
}
