// Hookfall - short cutscenes (2026-11-08): the boss's entrance and the trip through the portal. The world stands still
// (GameManager's Cutscene state stops time), the camera leaves the Cinemachine rig for a few seconds, black bars slide
// in and a title card names what happens. Everything here runs in unscaled time; its own particles too, so dust and
// sparks move while the world is frozen. Space, Enter or Esc skips to the end.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using Hookfall.Audio;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.UI;
using Hookfall.World;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Hookfall.Feel
{
    public class CutsceneDirector : MonoBehaviour
    {
        private static readonly Color BossColor = new Color(1f, 0.3f, 0.85f);
        private static readonly Color DustColor = new Color(0.62f, 0.58f, 0.66f);
        private static readonly Color PortalColor = new Color(0.45f, 0.95f, 1f);

        [SerializeField] private Camera cam;
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private PlayerController player;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private Material particleMaterial;

        [Header("Boss entrance")]
        [SerializeField] private float shotDistance = 62f;
        [SerializeField] private float shotBelow = 16f;

        private GameObject root;
        private RectTransform topBar, bottomBar;
        private CanvasGroup card;
        private TextMeshProUGUI title, subtitle;
        private Image fade;
        private ParticleSystem bursts;
        private bool skip;
        private Vector3 camFrom;
        private Quaternion rotFrom;
        private float fovFrom;

        /// <summary>True while a cutscene plays (the HUD hides).</summary>
        public static bool Running { get; private set; }

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Cutscene Canvas", 40);
            root = canvas.gameObject;
            Transform c = canvas.transform;
            fade = UIFactory.Image(UIFactory.Fill(c, "Fade"), new Color(1f, 1f, 1f, 0f));
            topBar = UIFactory.Rect(c, "Top Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            UIFactory.Image(topBar, Color.black);
            bottomBar = UIFactory.Rect(c, "Bottom Bar", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 0f), new Vector2(0.5f, 0f));
            UIFactory.Image(bottomBar, Color.black);

            RectTransform cardRect = UIFactory.Rect(c, "Title Card", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1500f, 200f));
            card = cardRect.gameObject.AddComponent<CanvasGroup>();
            title = UIFactory.Text(UIFactory.Rect(cardRect, "Title", new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero),
                "", 84f, UITheme.Gold, TextAlignmentOptions.Center, true);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 14f;
            UIFactory.Image(UIFactory.Rect(cardRect, "Line", new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(620f, 3f)),
                new Color(1f, 0.85f, 0.45f, 0.8f));
            subtitle = UIFactory.Text(UIFactory.Rect(cardRect, "Subtitle", new Vector2(0f, 0f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero),
                "", 32f, UITheme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Text(UIFactory.Rect(c, "Skip", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 40f), new Vector2(420f, 40f), new Vector2(1f, 0.5f)),
                "Space: skip", 22f, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Right, true);

            bursts = CreateBursts();
            root.SetActive(false);
        }

        private void OnDestroy() => Running = false;

        private void Update()
        {
            if (!Running) return;
            Keyboard k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame)) skip = true;
        }

        // ---------- The boss's entrance ----------

        /// <summary>
        /// The camera flies to the arena, the boss's islands rise, the boss falls from the sky, lands, roars and is named;
        /// then the camera comes back to the Warden. About five seconds.
        /// </summary>
        public IEnumerator BossEntrance(Boss boss, Transform arena, Vector3 rest, string bossName, string bossTitle)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.BeginCutscene())
            {
                boss.Descend(rest, 1f);
                yield break;
            }
            Begin();
            Vector3 toPlayer = player.transform.position - rest;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 1f ? toPlayer.normalized : Vector3.back;
            Vector3 shot = rest + toPlayer * shotDistance + Vector3.down * shotBelow;
            Quaternion shotRotation = Quaternion.LookRotation(rest + Vector3.up * 3f - shot);
            Vector3 arenaTo = arena.position, arenaFrom = arenaTo + Vector3.down * 35f;
            arena.position = arenaFrom;
            boss.Face(shot);
            boss.Descend(rest, 0f);
            title.text = Loc.Upper(bossName);
            subtitle.text = bossTitle;

            const float total = 5.4f;
            bool landed = false, roared = false;
            float t = 0f, shake = 0f;
            while (t < total)
            {
                float dt = Time.unscaledDeltaTime;
                if (skip && t < 4.3f) { t = 4.3f; landed = roared = true; skip = false; }
                t += dt;
                Bars(Smooth(0f, 0.5f, t) * (1f - Smooth(4.6f, 5.3f, t)));
                SetCard(Smooth(2.9f, 3.4f, t) * (1f - Smooth(4.4f, 4.9f, t)));

                arena.position = Vector3.Lerp(arenaFrom, arenaTo, Smooth(0.3f, 2.4f, t));
                float fall = Mathf.Clamp01((t - boss.EntranceStart) / boss.EntranceTime);
                boss.Descend(rest, fall);
                if (!landed && fall >= 1f)
                {
                    landed = true;
                    Burst(rest, 60, BossColor, 14f, 0.4f, 1f, false);
                    if (boss.LandsHeavy)
                    {
                        // The Colossus crashes down; a boss that flies in just arrives.
                        shake = 1.3f;
                        Burst(rest + Vector3.down * 7f, 140, DustColor, 24f, 0.7f, 1.4f, true);
                        if (audioManager != null) audioManager.Play(SoundId.BossSlam, rest);
                    }
                }
                if (!roared && t >= 2.6f)
                {
                    roared = true;
                    boss.Roar(1.7f);
                    shake = Mathf.Max(shake, 1f);
                    Burst(boss.CorePosition, 90, BossColor, 20f, 0.5f, 1.1f, true);
                }
                boss.Animate(dt);

                float go = Smooth(0f, 1.2f, t), back = Smooth(4.3f, 5.3f, t);
                Vector3 position = Vector3.Lerp(Vector3.Lerp(camFrom, shot, go), camFrom, back);
                Quaternion rotation = Quaternion.Slerp(Quaternion.Slerp(rotFrom, shotRotation, go), rotFrom, back);
                float push = Smooth(2.6f, 3.8f, t) * (1f - back);
                cam.fieldOfView = Mathf.Lerp(Mathf.Lerp(fovFrom, 58f, go) - 9f * push, fovFrom, back);
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.2f);
                cam.transform.SetPositionAndRotation(position + Random.insideUnitSphere * (shake * 0.7f), rotation);
                yield return null;
            }
            arena.position = arenaTo;
            boss.Descend(rest, 1f);
            End(false);
            if (gm != null) gm.EndCutscene();
        }

        // ---------- Through the portal ----------

        /// <summary>
        /// The camera dives into the portal and the screen goes white; midpoint builds the next stage behind the white;
        /// the white fades over the new map with the stage's title card. About four and a half seconds.
        /// </summary>
        public IEnumerator PortalTravel(Portal portal, Action midpoint, string titleText, string subtitleText)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.BeginCutscene())
            {
                midpoint();
                yield break;
            }
            Begin();
            if (audioManager != null) audioManager.Play(SoundId.PortalEnter);
            Vector3 centre = portal != null ? portal.Centre : player.transform.position;
            Vector3 into = centre + (camFrom - centre).normalized * 1.2f;
            Quaternion facing = Quaternion.LookRotation(centre - camFrom);
            float t = 0f, nextSpark = 0f;
            while (t < 1.3f)
            {
                float dt = Time.unscaledDeltaTime;
                if (skip) { t = 1.3f; skip = false; }
                t += dt;
                float k = Smooth(0f, 1.3f, t);
                Bars(Smooth(0f, 0.4f, t));
                if (portal != null) portal.Surge(k);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(camFrom, into, k * k), Quaternion.Slerp(rotFrom, facing, Smooth(0f, 0.6f, t)));
                cam.fieldOfView = Mathf.Lerp(fovFrom, 105f, k * k);
                Fade(Smooth(0.75f, 1.25f, t));
                if (t >= nextSpark)
                {
                    nextSpark += 0.05f;
                    Burst(centre + Random.insideUnitSphere * 2.5f, 12, PortalColor, 7f, 0.25f, 0.6f, false);
                }
                yield return null;
            }
            Fade(1f);

            midpoint();   // the new stage is built behind the white
            yield return null;
            yield return null;
            // Back on the Warden's own camera, snapped to where they now stand.
            brain.IgnoreTimeScale = true;
            End(true);
            root.SetActive(true);
            Running = true;
            Fade(1f);
            Bars(1f);
            yield return null;
            yield return null;

            title.text = titleText;
            subtitle.text = subtitleText;
            if (audioManager != null) audioManager.Play(SoundId.StageStart);
            t = 0f;
            while (t < 3.4f)
            {
                if (skip && t < 2.6f) { t = 2.6f; skip = false; }
                t += Time.unscaledDeltaTime;
                Fade(1f - Smooth(0.1f, 1.1f, t));
                SetCard(Smooth(0.3f, 0.8f, t) * (1f - Smooth(2.6f, 3.3f, t)));
                Bars(1f - Smooth(2.7f, 3.4f, t));
                yield return null;
            }
            brain.IgnoreTimeScale = false;
            Fade(0f);
            SetCard(0f);
            Bars(0f);
            root.SetActive(false);
            Running = false;
            if (gm != null) gm.EndCutscene();
        }

        // ---------- Camera and screen ----------

        private void Begin()
        {
            Running = true;
            skip = false;
            root.SetActive(true);
            Fade(0f);
            SetCard(0f);
            Bars(0f);
            if (cam == null) cam = Camera.main;
            camFrom = cam.transform.position;
            rotFrom = cam.transform.rotation;
            fovFrom = cam.fieldOfView;
            if (brain != null) brain.enabled = false;   // the cutscene moves the camera itself
        }

        /// <summary>Gives the camera back to Cinemachine; snap skips its damping after a teleport.</summary>
        private void End(bool snap)
        {
            if (snap && playerCamera != null) playerCamera.PreviousStateIsValid = false;
            if (brain != null) brain.enabled = true;
            else cam.transform.SetPositionAndRotation(camFrom, rotFrom);
            Bars(0f);
            SetCard(0f);
            root.SetActive(false);
            Running = false;
        }

        private void Bars(float amount)
        {
            float h = 128f * amount;
            topBar.sizeDelta = new Vector2(0f, h);
            bottomBar.sizeDelta = new Vector2(0f, h);
        }

        private void SetCard(float alpha)
        {
            card.alpha = alpha;
            card.transform.localScale = Vector3.one * (1.06f - 0.06f * alpha);
        }

        private void Fade(float alpha) => fade.color = new Color(1f, 1f, 1f, alpha);

        private static float Smooth(float from, float to, float t)
        {
            float x = Mathf.Clamp01((t - from) / (to - from));
            return x * x * (3f - 2f * x);
        }

        // ---------- Particles that move while time stands still ----------

        private ParticleSystem CreateBursts()
        {
            var go = new GameObject("Cutscene Bursts") { layer = Layers.NoOutline };
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.gravityModifier = 0.4f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        private void Burst(Vector3 position, int count, Color color, float speed, float size, float life, bool flat)
        {
            var p = new ParticleSystem.EmitParams { position = position, startColor = color, startLifetime = life, applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (flat) dir = new Vector3(dir.x, dir.y * 0.25f, dir.z).normalized;
                p.velocity = dir * (speed * Random.Range(0.4f, 1f));
                p.startSize = size * Random.Range(0.6f, 1.4f);
                bursts.Emit(p, 1);
            }
        }
    }
}
