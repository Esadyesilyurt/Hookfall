// Hookfall - marks an island: the hook can attach to it and standing on it turns combat off.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (registry, golem occupant). Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using UnityEngine;

namespace Hookfall.World
{
    public class Island : MonoBehaviour
    {
        /// <summary>Every island in the arena (golem and chest placement read this).</summary>
        public static readonly List<Island> All = new List<Island>();

        [SerializeField] private IslandKind kind = IslandKind.Inner;
        [SerializeField] private float width = 10f;

        public IslandKind Kind => kind;
        public float Width => width;
        /// <summary>The top surface centre in world space.</summary>
        public Vector3 Top => transform.position;
        /// <summary>The golem standing on this island, if any.</summary>
        public Component Occupant { get; set; }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void Setup(IslandKind islandKind, float islandWidth)
        {
            kind = islandKind;
            width = islandWidth;
        }
    }
}
