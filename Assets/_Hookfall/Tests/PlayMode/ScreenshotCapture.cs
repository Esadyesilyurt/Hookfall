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
            // Another character's detail view, then every panel of the menu.
            var menuFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type menuType = menu.GetType();
            var roster = (Array)menuType.GetField("characters", menuFlags).GetValue(menu);
            menuType.GetMethod("Pick", menuFlags).Invoke(menu, new object[] { roster.GetValue(Mathf.Min(4, roster.Length - 1)) });
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Grab("menu_character_other");
            foreach (string panel in new[] { "settings", "leaderboard", "shop", "codex", "creditsPanel", "intro" })
            {
                menuType.GetMethod("Show", menuFlags).Invoke(menu, new[] { menuType.GetField(panel, menuFlags).GetValue(menu) });
                yield return new WaitForSecondsRealtime(panel == "intro" ? 2.2f : 0.8f);
                yield return Grab("menu_" + panel);
            }

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
            // A flight chain for the shot: the meter needs a few frames to fade in.
            Type flowType = Type.GetType("Hookfall.Progression.FlowChain, Assembly-CSharp");
            Component flow = player.GetComponent(flowType);
            for (float until = Time.realtimeSinceStartup + 0.6f; Time.realtimeSinceStartup < until;)
            {
                flowType.GetProperty("Count").SetValue(flow, 27);
                flowType.GetField("timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(flow, 3.5f);
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
            // A fuller build for the pause menu and the run-end screen: a few chests' worth of items.
            Type combatType0 = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type rarityType0 = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            for (int i = 0; i < 9; i++)
                combatType0.GetMethod("Grant").Invoke(player.GetComponent(combatType0), new[] { Enum.ToObject(rarityType0, i % 4), (object)player.GetComponent(Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp")) });
            gmType.GetMethod("SetPaused").Invoke(gm, new object[] { true });
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Grab("ui_pause");
            Type pauseType = Type.GetType("Hookfall.UI.PauseMenuUI, Assembly-CSharp");
            var pause = UnityEngine.Object.FindAnyObjectByType(pauseType);
            var selectTab = (Action<int>)pauseType.GetField("selectTab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pause);
            string[] tabs = { "stats", "synergies", "controls" };
            for (int i = 0; i < tabs.Length; i++)
            {
                selectTab(i + 1);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Grab("ui_pause_" + tabs[i]);
            }
            selectTab(0);
            gmType.GetMethod("SetPaused").Invoke(gm, new object[] { false });
            // A few seconds of flight with the new items, so the HUD shot and the damage list have something to show.
            float fly = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < fly)
            {
                Type statsTypeF = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
                Component statsF = player.GetComponent(statsTypeF);
                statsTypeF.GetProperty("CurrentHealth").SetValue(statsF, statsTypeF.GetProperty("MaxHealth").GetValue(statsF));
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(-6f, 14f, -18f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(22f, 4f, 10f);
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            yield return Grab("arena_build");

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

            // The horde's kinds lined up in open sky, at full size at once (their spawn pop is skipped).
            var hidden = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Type enemyBaseType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            var director = (Component)UnityEngine.Object.FindAnyObjectByType(directorType);
            Vector3 line = new Vector3(60f, 74f, 60f);
            string[] kinds = { "batPrefab", "blightpuffPrefab", "razorwingPrefab", "venomwingPrefab", "jellyPrefab", "wyvernPrefab" };
            var lineUp = new System.Collections.Generic.List<Component>();
            for (int i = 0; i < kinds.Length; i++)
            {
                object prefab = directorType.GetField(kinds[i], hidden).GetValue(director);
                var enemy = (Component)directorType.GetMethod("SpawnFlyer", hidden).Invoke(director, new[] { prefab, (object)(line + new Vector3((i - 2.5f) * 4.6f, 0f, 0f)), 0f });
                enemyBaseType.GetField("spawnTimer", hidden).SetValue(enemy, 0f);
                enemy.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, 0f, 1f));
                Transform model = enemy.transform.Find("Visual");
                if (model != null) model.localScale = Vector3.one;
                lineUp.Add(enemy);
            }
            overview.transform.SetPositionAndRotation(line + new Vector3(3f, 2.5f, 15f), Quaternion.LookRotation(new Vector3(-3f, -3.2f, -15f)));
            Render(overview, "enemies_lineup");
            foreach (Component enemy in lineUp) enemyBaseType.GetMethod("Die").Invoke(enemy, new object[] { false });

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

        /// <summary>Close-ups of the Warden's poses, to judge the procedural animation: standing, running, rising, falling,
        /// gliding, the dash, the double-jump tuck, the landing squash and a swing on the rope.</summary>
        [UnityTest]
        public IEnumerator CaptureWardenPoses()
        {
            if (string.IsNullOrEmpty(Folder)) Assert.Ignore("Set HOOKFALL_SHOT_DIR to capture screenshots.");
            Directory.CreateDirectory(Folder);
            PlayerPrefs.SetString("hf_character", Environment.GetEnvironmentVariable("HOOKFALL_SHOT_CHARACTER") ?? "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            SceneSmokeTests.NoBoss();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type hookType = Type.GetType("Hookfall.Player.GrappleHook, Assembly-CSharp");
            Type animatorType = Type.GetType("Hookfall.Player.WardenAnimator, Assembly-CSharp");
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);
            Component hook = player.GetComponent(hookType);
            Component animator = player.GetComponentInChildren(animatorType);
            // No horde in the pictures.
            ((Behaviour)UnityEngine.Object.FindAnyObjectByType(directorType)).enabled = false;
            statsType.GetMethod("SetAbyssKillHeight").Invoke(stats, new object[] { -100000f });

            var shot = new GameObject("Pose Camera").AddComponent<Camera>();
            shot.CopyFrom(Camera.main);
            shot.fieldOfView = 30f;
            void Snap(string name)
            {
                // Three quarters from the front right, level with the chest.
                Vector3 forward = body.linearVelocity.sqrMagnitude > 1f ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).normalized : Vector3.forward;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Vector3 centre = player.transform.position + Vector3.up * 0.1f;
                Vector3 from = centre + right * 4.6f + forward * 2.6f + Vector3.up * 0.9f;
                shot.transform.SetPositionAndRotation(from, Quaternion.LookRotation(centre - from));
                Render(shot, "warden_" + name);
            }
            IEnumerator Fly(Vector3 velocity, float seconds)
            {
                float end = Time.time + seconds;
                while (Time.time < end)
                {
                    statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                    body.linearVelocity = velocity;
                    yield return null;
                }
            }

            // On the tower island: standing, then running.
            yield return new WaitForSeconds(1.2f);
            Snap("idle");
            yield return Fly(new Vector3(6.5f, 0f, 0f), 0.42f);
            Snap("run");

            // In open sky.
            Vector3 sky = new Vector3(50f, 70f, 50f);
            body.position = sky; player.transform.position = sky;
            yield return Fly(new Vector3(9f, 9f, 0f), 0.45f);
            Snap("rise");
            yield return Fly(new Vector3(6f, -15f, 0f), 0.45f);
            Snap("fall");
            body.position = sky; player.transform.position = sky;
            yield return Fly(new Vector3(21f, 0f, 0f), 0.5f);
            Snap("glide");
            animatorType.GetMethod("OnDash", flags).Invoke(animator, null);
            yield return Fly(new Vector3(21f, 0f, 0f), 0.09f);
            Snap("dash");
            yield return Fly(new Vector3(8f, 2f, 0f), 0.5f);
            animatorType.GetMethod("OnTuck", flags).Invoke(animator, null);
            yield return Fly(new Vector3(8f, 6f, 0f), 0.09f);
            Snap("tuck");

            // A swing: hook the tower from a clear spot and let the rope carry the Warden.
            Vector3 target = new Vector3(0f, 14f, 0f);
            Vector3 spot = Vector3.zero;
            for (int i = 0; i < 72 && spot == Vector3.zero; i++)
            {
                float a = i * 25f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 30f, 16f + (i % 4) * 4f, Mathf.Sin(a) * 30f);
                if (Physics.CheckSphere(p, 3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(p, (target - p).normalized, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore)
                    && Vector3.Distance(hit.point, target) < 6f) spot = p;
            }
            if (spot != Vector3.zero)
            {
                body.position = spot; player.transform.position = spot;
                body.linearVelocity = Vector3.Cross(Vector3.up, (target - spot).normalized) * 12f;
                yield return new WaitForFixedUpdate();
                var aim = new GameObject("Pose Aim").transform;
                aim.position = player.transform.position;
                aim.LookAt(target);
                hookType.GetField("aimSource", flags).SetValue(hook, aim);
                hookType.GetMethod("TryThrow", flags).Invoke(hook, new object[] { false });
                float until = Time.time + 1.1f;
                while (Time.time < until)
                {
                    statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                    yield return null;
                }
                Snap("swing");
                hookType.GetMethod("Release").Invoke(hook, null);
            }

            // Back on the island: the landing squash.
            body.position = new Vector3(0f, 1.6f, -7f); player.transform.position = body.position;
            body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.6f);
            animatorType.GetMethod("OnLanded", flags).Invoke(animator, new object[] { 20f });
            yield return new WaitForSeconds(0.06f);
            Snap("land");
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
