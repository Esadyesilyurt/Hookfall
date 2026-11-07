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
            UnityEngine.Object.Destroy(overview.gameObject);

            gmType.GetMethod("EndRun").Invoke(gm, new object[] { "Fell into the abyss" });
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Grab("ui_runend");
        }

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
