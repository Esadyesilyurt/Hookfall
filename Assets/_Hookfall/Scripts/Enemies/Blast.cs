// Hookfall - a burst waiting to go off (added 2026-11-10): three red rings mark its reach and close in for a moment
// (the warning), then everything inside is hurt, the Warden and the enemies alike, which can set off more bursts.
// Used by the Blightpuff and by Explosive elites; pooled by the EnemyDirector.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Blast : MonoBehaviour
    {
        private const int Points = 28;
        private static readonly List<EnemyBase> Caught = new List<EnemyBase>();

        private readonly LineRenderer[] rings = new LineRenderer[3];
        private EnemyDirector director;
        private PlayerStats player;
        private float radius, playerDamage, enemyDamage, delay, timer;

        /// <summary>Called once when the pool makes it.</summary>
        public void Build(Material material)
        {
            for (int i = 0; i < rings.Length; i++)
            {
                var go = new GameObject("Ring") { layer = Hookfall.Core.Layers.NoOutline };
                go.transform.SetParent(transform, false);
                // One level ring and two upright ones: a cage that reads as a sphere from any side.
                go.transform.localRotation = i == 0 ? Quaternion.identity : Quaternion.Euler(90f, i == 1 ? 0f : 90f, 0f);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = Points;
                line.startWidth = line.endWidth = 0.16f;
                line.sharedMaterial = material;
                line.startColor = line.endColor = new Color(1f, 0.3f, 0.12f);
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                for (int p = 0; p < Points; p++)
                {
                    float a = p / (float)Points * Mathf.PI * 2f;
                    line.SetPosition(p, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
                }
                rings[i] = line;
            }
        }

        public void Begin(EnemyDirector owner, PlayerStats target, Vector3 position, float reach, float hurtPlayer, float hurtEnemies, float fuse)
        {
            director = owner;
            player = target;
            transform.position = position;
            radius = reach;
            playerDamage = hurtPlayer;
            enemyDamage = hurtEnemies;
            delay = Mathf.Max(0.01f, fuse);
            timer = 0f;
            transform.localScale = Vector3.one * radius;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            // The rings pulse faster as the moment comes.
            float t = Mathf.Clamp01(timer / delay);
            transform.localScale = Vector3.one * (radius * (1f - 0.12f * Mathf.Abs(Mathf.Sin(t * t * 14f))));
            if (timer >= delay) GoOff();
        }

        private void GoOff()
        {
            Vector3 centre = transform.position;
            if (player != null && (player.transform.position - centre).sqrMagnitude <= radius * radius)
                player.TakeDamageFrom(playerDamage, centre);
            // Collect first: a hit can kill, and a death changes the list being walked.
            Caught.Clear();
            foreach (EnemyBase enemy in EnemyBase.Active)
            {
                float reach = radius + enemy.HitRadius;
                if (!enemy.IsBoss && (enemy.transform.position - centre).sqrMagnitude <= reach * reach) Caught.Add(enemy);
            }
            foreach (EnemyBase enemy in Caught) enemy.TakeDamage(enemyDamage);
            director.FinishBlast(this, radius);
        }
    }
}
