// Hookfall - the trait an elite enemy carries besides being bigger and tougher (added 2026-11-10):
// Shielded takes its first blows on a barrier, Swift flies faster, Explosive bursts when it dies,
// Splitting leaves two ordinary enemies of its kind behind.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies
{
    public enum EliteAffix
    {
        None,
        Shielded,
        Swift,
        Explosive,
        Splitting
    }
}
