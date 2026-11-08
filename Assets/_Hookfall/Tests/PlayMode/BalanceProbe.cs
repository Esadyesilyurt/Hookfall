// Hookfall - balance probe (2026-11-08): flies every character through the first 90 s of a run on an autopilot (a
// circle around the tower, picking the first level-up card) and logs kills, level, gold and damage taken, so the
// characters can be compared; then gives the Warden a mid-run build and times a boss fight. Not a pass/fail test:
// [Explicit], run it on purpose with -testFilter Hookfall.Tests.BalanceProbe. Results go to the log as "[Hookfall] Probe".
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hookfall.Tests
{
    public class BalanceProbe
    {
        private const float Speedup = 3f;
        private readonly RecordGuard record = new RecordGuard();

        [SetUp] public void SaveRecord() => record.Save();
        [TearDown] public void RestoreRecord() { record.Restore(); Time.timeScale = 1f; }

        private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp");

        [UnityTest, Explicit, Timeout(1200000)]
        public IEnumerator EarlyGameByCharacter()
        {
            // HOOKFALL_PROBE=Warden,FrostSeer limits the run to some characters.
            string only = Environment.GetEnvironmentVariable("HOOKFALL_PROBE");
            string[] ids = !string.IsNullOrEmpty(only) ? only.Split(',')
                : new[] { "Warden", "Skyrunner", "Bulwark", "Lumen", "EmberWitch", "FrostSeer", "StormHerald" };
            var report = new StringBuilder("[Hookfall] Probe: first 90 s per character (autopilot circling the tower)\n");
            foreach (string id in ids)
            {
                PlayerPrefs.SetString("hf_character", id);
                SceneManager.LoadScene("Arena");
                yield return null;
                SceneSmokeTests.NoBoss();
                SameMap();
                yield return null;
                // Above every island (tops reach 46 m), so no layout can block the path.
                var run = new Run();
                yield return run.Fly(90f, (t, p) => new Vector3(Mathf.Cos(t * 0.6f) * 30f, 52f + Mathf.Sin(t * 0.9f) * 3f, Mathf.Sin(t * 0.6f) * 30f));
                string line = $"  {id,-12} kills {run.Kills,4}  level {run.Level,2}  gold {run.Gold,4}  damage taken {run.Damage,6:0}  hits {run.Hits,3}  first level-up {run.FirstLevelUp,5:0.0} s";
                Debug.Log("[Hookfall] Probe " + line.Trim());
                report.AppendLine(line);
            }
            Debug.Log(report.ToString());
        }

        /// <summary>Every character flies over the same layout: the generator's seed fixed, the map rebuilt.</summary>
        private static void SameMap()
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type generatorType = T("Hookfall.World.IslandGenerator"), skyType = T("Hookfall.World.SkyFeatures");
            object generator = UnityEngine.Object.FindAnyObjectByType(generatorType);
            generatorType.GetField("seed", flags).SetValue(generator, 424242);
            generatorType.GetField("regenerations", flags).SetValue(generator, -1);
            generatorType.GetMethod("Regenerate").Invoke(generator, null);
            object sky = UnityEngine.Object.FindAnyObjectByType(skyType);
            if (sky != null) skyType.GetMethod("Rebuild").Invoke(sky, null);
        }

        [UnityTest, Explicit, Timeout(600000)]
        public IEnumerator BossFightWithAMidRunBuild()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type combatType = T("Hookfall.Combat.CombatSystem"), rewardType = T("Hookfall.Combat.ChestReward"), statType = T("Hookfall.Player.StatType");
            Type statsType = T("Hookfall.Player.PlayerStats"), stageType = T("Hookfall.World.StageDirector"), enemyType = T("Hookfall.Enemies.EnemyBase");
            Type gmType = T("Hookfall.Core.GameManager"), cutsceneType = T("Hookfall.Feel.CutsceneDirector");
            Component combat = player.GetComponent(combatType), stats = player.GetComponent(statsType);

            // A typical build at the boss: four attack items at level 3 and a few damage and speed picks.
            string[] build = { "PF_Item_HomingSparks", "PF_Item_SkyboltCrossbow", "PF_Item_StormAura", "PF_Item_ChainLightning" };
            foreach (Component prefab in (IList)combatType.GetField("itemPrefabs", flags).GetValue(combat))
                if (Array.IndexOf(build, prefab.name) >= 0)
                {
                    object reward = Activator.CreateInstance(rewardType, prefab.GetType().GetProperty("Rarity").GetValue(prefab), prefab, true, 3, Enum.ToObject(statType, 0));
                    combatType.GetMethod("Claim").Invoke(combat, new[] { reward, stats, null });
                }
            foreach (Component item in (IList)combatType.GetProperty("Items").GetValue(combat))
                item.GetType().GetMethod("SetLevel").Invoke(item, new object[] { 3 });
            foreach (string pick in new[] { "Damage", "Damage", "Damage", "AttackSpeed", "AttackSpeed", "MaxHealth", "MaxHealth", "Armor" })
                statsType.GetMethod("Apply").Invoke(stats, new object[] { Enum.Parse(statType, pick), 1 });

            object stage = stageType.GetProperty("Instance").GetValue(null);
            stageType.GetMethod("SummonBoss").Invoke(stage, null);
            object gm = gmType.GetProperty("Instance").GetValue(null);
            var cutscenes = UnityEngine.Object.FindAnyObjectByType(cutsceneType);
            object boss = null;
            float end = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < end)
            {
                if (gmType.GetProperty("State").GetValue(gm).ToString() == "Cutscene") cutsceneType.GetField("skip", flags).SetValue(cutscenes, true);
                boss = stageType.GetProperty("Boss").GetValue(stage);
                if (boss != null && (bool)boss.GetType().GetProperty("Fighting").GetValue(boss) && gmType.GetProperty("State").GetValue(gm).ToString() == "Playing") break;
                yield return null;
            }
            Assert.IsNotNull(boss, "The boss should rise");
            Vector3 centre = (Vector3)stageType.GetProperty("ArenaCentre").GetValue(stage);
            float max = (float)enemyType.GetProperty("MaxHealth").GetValue(boss);

            // Circle the boss at 16-26 m, around its height, the way a player swings around it.
            var run = new Run();
            yield return run.Fly(150f, (t, p) => centre + new Vector3(Mathf.Cos(t * 0.75f) * (21f + Mathf.Sin(t * 0.5f) * 5f), Mathf.Sin(t * 0.8f) * 6f,
                Mathf.Sin(t * 0.75f) * (21f + Mathf.Sin(t * 0.5f) * 5f)), () => (bool)enemyType.GetProperty("IsDead").GetValue(boss));
            float left = (bool)enemyType.GetProperty("IsDead").GetValue(boss) ? 0f : (float)enemyType.GetProperty("Health").GetValue(boss);
            Debug.Log($"[Hookfall] Probe boss: max health {max:0}, {(left <= 0f ? $"killed in {run.Elapsed:0} s" : $"{left / max:P0} left after {run.Elapsed:0} s")}, " +
                      $"damage taken {run.Damage:0} in {run.Hits} hits ({run.Damage / Mathf.Max(1f, run.Elapsed):0.0}/s), Warden max health {(float)statsType.GetProperty("MaxHealth").GetValue(stats):0}");
        }

        /// <summary>One autopilot flight: steers the Warden along a path, keeps them alive and counts what happens.</summary>
        private class Run
        {
            public int Kills, Level, Gold, Hits;
            public float Damage, FirstLevelUp = -1f, Elapsed;

            public IEnumerator Fly(float seconds, Func<float, Vector3, Vector3> path, Func<bool> done = null)
            {
                GameObject player = GameObject.Find("Player");
                var body = player.GetComponent<Rigidbody>();
                Type statsType = T("Hookfall.Player.PlayerStats"), xpType = T("Hookfall.Progression.XPSystem"), gmType = T("Hookfall.Core.GameManager");
                Type levelUpType = T("Hookfall.Progression.LevelUpSystem"), walletType = T("Hookfall.Progression.GoldWallet"), chestUiType = T("Hookfall.UI.ChestResultUI");
                Component stats = player.GetComponent(statsType), xp = player.GetComponent(xpType), levelUp = player.GetComponent(levelUpType);
                Component wallet = player.GetComponent(walletType);
                object gm = gmType.GetProperty("Instance").GetValue(null);
                var health = statsType.GetProperty("CurrentHealth");
                var maxHealth = statsType.GetProperty("MaxHealth");
                statsType.GetMethod("SetAbyssKillHeight").Invoke(stats, new object[] { -100000f });
                float last = (float)health.GetValue(stats);
                float chestAt = -1f;
                body.position = path(0f, body.position);
                while (Elapsed < seconds && (done == null || !done()))
                {
                    string state = gmType.GetProperty("State").GetValue(gm).ToString();
                    if (state == "LevelUp") levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                    else if (state == "ChestResult")
                    {
                        if (chestAt < 0f) chestAt = Time.realtimeSinceStartup;
                        if (Time.realtimeSinceStartup - chestAt > 0.4f)
                        {
                            object ui = chestUiType.GetProperty("Instance").GetValue(null);
                            chestUiType.GetMethod("Pick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(ui, new object[] { 0 });
                            chestAt = -1f;
                        }
                    }
                    else if (state == "Playing") Time.timeScale = Speedup;

                    float now = (float)health.GetValue(stats);
                    if (now < last) { Damage += last - now; Hits++; }
                    float max = (float)maxHealth.GetValue(stats);
                    health.SetValue(stats, max);
                    last = max;

                    if (Time.timeScale > 0f)
                    {
                        Elapsed += Time.deltaTime;
                        Vector3 target = path(Elapsed + 0.4f, body.position);
                        Vector3 v = (target - body.position) * 2.5f;
                        body.linearVelocity = Vector3.ClampMagnitude(v, 26f);
                    }
                    int level = (int)xpType.GetProperty("Level").GetValue(xp);
                    if (level > 1 && FirstLevelUp < 0f) FirstLevelUp = Elapsed;
                    yield return null;
                }
                Time.timeScale = 1f;
                Kills = (int)gmType.GetProperty("Kills").GetValue(gm);
                Level = (int)xpType.GetProperty("Level").GetValue(xp);
                Gold = (int)walletType.GetProperty("Earned").GetValue(wallet);
            }
        }
    }
}
