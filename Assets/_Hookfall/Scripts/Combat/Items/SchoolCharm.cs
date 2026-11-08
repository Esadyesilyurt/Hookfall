// Hookfall - a school charm (2026-11-08): a passive that strengthens its school's effect (burn, chill, shock, bleed, or
// Arcane item damage) and counts toward that school's set. The heart of a build: pair it with items of its school.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class SchoolCharm : PassiveItem
    {
        [SerializeField] private School school = School.Flame;
        [Tooltip("Boost per level: burn and bleed damage share, chilled/shocked extra damage taken, or Arcane item damage.")]
        [SerializeField] private float boostPerLevel = 0.25f;

        public School Boosted => school;
        public float Boost => boostPerLevel * Level;

        private string Effect(float boost)
        {
            string pct = (boost * 100f).ToString("0");
            switch (school)
            {
                case School.Flame: return Loc.F("+{0}% burn damage", pct);
                case School.Frost: return Loc.F("Chilled enemies take +{0}% damage", pct);
                case School.Storm: return Loc.F("Shocked enemies take +{0}% damage", pct);
                case School.Blood: return Loc.F("+{0}% bleed damage", pct);
                default: return Loc.F("+{0}% damage for {1} items", pct, SchoolInfo.Name(school));
            }
        }

        public override IEnumerable<string> StatLines()
        {
            yield return Effect(Boost);
        }

        public override string DescribeNextLevel() => Effect(boostPerLevel * (Level + 1));
    }
}
