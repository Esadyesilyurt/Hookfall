// Hookfall - performance tests: the game's own enemy cap (120) and the production plan's 300 enemies (Week 11) must
// each hold 60 FPS at 1080p while the Warden fights. Writes the numbers to the log, and appends them to
// HOOKFALL_PERF_FILE when set. Close the other programs that use the GPU first: a game left running cut the result to a third.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08; split in two on 2026-11-09, when the horde started
// keeping up with the Warden and the whole horde came on screen at once. Prompts and changes are recorded in Docs/PromptLog.md.
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

        /// <summary>The game's own cap (120 since 2026-11-08) with the whole horde around the Warden: must hold 60 FPS.</summary>
        [UnityTest]
        public IEnumerator EnemiesAtTheGameCap() => Measure(0, true);

        /// <summary>The production plan's 300 enemies, more than the game ever allows at once: must hold 60 FPS too.</summary>
        [UnityTest]
        public IEnumerator ThreeHundredEnemies() => Measure(300, true);

        /// <param name="cap">Enemies to fill the sky with; 0 keeps the game's own cap.</param>
        private static IEnumerator Measure(int cap, bool requireSixty)
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject player = GameObject.Find("Player");
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            var director = (Component)UnityEngine.Object.FindAnyObjectByType(directorType);
            directorType.GetField("baseSpawnPerSecond", flags).SetValue(director, 400f);
            if (cap > 0) directorType.GetField("maxAlive", flags).SetValue(director, cap);
            // The frame-rate fallback would hold the horde below the count being measured.
            directorType.GetField("minFps", flags).SetValue(director, 0f);
            int wanted = (int)directorType.GetField("maxAlive", flags).GetValue(director);
            SceneSmokeTests.NoBoss();   // the boss's entrance would stop time mid-measurement
            var alive = directorType.GetProperty("AliveCount");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);

            int picks = 0;
            void Keep()
            {
                statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 14f, -12f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(18f, 6f, 8f);
                }
                if (Time.timeScale == 0f) { levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 }); picks++; }
            }
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            object gm = gmType.GetProperty("Instance").GetValue(null);

            float until = Time.realtimeSinceStartup + 20f;
            while ((int)alive.GetValue(director) < wanted - 10 && Time.realtimeSinceStartup < until)
            {
                Keep();
                yield return null;
            }

            // Batch mode runs a 640x480 window; render the game camera at 1080p so the GPU cost is real.
            var screen = new RenderTexture(1920, 1080, 24);
            Camera.main.targetTexture = screen;

            var frames = new List<float>();
            int fewest = int.MaxValue, most = 0;
            int killsBefore = (int)gmType.GetProperty("Kills").GetValue(gm);
            picks = 0;
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
            UnityEngine.Object.Destroy(screen);
            frames.Sort();
            float sum = 0f;
            foreach (float f in frames) sum += f;
            float avg = sum / frames.Count;
            float p95 = frames[(int)(frames.Count * 0.95f)];
            string report = $"[Hookfall] Perf with a {wanted}-enemy cap: {fewest}-{most} alive, {frames.Count} frames, average {avg:0.00} ms ({1000f / avg:0} FPS), " +
                            $"95th percentile {p95:0.00} ms ({1000f / p95:0} FPS), rendered at 1920x1080, GPU {SystemInfo.graphicsDeviceName}; " +
                            $"{(int)gmType.GetProperty("Kills").GetValue(gm) - killsBefore} kills and {picks} level-up picks while measuring";
            Debug.Log(report);
            string file = Environment.GetEnvironmentVariable("HOOKFALL_PERF_FILE");
            if (!string.IsNullOrEmpty(file)) File.AppendAllText(file, report + Environment.NewLine);
            Assert.Greater(most, wanted * 0.8f, $"The director never got near the {wanted}-enemy cap");
            if (requireSixty) Assert.GreaterOrEqual(1000f / avg, 60f, $"60 FPS with the game's {wanted} enemies (hardware dependent)");
        }
    }
}
