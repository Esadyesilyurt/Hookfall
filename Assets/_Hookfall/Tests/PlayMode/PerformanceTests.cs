// Hookfall - the 300-enemy test from the production plan (Week 11): fills the arena to the enemy cap and measures
// frame times for 10 s while the Warden fights. Writes the numbers to the log, and to HOOKFALL_PERF_FILE when set.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hookfall.Tests
{
    public class PerformanceTests
    {
        private readonly RecordGuard record = new RecordGuard();

        [SetUp] public void SaveRecord() => record.Save();
        [TearDown] public void RestoreRecord() => record.Restore();

        [UnityTest]
        public IEnumerator ThreeHundredEnemies()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject player = GameObject.Find("Player");
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            var director = (Component)UnityEngine.Object.FindAnyObjectByType(directorType);
            directorType.GetField("baseSpawnPerSecond", flags).SetValue(director, 400f);
            directorType.GetField("maxAlive", flags).SetValue(director, 300);   // the game caps lower; the plan's target is 300
            var alive = directorType.GetProperty("AliveCount");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);

            void Keep()
            {
                statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 14f, -12f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(18f, 6f, 8f);
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
            }

            float until = Time.realtimeSinceStartup + 20f;
            while ((int)alive.GetValue(director) < 290 && Time.realtimeSinceStartup < until)
            {
                Keep();
                yield return null;
            }

            // Batch mode runs a 640x480 window; render the game camera at 1080p so the GPU cost is real.
            var target = new RenderTexture(1920, 1080, 24);
            Camera.main.targetTexture = target;

            var frames = new List<float>();
            int fewest = int.MaxValue, most = 0;
            until = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < until)
            {
                Keep();
                yield return null;
                frames.Add(Time.unscaledDeltaTime * 1000f);
                int count = (int)alive.GetValue(director);
                fewest = Mathf.Min(fewest, count);
                most = Mathf.Max(most, count);
            }

            Camera.main.targetTexture = null;
            UnityEngine.Object.Destroy(target);
            frames.Sort();
            float sum = 0f;
            foreach (float f in frames) sum += f;
            float avg = sum / frames.Count;
            float p95 = frames[(int)(frames.Count * 0.95f)];
            string report = $"[Hookfall] Perf: {fewest}-{most} enemies alive, {frames.Count} frames, average {avg:0.00} ms ({1000f / avg:0} FPS), " +
                            $"95th percentile {p95:0.00} ms ({1000f / p95:0} FPS), rendered at 1920x1080, GPU {SystemInfo.graphicsDeviceName}";
            Debug.Log(report);
            string file = Environment.GetEnvironmentVariable("HOOKFALL_PERF_FILE");
            if (!string.IsNullOrEmpty(file)) File.WriteAllText(file, report);
            Assert.Greater(most, 250, "The director never got near the 300-enemy cap");
            Assert.GreaterOrEqual(1000f / avg, 60f, "Production plan target: 60 FPS with 300 enemies (hardware dependent)");
        }
    }
}
