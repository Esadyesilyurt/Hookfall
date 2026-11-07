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
    }
}
