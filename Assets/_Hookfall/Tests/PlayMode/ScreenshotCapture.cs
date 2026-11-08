// Hookfall - renders review screenshots of both scenes from a batch-mode test run. Runs only when the environment
// variable HOOKFALL_SHOT_DIR names an output folder, so normal test runs skip it.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hookfall.Tests
{
    public class ScreenshotCapture
    {
        private readonly RecordGuard record = new RecordGuard();

        [SetUp] public void SaveRecord() => record.Save();
        [TearDown] public void RestoreRecord() => record.Restore();

        private static string Folder => Environment.GetEnvironmentVariable("HOOKFALL_SHOT_DIR");

        [UnityTest]
        public IEnumerator CaptureReviewShots()
        {
            if (string.IsNullOrEmpty(Folder)) Assert.Ignore("Set HOOKFALL_SHOT_DIR to capture screenshots.");
            Directory.CreateDirectory(Folder);
            // HOOKFALL_SHOT_LANG=tr renders every shot in Turkish (RecordGuard puts the player's language back).
            Type locType = Type.GetType("Hookfall.Core.Loc, Assembly-CSharp");
            locType.GetProperty("Turkish").SetValue(null, Environment.GetEnvironmentVariable("HOOKFALL_SHOT_LANG") == "tr");

            SceneManager.LoadScene("MainMenu");
            yield return new WaitForSecondsRealtime(1f);
            yield return Grab("menu");
            var menu = UnityEngine.Object.FindAnyObjectByType(Type.GetType("Hookfall.UI.MainMenuUI, Assembly-CSharp"));
            menu.GetType().GetMethod("OnPlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(menu, null);
            yield return new WaitForSecondsRealtime(1f);
            yield return Grab("menu_characters");

            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            gmType.GetProperty("RunTime").SetValue(gmType.GetProperty("Instance").GetValue(null), 150f);
            Type stageType0 = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            stageType0.GetProperty("StageTime").SetValue(stageType0.GetProperty("Instance").GetValue(null), 150f);
            SceneSmokeTests.NoBoss();
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            float end = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < end)
            {
                Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
                Component stats = player.GetComponent(statsType);
                statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(-6f, 14f, -18f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(22f, 4f, 10f);
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            yield return Grab("arena_play");

            // The screens: level-up, pause, run end.
            Type xpType = Type.GetType("Hookfall.Progression.XPSystem, Assembly-CSharp");
            xpType.GetMethod("AddXP").Invoke(player.GetComponent(xpType), new object[] { 60f });
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Grab("ui_levelup");
            while (Time.timeScale == 0f && levelUp != null)
            {
                levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            object gm = gmType.GetProperty("Instance").GetValue(null);
            gmType.GetMethod("SetPaused").Invoke(gm, new object[] { true });
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Grab("ui_pause");
            gmType.GetMethod("SetPaused").Invoke(gm, new object[] { false });

            // An Epic chest's three cards.
            Type chestUiType = Type.GetType("Hookfall.UI.ChestResultUI, Assembly-CSharp");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type statsType2 = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            chestUiType.GetMethod("Offer").Invoke(null, new object[] { player.GetComponent(combatType), player.GetComponent(statsType2),
                Enum.ToObject(Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp"), 2) });
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Grab("ui_chest");
            object chestUi = chestUiType.GetProperty("Instance").GetValue(null);
            chestUiType.GetMethod("Pick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(chestUi, new object[] { 0 });
            yield return null;

            // Overview from outside the arena, without the HUD.
            Camera cam = Camera.main;
            var overview = new GameObject("Overview").AddComponent<Camera>();
            overview.CopyFrom(cam);
            overview.fieldOfView = 55f;
            overview.transform.SetPositionAndRotation(new Vector3(70f, 45f, -95f), Quaternion.LookRotation(new Vector3(-70f, -40f, 95f)));
            Render(overview, "arena_overview");
            overview.transform.SetPositionAndRotation(new Vector3(14f, 10f, -22f), Quaternion.LookRotation(new Vector3(-14f, 2f, 22f)));
            Render(overview, "tower_close");

            // A chest with its spotlight: spawn one now and look at it from 30 m.
            Type spawnerType = Type.GetType("Hookfall.World.ChestSpawner, Assembly-CSharp");
            var spawner = (Component)UnityEngine.Object.FindAnyObjectByType(spawnerType);
            spawnerType.GetField("timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(spawner, 0f);
            yield return null;
            yield return null;
            if (spawner.transform.childCount > 0)
            {
                Vector3 chest = spawner.transform.GetChild(0).position;
                overview.transform.SetPositionAndRotation(chest + new Vector3(18f, 14f, -22f), Quaternion.LookRotation(new Vector3(-18f, -10f, 22f)));
                Render(overview, "chest_light");
            }
            // Sky features: one of each, seen from 20-35 m.
            GameObject sky = GameObject.Find("Sky Features");
            if (sky != null)
            {
                Type channelType = Type.GetType("Hookfall.World.AirChannel, Assembly-CSharp");
                var channel = sky.GetComponentInChildren(channelType);
                if (channel != null)
                {
                    Vector3 a = (Vector3)channelType.GetProperty("Entry").GetValue(channel);
                    Vector3 b = (Vector3)channelType.GetProperty("Exit").GetValue(channel);
                    Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up);
                    LookAt(overview, (a + b) * 0.5f, side * 35f + Vector3.up * 8f);
                    Render(overview, "sky_channel");
                }
                foreach (string kind in new[] { "Updraft", "RefillCrystal", "HookAnchor" })
                {
                    var feature = sky.GetComponentInChildren(Type.GetType($"Hookfall.World.{kind}, Assembly-CSharp"));
                    if (feature == null) continue;
                    Vector3 target = feature.transform.position + (kind == "Updraft" ? Vector3.up * 15f : Vector3.zero);
                    LookAt(overview, target, kind == "Updraft" ? new Vector3(22f, 4f, -22f) : new Vector3(9f, 4f, -12f));
                    Render(overview, "sky_" + kind.ToLower());
                }
            }
            UnityEngine.Object.Destroy(overview.gameObject);

            gmType.GetMethod("EndRun").Invoke(gm, new object[] { "Fell into the abyss" });
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Grab("ui_runend");
        }

        /// <summary>The stage boss: its entrance, the fight, its fall, the portal and the next stage's title.</summary>
        [UnityTest]
        public IEnumerator CaptureBossShots()
        {
            if (string.IsNullOrEmpty(Folder)) Assert.Ignore("Set HOOKFALL_SHOT_DIR to capture screenshots.");
            Directory.CreateDirectory(Folder);
            Type locType = Type.GetType("Hookfall.Core.Loc, Assembly-CSharp");
            locType.GetProperty("Turkish").SetValue(null, Environment.GetEnvironmentVariable("HOOKFALL_SHOT_LANG") == "tr");
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject player = GameObject.Find("Player");
            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            Type chestUiType = Type.GetType("Hookfall.UI.ChestResultUI, Assembly-CSharp");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            object stage = stageType.GetProperty("Instance").GetValue(null);
            object gm = gmType.GetProperty("Instance").GetValue(null);
            Component stats = player.GetComponent(statsType);
            Func<string> state = () => gmType.GetProperty("State").GetValue(gm).ToString();
            void Alive()
            {
                statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                statsType.GetMethod("SetAbyssKillHeight").Invoke(stats, new object[] { -100000f });
                if (state() == "LevelUp") levelUpType.GetMethod("Pick").Invoke(player.GetComponent(levelUpType), new object[] { 0 });
            }
            Vector3 centre = (Vector3)stageType.GetProperty("ArenaCentre").GetValue(stage);

            // Stand on the tower deck, so the entrance is seen from the Warden's place.
            yield return new WaitForSecondsRealtime(1f);
            player.GetComponent<Rigidbody>().position = new Vector3(0f, 19.5f, -1.5f);
            player.transform.position = new Vector3(0f, 19.5f, -1.5f);
            yield return new WaitForSecondsRealtime(0.5f);
            stageType.GetMethod("SummonBoss").Invoke(stage, null);
            yield return new WaitForSecondsRealtime(1.9f);
            yield return Grab("boss_entrance_fall");
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Grab("boss_entrance_roar");
            float end = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < end && state() != "Playing") { Alive(); yield return null; }

            // The fight: the Warden on one of the boss's islands, the HUD with its health bar.
            object boss = stageType.GetProperty("Boss").GetValue(stage);
            Vector3 perch = centre + new Vector3(0f, -14f, -30f);
            foreach (Component island in (IList)Type.GetType("Hookfall.World.Island, Assembly-CSharp").GetField("All").GetValue(null))
                if (island.GetType().GetProperty("Kind").GetValue(island).ToString() == "Boss") { perch = island.transform.position + Vector3.up * 1.6f; break; }
            player.GetComponent<Rigidbody>().position = perch;
            player.transform.position = perch;
            enemyType.GetMethod("TakeDamage").Invoke(boss, new object[] { (float)enemyType.GetProperty("MaxHealth").GetValue(boss) * 0.45f, false });
            end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end) { Alive(); yield return null; }
            yield return Grab("boss_fight");
            Camera cam = Camera.main;
            var overview = new GameObject("Overview").AddComponent<Camera>();
            overview.CopyFrom(cam);
            overview.fieldOfView = 55f;
            LookAt(overview, centre + Vector3.down * 8f, new Vector3(70f, 10f, -70f));
            Render(overview, "boss_arena");

            // It falls; the portal rises.
            enemyType.GetMethod("TakeDamage").Invoke(boss, new object[] { (float)enemyType.GetProperty("MaxHealth").GetValue(boss) * 5f, false });
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Grab("boss_falls");
            end = Time.realtimeSinceStartup + 10f;
            bool rewardShot = false;
            while (Time.realtimeSinceStartup < end && !rewardShot)
            {
                Alive();
                if (state() == "ChestResult")
                {
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Grab("boss_reward");
                    object ui = chestUiType.GetProperty("Instance").GetValue(null);
                    chestUiType.GetMethod("Pick", flags).Invoke(ui, new object[] { 0 });
                    rewardShot = true;
                }
                yield return null;
            }
            object portal = stageType.GetProperty("Portal").GetValue(stage);
            if (portal != null)
            {
                Vector3 portalCentre = (Vector3)portal.GetType().GetProperty("Centre").GetValue(portal);
                LookAt(overview, portalCentre, new Vector3(16f, 5f, -18f));
                Render(overview, "portal");
                UnityEngine.Object.Destroy(overview.gameObject);
                player.GetComponent<Rigidbody>().position = portalCentre + new Vector3(0f, -1.5f, -8f);
                player.transform.position = portalCentre + new Vector3(0f, -1.5f, -8f);
                yield return new WaitForSecondsRealtime(0.5f);
                player.GetComponent<Rigidbody>().position = portalCentre;
                player.transform.position = portalCentre;
                yield return new WaitForSecondsRealtime(1.0f);
                yield return Grab("portal_dive");
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Grab("stage_title");
                end = Time.realtimeSinceStartup + 8f;
                while (Time.realtimeSinceStartup < end && state() != "Playing") { Alive(); yield return null; }
                yield return new WaitForSecondsRealtime(1.5f);
                Alive();
                yield return Grab("stage2_start");
            }
        }

        private static void LookAt(Camera camera, Vector3 target, Vector3 offset) =>
            camera.transform.SetPositionAndRotation(target + offset, Quaternion.LookRotation(-offset));

        /// <summary>WaitForEndOfFrame never fires in batch mode, so the UI is moved onto the camera and rendered with it.</summary>
        private static IEnumerator Grab(string name)
        {
            Camera cam = Camera.main;
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.5f;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            Render(cam, name);
        }

        private static void Render(Camera camera, string name)
        {
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
        }
    }
}
