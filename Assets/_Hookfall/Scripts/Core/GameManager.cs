// Hookfall - run state: timer, kills, pause, death and restart.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hookfall.Core
{
    public enum RunState { Playing, Paused, LevelUp, Dead }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.Playing;
        public float RunTime { get; private set; }
        public int Kills { get; private set; }
        public string EndReason { get; private set; } = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
            LockCursor(true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State == RunState.Playing) RunTime += Time.deltaTime;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (State == RunState.Playing) SetPaused(true);
                else if (State == RunState.Paused) SetPaused(false);
            }

            if (State == RunState.Dead && keyboard.rKey.wasPressedThisFrame) Restart();
        }

        public void RegisterKill() => Kills++;

        public void SetPaused(bool paused)
        {
            State = paused ? RunState.Paused : RunState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            LockCursor(!paused);
        }

        public void EndRun(string reason)
        {
            if (State == RunState.Dead) return;
            State = RunState.Dead;
            EndReason = reason;
            Time.timeScale = 0f;
            LockCursor(false);
            Debug.Log($"[Hookfall] Run over ({reason}). Time {RunTime:F1} s, kills {Kills}.");
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
