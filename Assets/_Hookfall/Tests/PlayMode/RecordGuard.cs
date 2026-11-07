// Hookfall - keeps play-mode tests from touching the player's saved best run: the editor's PlayerPrefs are the same
// ones play mode uses, and a test that ends a run would otherwise overwrite the real record.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Tests
{
    public class RecordGuard
    {
        private static readonly string[] Keys = { "hf_best_time", "hf_best_level", "hf_best_kills" };
        private bool had;
        private float time;
        private int level, kills;

        public void Save()
        {
            had = PlayerPrefs.HasKey(Keys[0]);
            time = PlayerPrefs.GetFloat(Keys[0], 0f);
            level = PlayerPrefs.GetInt(Keys[1], 0);
            kills = PlayerPrefs.GetInt(Keys[2], 0);
        }

        public void Restore()
        {
            if (had)
            {
                PlayerPrefs.SetFloat(Keys[0], time);
                PlayerPrefs.SetInt(Keys[1], level);
                PlayerPrefs.SetInt(Keys[2], kills);
            }
            else foreach (string key in Keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
