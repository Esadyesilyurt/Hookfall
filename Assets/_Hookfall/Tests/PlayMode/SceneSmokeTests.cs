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
