// Hookfall - editor menu to forget the saved best run, for example after test runs.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using UnityEditor;
using UnityEngine;

namespace Hookfall.EditorTools
{
    public static class RecordTools
    {
        [MenuItem("Hookfall/Clear Best Run")]
        public static void ClearBestRun()
        {
            RunRecord.Clear();
            Debug.Log("[Hookfall] Best run cleared.");
        }
    }
}
