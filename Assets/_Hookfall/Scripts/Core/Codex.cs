// Hookfall - what the player has met so far, for the menu's codex (added 2026-11-11): the items they have owned and
// how many of each enemy they have killed. Kept in memory during a run and saved to PlayerPrefs when it ends
// ("hf_codex_items": names joined by commas; "hf_codex_kills": "Bat=12,Wyvern=3").
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hookfall.Core
{
    public static class Codex
    {
        public const string ItemsKey = "hf_codex_items";
        public const string KillsKey = "hf_codex_kills";

        private static readonly HashSet<string> Items = new HashSet<string>();
        private static readonly Dictionary<string, int> KillCounts = new Dictionary<string, int>();
        private static bool loaded, dirty;

        /// <summary>Reads the saved codex again (a new run, the menu): what a test left in memory is dropped.</summary>
        public static void Reload()
        {
            Items.Clear();
            KillCounts.Clear();
            foreach (string item in PlayerPrefs.GetString(ItemsKey, "").Split(','))
                if (item.Length > 0) Items.Add(item);
            foreach (string pair in PlayerPrefs.GetString(KillsKey, "").Split(','))
            {
                int at = pair.IndexOf('=');
                if (at > 0 && int.TryParse(pair.Substring(at + 1), out int count)) KillCounts[pair.Substring(0, at)] = count;
            }
            loaded = true;
            dirty = false;
        }

        public static bool HasItem(string id)
        {
            if (!loaded) Reload();
            return Items.Contains(Clean(id));
        }

        public static int Kills(string enemy)
        {
            if (!loaded) Reload();
            return KillCounts.TryGetValue(enemy, out int count) ? count : 0;
        }

        public static void MarkItem(string id)
        {
            if (!loaded) Reload();
            dirty |= Items.Add(Clean(id));
        }

        public static void AddKill(string enemy)
        {
            if (!loaded) Reload();
            KillCounts[enemy] = Kills(enemy) + 1;
            dirty = true;
        }

        /// <summary>Writes the codex down (the end of a run).</summary>
        public static void Save()
        {
            if (!loaded || !dirty) return;
            PlayerPrefs.SetString(ItemsKey, string.Join(",", Items));
            var text = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in KillCounts) text.Append(text.Length > 0 ? "," : "").Append(pair.Key).Append('=').Append(pair.Value);
            PlayerPrefs.SetString(KillsKey, text.ToString());
            PlayerPrefs.Save();
            dirty = false;
        }

        private static string Clean(string id) => id.Replace("(Clone)", "").Replace("PF_Item_", "");
    }
}
