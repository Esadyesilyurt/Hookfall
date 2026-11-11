// Hookfall - first-run hints (added 2026-11-11): on the very first run a line over the crosshair says what to do
// next - throw the hook, let go and fly, jump and dash in the air, open a chest - and moves on when the player has
// done it. Once all four are through it never shows again (PlayerPrefs "hf_hints_done").
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using TMPro;
using UnityEngine;

namespace Hookfall.UI
{
    public class HintGuide : MonoBehaviour
    {
        public const string DoneKey = "hf_hints_done";

        [SerializeField] private GrappleHook hook;
        [SerializeField] private PlayerController controller;
        [SerializeField] private CombatSystem combat;
        [Tooltip("Seconds of flight with the attacks on before the flying hint counts as learnt.")]
        [SerializeField] private float flightNeeded = 2.5f;
        [Tooltip("Seconds a hint that needs no action stays (and the longest the air-move hint waits).")]
        [SerializeField] private float readTime = 10f;

        private static readonly string[] Lines =
        {
            "Aim at an island and hold the left mouse button to throw the hook",
            "Let go to fly. Your weapons fire only while you are in the air",
            "In the air: Space jumps again, Shift dashes",
            "Gold opens chests: stand next to one and press E",
        };

        private CanvasGroup group;
        private TextMeshProUGUI text;
        private int step;
        private float timer;
        private bool acted;

        private void Awake()
        {
            if (PlayerPrefs.GetInt(DoneKey, 0) != 0) { enabled = false; return; }
            Canvas canvas = UIFactory.Canvas(transform, "Hint Canvas", 2);
            RectTransform plaque = UIFactory.Centre(canvas.transform, "Hint", new Vector2(0f, 190f), new Vector2(900f, 58f));
            UIFactory.Image(plaque, Color.white, UISkin.Plaque);
            group = plaque.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            text = UIFactory.Text(plaque, "Text", Lines[0], 24f, UITheme.Text);
            UIFactory.Stretch((RectTransform)text.transform, 8f);
            text.fontStyle = FontStyles.Bold;
            UIFactory.FitText(text, 16f);
        }

        private void OnEnable()
        {
            if (hook != null) hook.Attached += OnAct0;
            if (controller != null) { controller.AirJumped += OnAct2; controller.Dashed += OnAct2; }
        }

        private void OnDisable()
        {
            if (hook != null) hook.Attached -= OnAct0;
            if (controller != null) { controller.AirJumped -= OnAct2; controller.Dashed -= OnAct2; }
        }

        private void OnAct0() { if (step == 0) acted = true; }
        private void OnAct2() { if (step == 2) acted = true; }

        private void Update()
        {
            if (group == null) return;
            GameManager gm = GameManager.Instance;
            bool playing = gm == null || gm.State == RunState.Playing;
            group.alpha = Mathf.MoveTowards(group.alpha, playing ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (!playing) return;

            switch (step)
            {
                case 1: if (combat != null && combat.AttacksActive) timer += Time.deltaTime; acted = timer >= flightNeeded; break;
                case 2: timer += Time.deltaTime; acted |= timer >= readTime; break;
                case 3: timer += Time.deltaTime; acted = timer >= readTime; break;
            }
            if (!acted) return;
            acted = false;
            timer = 0f;
            step++;
            if (step < Lines.Length) { text.text = Loc.T(Lines[step]); group.alpha = 0f; return; }
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            Destroy(group.transform.parent.gameObject);
            group = null;
            enabled = false;
        }
    }
}
