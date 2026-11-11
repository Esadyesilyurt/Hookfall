// Hookfall - a formation of Razorwings: the first living member leads, the others fly in the slots behind it. When
// the leader falls the next one takes its place and the ranks close.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;

namespace Hookfall.Enemies
{
    public class Flock
    {
        private readonly List<Razorwing> members = new List<Razorwing>();

        public Razorwing Leader => members.Count > 0 ? members[0] : null;
        public int Count => members.Count;

        public void Add(Razorwing member)
        {
            members.Add(member);
            member.Flock = this;
        }

        public void Remove(Razorwing member) => members.Remove(member);

        /// <summary>0 for the leader, then 1, 2, 3... alternating to its left and right.</summary>
        public int SlotOf(Razorwing member) => members.IndexOf(member);
    }
}
