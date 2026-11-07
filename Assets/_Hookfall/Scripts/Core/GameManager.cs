// Hookfall - run state: timer, kills, pause, modal screens (level-up, chest), death, record and scene changes.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (modal states, record, menu). Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hookfall.Core
{
    public class GameManager : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string ArenaScene = "Arena";

        [SerializeField] private XPSystem playerXP;

        public static GameManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.Playing;
        public float RunTime { get; private set; }
        public int Kills { get; private set; }
        public string EndReason { get; private set; } = "";
        public bool NewRecord { get; private set; }

        public event Action<RunState> StateChanged;
        public event Action RunEnded;

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
                if (State == RunState.Playing) SetState(RunState.Paused);
                else if (State == RunState.Paused) SetState(RunState.Playing);
            }
        }

        public void RegisterKill() => Kills++;

        public void SetPaused(bool paused) => SetState(paused ? RunState.Paused : RunState.Playing);

        /// <summary>Pauses the game for a screen that needs a choice (level-up, chest result).</summary>
        public bool OpenModal(RunState modal)
        {
            if (State != RunState.Playing) return false;
            SetState(modal);
            return true;
        }

        public void CloseModal()
        {
            if (State == RunState.LevelUp || State == RunState.ChestResult) SetState(RunState.Playing);
        }

        public void EndRun(string reason)
        {
            if (State == RunState.Dead) return;
            EndReason = reason;
            NewRecord = RunRecord.Submit(RunTime, playerXP != null ? playerXP.Level : 1, Kills);
            SetState(RunState.Dead);
            Debug.Log($"[Hookfall] Run over ({reason}). Time {RunTime:F1} s, kills {Kills}.");
            RunEnded?.Invoke();
        }

        public void Restart() => LoadScene(ArenaScene);
        public void QuitToMenu() => LoadScene(MenuScene);

        private static void LoadScene(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        private void SetState(RunState next)
        {
            State = next;
            Time.timeScale = next == RunState.Playing ? 1f : 0f;
            LockCursor(next == RunState.Playing);
            StateChanged?.Invoke(next);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
