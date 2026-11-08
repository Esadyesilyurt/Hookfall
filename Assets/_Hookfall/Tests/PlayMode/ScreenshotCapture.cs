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
