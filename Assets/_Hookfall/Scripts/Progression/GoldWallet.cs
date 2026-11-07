// Hookfall - gold from enemies, spent on chests (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Progression
{
    public class GoldWallet : MonoBehaviour
    {
        public int Gold { get; private set; }
        public int Earned { get; private set; }

        public void Add(int amount)
        {
            amount = Mathf.Max(0, amount);
            Gold += amount;
            Earned += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount > Gold) return false;
            Gold -= amount;
            return true;
        }
    }
}
