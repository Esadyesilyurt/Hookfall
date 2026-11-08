// Hookfall - a floating rock in open air that the grappling hook can catch, like an island, so gaps can be swung across.
// Marks the rock for GrappleHook.IsHookable and turns its glow ring. Placed by SkyFeatures.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    public class HookAnchor : MonoBehaviour
    {
        [SerializeField] private Transform ring;

        public void Init(Transform glowRing) => ring = glowRing;

        private void Update()
        {
            if (ring != null) ring.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }
    }
}
