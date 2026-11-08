// Hookfall - which arena zone an island belongs to (GDD 7.1). Large islands (2026-11-08) are wide and never get a
// golem; Boss islands float around the stage boss and carry its portal.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.World
{
    public enum IslandKind { Tower, Inner, Outer, Large, Boss }
}
