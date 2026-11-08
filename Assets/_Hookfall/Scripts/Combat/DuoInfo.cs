// Hookfall - the two schools, name and effect of every duo synergy (English source text, translated through Loc).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;

namespace Hookfall.Combat
{
    public static class DuoInfo
    {
        public static readonly Duo[] All = (Duo[])System.Enum.GetValues(typeof(Duo));

        public static (School first, School second) Schools(Duo duo)
        {
            switch (duo)
            {
                case Duo.ThermalShock: return (School.Flame, School.Frost);
                case Duo.Plasma: return (School.Flame, School.Storm);
                case Duo.Superconductor: return (School.Frost, School.Storm);
                case Duo.BoilingBlood: return (School.Blood, School.Flame);
                case Duo.FrostbittenWounds: return (School.Blood, School.Frost);
                case Duo.IronWill: return (School.Blood, School.Guardian);
                case Duo.TailwindThunder: return (School.Storm, School.Wild);
                case Duo.Spellstorm: return (School.Arcane, School.Storm);
                case Duo.GoldenTouch: return (School.Arcane, School.Fortune);
                case Duo.GlacierWall: return (School.Guardian, School.Frost);
                default: return (School.Wild, School.Flame);
            }
        }

        public static string Name(Duo duo)
        {
            switch (duo)
            {
                case Duo.ThermalShock: return Loc.T("Thermal Shock");
                case Duo.Plasma: return Loc.T("Plasma");
                case Duo.Superconductor: return Loc.T("Superconductor");
                case Duo.BoilingBlood: return Loc.T("Boiling Blood");
                case Duo.FrostbittenWounds: return Loc.T("Frostbitten Wounds");
                case Duo.IronWill: return Loc.T("Iron Will");
                case Duo.TailwindThunder: return Loc.T("Tailwind Thunder");
                case Duo.Spellstorm: return Loc.T("Spellstorm");
                case Duo.GoldenTouch: return Loc.T("Golden Touch");
                case Duo.GlacierWall: return Loc.T("Glacier Wall");
                default: return Loc.T("Wildfire");
            }
        }

        public static string Effect(Duo duo)
        {
            switch (duo)
            {
                case Duo.ThermalShock: return Loc.T("Chilling a burning enemy or burning a chilled one sets off a steam burst");
                case Duo.Plasma: return Loc.T("Shocked enemies burn twice as hard");
                case Duo.Superconductor: return Loc.T("Storm hits on frozen enemies arc to the enemies around them");
                case Duo.BoilingBlood: return Loc.T("Enemies that burn and bleed at once take +50% from both");
                case Duo.FrostbittenWounds: return Loc.T("Hits on chilled enemies make them bleed");
                case Duo.IronWill: return Loc.T("Healing past full health raises a shield that blocks a hit");
                case Duo.TailwindThunder: return Loc.T("Flying faster than 25 m/s calls lightning on the nearest enemy");
                case Duo.Spellstorm: return Loc.T("Every projectile hit shocks");
                case Duo.GoldenTouch: return Loc.T("Defeated enemies often drop extra gold");
                case Duo.GlacierWall: return Loc.T("Getting hit chills every enemy around you");
                default: return Loc.T("Wild items set enemies on fire");
            }
        }

        /// <summary>"Flame + Frost" in the schools' colours.</summary>
        public static string Pair(Duo duo)
        {
            (School a, School b) = Schools(duo);
            return SchoolInfo.Colored(a) + " + " + SchoolInfo.Colored(b);
        }
    }
}
