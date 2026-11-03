// Hookfall - marks an island: the hook can attach to it and standing on it turns combat off.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    public enum IslandKind { Tower, Inner, Outer }

    public class Island : MonoBehaviour
    {
        [SerializeField] private IslandKind kind = IslandKind.Inner;
        [SerializeField] private float width = 10f;

        public IslandKind Kind => kind;
        public float Width => width;

        public void Setup(IslandKind islandKind, float islandWidth)
        {
            kind = islandKind;
            width = islandWidth;
        }
    }
}
