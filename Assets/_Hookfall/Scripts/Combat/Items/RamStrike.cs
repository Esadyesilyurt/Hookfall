// Hookfall - Ram Strike: above 20 m/s, touching an enemy deals speed × 1.5 damage; +20% damage per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class RamStrike : ItemBase
    {
        [SerializeField] private float minSpeed = 20f;
        [SerializeField] private float speedToDamage = 1.5f;
        [SerializeField] private float damagePerLevel = 0.2f;
        [SerializeField] private float contactRadius = 1.6f;
        [Tooltip("The same enemy can be rammed again after this many seconds.")]
        [SerializeField] private float rehitDelay = 0.5f;
        [SerializeField] private Material auraMaterial;

        private readonly List<EnemyBase> touching = new List<EnemyBase>();
        private readonly Dictionary<EnemyBase, float> lastHit = new Dictionary<EnemyBase, float>();
        private PlayerController player;
        private GameObject aura;
        private bool visualsOn;

        private float LevelFactor(int level) => 1f + damagePerLevel * (level - 1);

        protected override void Awake()
        {
            base.Awake();
            player = GetComponentInParent<PlayerController>();
            aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aura.name = "Ram Aura";
            aura.layer = Hookfall.Core.Layers.NoOutline;
            Destroy(aura.GetComponent<Collider>());
            aura.transform.SetParent(transform, false);
            aura.transform.localScale = new Vector3(1.6f, 1.6f, 2.6f);
            var r = aura.GetComponent<Renderer>();
            r.sharedMaterial = auraMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aura.SetActive(false);
        }

        protected override void Attack() { }   // contact damage is checked every frame instead

        protected override void OnActiveTick(float deltaTime)
        {
            float speed = player != null ? player.Speed : 0f;
            bool ramming = speed >= minSpeed;
            aura.SetActive(visualsOn && ramming);
            if (!ramming) return;
            aura.transform.rotation = Quaternion.LookRotation(player.Body.linearVelocity);

            float damage = speed * speedToDamage * LevelFactor(Level) * (Stats != null ? Stats.DamageMultiplier : 1f);
            EnemyQuery.InRadius(transform.position, contactRadius, touching);
            foreach (EnemyBase enemy in touching)
            {
                if (lastHit.TryGetValue(enemy, out float t) && Time.time - t < rehitDelay) continue;
                lastHit[enemy] = Time.time;
                enemy.TakeDamage(damage);
            }
            if (lastHit.Count > 400) lastHit.Clear();   // pooled enemies come back; keep the table small
        }

        public override void SetActiveVisuals(bool active)
        {
            visualsOn = active;
            if (!active && aura != null) aura.SetActive(false);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Damage = speed × {0}", (speedToDamage * LevelFactor(Level)).ToString("0.0"));
            yield return Loc.F("Above {0} m/s", minSpeed.ToString("0"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("Damage = speed × {0} → × {1}", (speedToDamage * LevelFactor(Level)).ToString("0.0"), (speedToDamage * LevelFactor(Level + 1)).ToString("0.0"));
    }
}
