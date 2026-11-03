// Hookfall - XP and levels: XP needed for level n = 10 x n^1.4 (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    public class XPSystem : MonoBehaviour
    {
        private PlayerStats stats;

        public int Level { get; private set; } = 1;
        public float CurrentXP { get; private set; }
        public float XPToNextLevel => Mathf.Round(10f * Mathf.Pow(Level, 1.4f));

        public event Action<int> LeveledUp;

        private void Awake() => stats = GetComponent<PlayerStats>();

        public void AddXP(float amount)
        {
            CurrentXP += amount * (stats != null ? stats.XPMultiplier : 1f);
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                Level++;
                LeveledUp?.Invoke(Level);
            }
        }
    }
}
