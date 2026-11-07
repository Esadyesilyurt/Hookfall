// Hookfall - play-mode smoke tests: each scene loads and runs with no errors in the Console (CLAUDE.md: main must play clean).
// Game types are reached by reflection because a test assembly cannot reference Assembly-CSharp.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hookfall.Tests
{
    public class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator MainMenuLoadsClean()
        {
            SceneManager.LoadScene("MainMenu");
            yield return new WaitForSecondsRealtime(3f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ArenaRunsClean()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Arena has no Player");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            // Keep the player in the air above the tower for a while so combat, kills and pickups all run.
            float end = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < end)
            {
                var body = player.GetComponent<Rigidbody>();
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    body.linearVelocity = Vector3.zero;
                }
                // Answer any level-up screen with the first card.
                if (Time.timeScale == 0f && levelUp != null)
                    levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            int kills = (int)gmType.GetProperty("Kills").GetValue(gmType.GetProperty("Instance").GetValue(null));
            Assert.Greater(kills, 0, "25 s in the air with Orbit Blades should kill at least one bat");
        }

        /// <summary>Gives every item through the chest code, flies for a while, then opens the tower cache.</summary>
        [UnityTest]
        public IEnumerator AllItemsAndTowerCacheRunClean()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type rarityType = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component stats = player.GetComponent(statsType);
            var grant = combatType.GetMethod("Grant");
            for (int i = 0; i < 40; i++) grant.Invoke(combat, new[] { Enum.ToObject(rarityType, 2), (object)stats });
            var items = (ICollection)combatType.GetProperty("Items").GetValue(combat);
            Assert.AreEqual(6, items.Count, "Chests should fill all six item slots");

            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            object gm = gmType.GetProperty("Instance").GetValue(null);
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            float end = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < end)
            {
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(25f, 0f, 0f);   // fast enough for Ram Strike
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }

            // Tower cache: make it ready, stand on the tower top for 5 s.
            Type cacheType = Type.GetType("Hookfall.World.TowerCache, Assembly-CSharp");
            var cache = (Component)UnityEngine.Object.FindAnyObjectByType(cacheType);
            cacheType.GetMethod("SetReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(cache, new object[] { true });
            var ready = cacheType.GetProperty("Ready");
            bool opened = false;
            end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end && !opened)
            {
                opened = !(bool)ready.GetValue(cache);   // the cache stops being ready once it opens
                if (Time.timeScale == 0f && levelUp != null) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                player.transform.position = cache.transform.position + new Vector3(1.2f, 1.02f, 0f);
                player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                yield return null;
            }
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Assert.IsTrue(opened, "Standing on the tower top for 5 s should open the tower cache. " +
                $"State {gmType.GetProperty("State").GetValue(gm)}, timeScale {Time.timeScale}, " +
                $"airborne {pcType.GetProperty("IsAirborne").GetValue(player.GetComponent(pcType))}, " +
                $"player {player.transform.position}, cache {cache.transform.position}");
            gmType.GetMethod("CloseModal").Invoke(gm, null);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Jumps the run clock to 4:55 so wyverns, golems and the first elite wave all appear.</summary>
        [UnityTest]
        public IEnumerator LateRunSpawnsEveryEnemy()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            object gm = gmType.GetProperty("Instance").GetValue(null);
            gmType.GetProperty("RunTime").SetValue(gm, 295f);

            GameObject player = GameObject.Find("Player");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            bool sawWyvern = false, sawGolem = false, sawElite = false;
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            var active = (IList)enemyType.GetField("Active").GetValue(null);

            float end = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < end && !(sawWyvern && sawGolem && sawElite))
            {
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                }
                if (Time.timeScale == 0f && levelUp != null)
                    levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });

                foreach (object e in active)
                {
                    string name = e.GetType().Name;
                    sawWyvern |= name == "Wyvern";
                    sawGolem |= name == "Golem";
                    sawElite |= (bool)enemyType.GetProperty("IsElite").GetValue(e);
                }
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(sawWyvern, "No wyvern spawned");
            Assert.IsTrue(sawGolem, "No golem spawned");
            Assert.IsTrue(sawElite, "No elite wave spawned");
        }
    }
}
