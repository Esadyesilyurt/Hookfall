// Hookfall - spawns the horde ahead of and around the player and scales difficulty over time (GDD 4.8, 5.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class EnemyDirector : MonoBehaviour
    {
        [SerializeField] private EnemyBase batPrefab;
        [SerializeField] private PlayerController player;

        [Header("Spawning")]
        [SerializeField] private float baseSpawnPerSecond = 2f;
        [Tooltip("+15% spawn rate per minute survived.")]
        [SerializeField] private float spawnGrowthPerMinute = 0.15f;
        [SerializeField] private int maxAlive = 300;
        [SerializeField] private float spawnDistance = 60f;
        [Tooltip("Share of spawns placed ahead of the player's flight path.")]
        [SerializeField, Range(0f, 1f)] private float aheadSpawnShare = 0.6f;

        [Header("Scaling")]
        [SerializeField] private float healthGrowthPerMinute = 0.15f;
        [SerializeField] private float damageGrowthPerMinute = 0.08f;

        private readonly List<EnemyBase> alive = new List<EnemyBase>();
        private float spawnBudget;

        public int AliveCount => alive.Count;

        private float MinutesSurvived =>
            (GameManager.Instance != null ? GameManager.Instance.RunTime : Time.timeSinceLevelLoad) / 60f;

        private void Update()
        {
            if (player == null || batPrefab == null) return;

            float minutes = MinutesSurvived;
            float spawnRate = baseSpawnPerSecond * Mathf.Pow(1f + spawnGrowthPerMinute, minutes);
            spawnBudget += spawnRate * Time.deltaTime;

            while (spawnBudget >= 1f && alive.Count < maxAlive)
            {
                spawnBudget -= 1f;
                Spawn(batPrefab, minutes);
            }
            if (alive.Count >= maxAlive) spawnBudget = 0f;   // fallback: cap reached, pause spawning
        }

        private void Spawn(EnemyBase prefab, float minutes)
        {
            // TODO (Week 11): object pooling instead of Instantiate/Destroy.
            EnemyBase enemy = Instantiate(prefab, PickSpawnPoint(), Quaternion.identity, transform);
            enemy.Init(this, player,
                1f + healthGrowthPerMinute * minutes,
                1f + damageGrowthPerMinute * minutes);
            alive.Add(enemy);
        }

        /// <summary>Ahead of the player's flight path most of the time, otherwise anywhere around them.</summary>
        private Vector3 PickSpawnPoint()
        {
            Vector3 velocity = player.Body.linearVelocity;
            Vector3 direction;

            if (velocity.sqrMagnitude > 25f && Random.value < aheadSpawnShare)
                direction = Quaternion.AngleAxis(Random.Range(-60f, 60f), Vector3.up) * velocity.normalized;
            else
                direction = Random.onUnitSphere;

            direction.y = Mathf.Clamp(direction.y, -0.3f, 0.5f);
            return player.transform.position + direction.normalized * spawnDistance;
        }

        public void Relocate(EnemyBase enemy) => enemy.transform.position = PickSpawnPoint();

        public void OnEnemyRemoved(EnemyBase enemy) => alive.Remove(enemy);
    }
}
