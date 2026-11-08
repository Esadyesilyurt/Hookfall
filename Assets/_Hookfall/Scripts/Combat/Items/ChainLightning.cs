// Hookfall - Chain Lightning (Rare, added 2026-11-08): strikes the nearest enemy up to 30 m away, then jumps on to the
// next ones close by, losing a little power each jump. One more jump per level.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.Combat.Items
{
    public class ChainLightning : ItemBase
    {
        [SerializeField] private float range = 30f;
        [SerializeField] private float jumpRange = 9f;
        [SerializeField] private int baseJumps = 2;
        [Tooltip("Damage kept on each jump.")]
        [SerializeField] private float falloff = 0.85f;
        [SerializeField] private Material boltMaterial;
        [SerializeField] private float flashTime = 0.15f;

        private readonly HashSet<EnemyBase> hit = new HashSet<EnemyBase>();
        private readonly List<Vector3> points = new List<Vector3>();
        private LineRenderer line;
        private float flash;

        private int Jumps(int level) => baseJumps + level - 1;

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Lightning") { layer = Layers.NoOutline };
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = boltMaterial;
            line.widthMultiplier = 0.18f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.enabled = false;
        }

        private void Update()
        {
            if (line.enabled && (flash -= Time.deltaTime) <= 0f) line.enabled = false;
        }

        protected override void Attack()
        {
            EnemyBase target = EnemyQuery.Nearest(transform.position, range);
            if (target == null) return;
            hit.Clear();
            points.Clear();
            points.Add(transform.position + Vector3.up * 0.6f);
            float damage = Damage;
            for (int j = 0; j <= Jumps(Level) && target != null; j++)
            {
                Vector3 at = target.transform.position;
                points.Add(at);
                hit.Add(target);
                target.TakeDamage(damage);
                damage *= falloff;
                target = EnemyQuery.Nearest(at, jumpRange, hit);
            }
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
            line.enabled = true;
            flash = flashTime;
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Range {0} m", range.ToString("0"));
            yield return Loc.F("Jumps to {0} more", Jumps(Level));
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} jumps", Jumps(Level), Jumps(Level + 1));
    }
}
