// Hookfall - the game's two languages (2026-11-08): English, the source text everywhere in the code, and Turkish from
// LocTable. Every English string shown on screen goes through T (UIFactory.Text does it for fixed labels); strings with
// numbers go through F with a {0} format. A string missing from the table stays English.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Core
{
    public static class Loc
    {
        public const string LanguageKey = "hf_language";

        private static int turkish = -1;   // cached PlayerPrefs value: -1 unknown, 0 English, 1 Turkish

        public static bool Turkish
        {
            get
            {
                if (turkish < 0) turkish = PlayerPrefs.GetString(LanguageKey, "en") == "tr" ? 1 : 0;
                return turkish == 1;
            }
            set
            {
                turkish = value ? 1 : 0;
                PlayerPrefs.SetString(LanguageKey, value ? "tr" : "en");
                PlayerPrefs.Save();
            }
        }

        public static string T(string english) =>
            Turkish && !string.IsNullOrEmpty(english) && LocTable.Turkish.TryGetValue(english, out string text) ? text : english;

        public static string F(string englishFormat, params object[] args) => string.Format(T(englishFormat), args);

        private static readonly System.Globalization.CultureInfo TurkishCulture = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>Capitals for display: Turkish dots its capital i (DEVİ, not DEVI). Keys keep ToUpperInvariant.</summary>
        public static string Upper(string text) => text == null ? null : Turkish ? text.ToUpper(TurkishCulture) : text.ToUpperInvariant();
    }
}
