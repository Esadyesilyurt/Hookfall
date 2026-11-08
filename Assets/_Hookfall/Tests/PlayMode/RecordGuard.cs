// Hookfall - keeps play-mode tests from touching the player's saved progress: the editor's PlayerPrefs are the same
// ones play mode uses, and a test that ends a run would otherwise overwrite the real record and add to the character
// unlock progress (extended on 2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using UnityEngine;

namespace Hookfall.Tests
{
    public class RecordGuard
    {
        private static readonly string[] FloatKeys = { "hf_best_time" };
        private static readonly string[] IntKeys = { "hf_best_level", "hf_best_kills", "hf_total_kills", "hf_max_level" };
        private static readonly string[] StringKeys = { "hf_character" };
        private readonly Dictionary<string, object> saved = new Dictionary<string, object>();

        public void Save()
        {
            saved.Clear();
            foreach (string key in FloatKeys) if (PlayerPrefs.HasKey(key)) saved[key] = PlayerPrefs.GetFloat(key);
            foreach (string key in IntKeys) if (PlayerPrefs.HasKey(key)) saved[key] = PlayerPrefs.GetInt(key);
            foreach (string key in StringKeys) if (PlayerPrefs.HasKey(key)) saved[key] = PlayerPrefs.GetString(key);
        }

        public void Restore()
        {
            foreach (string key in FloatKeys) Put(key, k => PlayerPrefs.SetFloat(k, (float)saved[k]));
            foreach (string key in IntKeys) Put(key, k => PlayerPrefs.SetInt(k, (int)saved[k]));
            foreach (string key in StringKeys) Put(key, k => PlayerPrefs.SetString(k, (string)saved[k]));
            PlayerPrefs.Save();
        }

        private void Put(string key, System.Action<string> set)
        {
            if (saved.ContainsKey(key)) set(key);
            else PlayerPrefs.DeleteKey(key);
        }
    }
}
