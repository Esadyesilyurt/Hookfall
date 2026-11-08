// Hookfall - how each school is shown: name, colour, and what its 2- and 4-item set bonuses do (English source text,
// translated through Loc where it is displayed).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using System.Text;
using Hookfall.Core;
using UnityEngine;

namespace Hookfall.Combat
{
    public static class SchoolInfo
    {
        public static readonly School[] All = (School[])System.Enum.GetValues(typeof(School));

        public static string Name(School school) => Loc.T(school.ToString());

        public static Color Color(School school)
        {
            switch (school)
            {
                case School.Flame: return new Color(1f, 0.48f, 0.18f);
                case School.Frost: return new Color(0.45f, 0.82f, 1f);
                case School.Storm: return new Color(1f, 0.9f, 0.3f);
                case School.Blood: return new Color(0.95f, 0.22f, 0.3f);
                case School.Arcane: return new Color(0.72f, 0.52f, 1f);
                case School.Guardian: return new Color(0.75f, 0.8f, 0.88f);
                case School.Wild: return new Color(0.45f, 0.88f, 0.4f);
                default: return new Color(1f, 0.8f, 0.25f);
            }
        }

        public static string Hex(School school) => "#" + ColorUtility.ToHtmlStringRGB(Color(school));

        public static string Colored(School school) => $"<color={Hex(school)}>{Name(school)}</color>";

        /// <summary>"Flame · Arcane" in the schools' colours, for item cards.</summary>
        public static string Line(IReadOnlyList<School> schools)
        {
            var text = new StringBuilder();
            for (int i = 0; i < schools.Count; i++)
            {
                if (i > 0) text.Append("  ·  ");
                text.Append(Colored(schools[i]));
            }
            return text.ToString();
        }

        /// <summary>The set bonus at 2 (tier 1) or 4 (tier 2) items of the school.</summary>
        public static string Bonus(School school, int tier)
        {
            bool two = tier == 1;
            switch (school)
            {
                case School.Flame: return Loc.T(two ? "Burns deal +50% damage" : "Burning enemies spread their fire when they die");
                case School.Frost: return Loc.T(two ? "Chill slows 50%; chilled enemies take +15% damage" : "Hits can freeze chilled enemies solid");
                case School.Storm: return Loc.T(two ? "Shocked enemies take +25% more damage" : "Shocked enemies burst into lightning when they die");
                case School.Blood: return Loc.T(two ? "+4% lifesteal" : "Every hit bleeds; lifesteal doubles below half health");
                case School.Arcane: return Loc.T(two ? "+1 projectile for every volley" : "All attack items recharge 20% faster");
                case School.Guardian: return Loc.T(two ? "+3 armor" : "Getting hit in the air releases a shockwave");
                case School.Wild: return Loc.T(two ? "+10% flight and +8% walk speed" : "Speed becomes damage: up to +40%");
                default: return Loc.T(two ? "+20% XP and +20% luck" : "Chests offer 4 choices");
            }
        }
    }
}
