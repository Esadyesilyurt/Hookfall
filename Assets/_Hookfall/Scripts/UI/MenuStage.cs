// Hookfall - the scene behind the main menu (2026-11-11): the camera stands on top of the tower, the chosen
// character keeps the watch there (breathing, looking round, cape in the wind) and bats and a wyvern circle in the
// evening sky. Replaces the far view of the whole map with a slow camera sway.
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using Hookfall.World;
using UnityEngine;

namespace Hookfall.UI
{
    public class MenuStage : MonoBehaviour
    {
        [Tooltip("One model per character, named by the character's id (set by the scene builder).")]
        [SerializeField] private GameObject[] models = new GameObject[0];
        [Tooltip("Models that circle the tower in the distance (bats, a wyvern).")]
        [SerializeField] private Transform[] flyers = new Transform[0];

        [Header("Where things stand, from the top of the tower")]
        [SerializeField] private Vector3 characterOffset = new Vector3(1.0f, 0f, -2.3f);
        [SerializeField] private float characterYaw = 305f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(-2.2f, 1.75f, 0.6f);
        [Tooltip("The point the camera looks at, from the character's feet.")]
        [SerializeField] private Vector3 lookOffset = new Vector3(-1.1f, 1.3f, 0f);
        [Tooltip("How far the camera swings to each side, in degrees, and the seconds for one swing there and back.")]
        [SerializeField] private float swing = 2.5f;
        [SerializeField] private float period = 28f;

        private static MenuStage instance;
        private Transform cam;
        private Vector3 feet, look, camHome;
        private bool ready;
        private Transform shownModel, head, cape, armLeft, armRight;
        private Vector3 modelHome;
        private Transform[] wingLeft, wingRight;

        /// <summary>Puts a character on the tower (the one being looked at in the character select).</summary>
        public static void Show(string id)
        {
            if (instance != null) instance.ShowModel(id);
        }

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Start()
        {
            // The island generator builds the tower in its Awake, so it stands by now.
            cam = Camera.main != null ? Camera.main.transform : transform;
            var generator = FindAnyObjectByType<IslandGenerator>();
            Vector3 top = generator != null ? generator.TowerTop : new Vector3(0f, 20f, 0f);
            feet = top + characterOffset;
            look = feet + lookOffset;
            camHome = top + cameraOffset;
            foreach (GameObject model in models)
            {
                if (model == null) continue;
                model.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, characterYaw, 0f));
            }
            wingLeft = new Transform[flyers.Length];
            wingRight = new Transform[flyers.Length];
            for (int i = 0; i < flyers.Length; i++)
            {
                if (flyers[i] == null) continue;
                wingLeft[i] = Find(flyers[i], "Wing_L");   // a model without wings just glides
                wingRight[i] = Find(flyers[i], "Wing_R");
            }
            ready = true;
            ShowModel(CharacterRoster.SelectedId);
        }

        private void ShowModel(string id)
        {
            if (!ready) return;
            GameObject found = null;
            foreach (GameObject model in models)
            {
                if (model == null) continue;
                bool on = model.name == id;
                model.SetActive(on);
                if (on) found = model;
            }
            if (found == null && models.Length > 0 && models[0] != null) { found = models[0]; found.SetActive(true); }
            if (found == null) return;
            shownModel = found.transform;
            modelHome = feet;
            head = Find(shownModel, "Head");
            cape = Find(shownModel, "Cape");
            armLeft = Find(shownModel, "Arm_L");
            armRight = Find(shownModel, "Arm_R");
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private void LateUpdate()
        {
            if (!ready) return;
            float time = Time.unscaledTime;

            // The camera: a slow sway round the watcher.
            float a = Mathf.Sin(time * Mathf.PI * 2f / period) * swing;
            Vector3 p = look + Quaternion.AngleAxis(a, Vector3.up) * (camHome - look);
            p.y += Mathf.Sin(time * 0.45f) * 0.12f;
            cam.SetPositionAndRotation(p, Quaternion.LookRotation(look - p));

            // The watcher: breathing, a look round now and then, cape and arms in the wind.
            if (shownModel != null)
            {
                shownModel.position = modelHome + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.025f);
                if (head != null) head.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.7f) * 3f, Mathf.Sin(time * 0.31f) * 28f, 0f);
                if (cape != null) cape.localRotation = Quaternion.Euler(14f + Mathf.Sin(time * 2.1f) * 7f + Mathf.Sin(time * 3.4f) * 3f, 0f, Mathf.Sin(time * 1.3f) * 4f);
                if (armLeft != null) armLeft.localRotation = Quaternion.Euler(Mathf.Sin(time * 1.1f) * 4f, 0f, 0f);
                if (armRight != null) armRight.localRotation = Quaternion.Euler(-Mathf.Sin(time * 1.1f) * 4f, 0f, 0f);
            }

            // The sky: each flyer on its own circle round the tower, banking into it.
            for (int i = 0; i < flyers.Length; i++)
            {
                Transform f = flyers[i];
                if (f == null) continue;
                float radius = 26f + i * 7f % 40f, speed = (0.22f - i * 0.011f) * (i % 2 == 0 ? 1f : -1f);
                float angle = time * speed + i * 1.9f;
                Vector3 at = look + new Vector3(Mathf.Cos(angle) * radius, 3f + (i * 5f % 17f) - 6f + Mathf.Sin(time * 0.9f + i) * 1.5f, -55f + Mathf.Sin(angle) * radius);
                Vector3 ahead = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * Mathf.Sign(speed);
                f.SetPositionAndRotation(at, Quaternion.LookRotation(ahead) * Quaternion.Euler(0f, 0f, -18f * Mathf.Sign(speed)));
                float flap = Mathf.Sin(time * 9f + i) * 32f;
                if (wingLeft[i] != null) wingLeft[i].localRotation = Quaternion.Euler(0f, 0f, flap);
                if (wingRight[i] != null) wingRight[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }
    }
}
