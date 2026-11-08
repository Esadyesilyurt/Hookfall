// Hookfall - builds the item prefabs (the six GDD 4.5 attack items, the Tether Coil and the passive items added on
// 2026-11-08, each with a rarity), the projectile prefabs, and the chest and tower cache prefabs.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using UnityEditor;
using Hookfall.Combat;
using Hookfall.Combat.Items;
using Hookfall.Player;
using Hookfall.World;
using UnityEngine;
using UnityEngine.Rendering;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class ItemPrefabs
    {
        private const string Folder = Root + "/Prefabs/Items";

        public static void CreateMaterials()
        {
            GetOrCreateMaterial("M_Shard", new Color(0.45f, 1f, 1f), true);
            GetOrCreateMaterial("M_Spark", new Color(0.6f, 1f, 0.95f), true);
            GetOrCreateMaterial("M_Beam", new Color(0.4f, 0.98f, 1f), true);
            Transparent(GetOrCreateMaterial("M_Aura", new Color(0.3f, 0.95f, 1f, 0.18f), true));
            GetOrCreateMaterial("M_ChestWood", new Color(0.45f, 0.28f, 0.15f), false);
            GetOrCreateMaterial("M_ChestTrim", new Color(1f, 0.72f, 0.2f), false);
            GetOrCreateMaterial("M_CacheGlow", new Color(1f, 0.85f, 0.4f), true);
            GetOrCreateMaterial("M_HealGlow", new Color(0.45f, 1f, 0.5f), true);
            Transparent(GetOrCreateMaterial("M_CacheBeam", new Color(1f, 0.85f, 0.4f, 0.35f), true));
        }

        /// <summary>All chest items: attack items, then passives; index 0 is Orbit Blades, the starting item.</summary>
        public static List<ItemBase> CreateItems()
        {
            Projectile shard = CreateProjectile("PF_Shard", PrimitiveType.Cube, new Vector3(0.12f, 0.12f, 0.7f), "M_Shard", 0.25f);
            Projectile spark = CreateProjectile("PF_Spark", PrimitiveType.Sphere, Vector3.one * 0.3f, "M_Spark", 0.35f);

            var items = new List<ItemBase>
            {
                Item<OrbitBlades>("OrbitBlades", "Moonblade Halo", 10f, 0.5f,
                    "Twin vault blades bound to the Warden by a Tether rune; they circle and cut whatever comes close.",
                    i => SetRef(i, "bladeMaterial", Mat("M_Shard")), Rarity.Common),
                Item<ShardBurst>("ShardBurst", "Crystal Hedgehog", 12f, 1.5f,
                    "Splinters of a cracked Heartstone ward that burst outwards whenever the Warden takes flight.",
                    i => SetRef(i, "projectilePrefab", shard), Rarity.Common),
                Item<ArcLaser>("ArcLaser", "Sunlance Prism", 20f, 2f,
                    "A lens from the fallen archer tower; it bends the Heartstone's light into a sweeping blade.",
                    i => SetRef(i, "beamMaterial", Mat("M_Beam")), Rarity.Epic),
                Item<RamStrike>("RamStrike", "Rhino Spirit Charge", 0f, 0.5f,
                    "The Order's oldest lesson: a Warden at full speed is the weapon.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura")), Rarity.Rare),
                Item<HomingSparks>("HomingSparks", "Will-o'-Wisps", 15f, 1.2f,
                    "Sparks from the vault forge that still remember who their enemies are.",
                    i => SetRef(i, "projectilePrefab", spark), Rarity.Rare),
                Item<StormAura>("StormAura", "Thunderbird Cloak", 4f, 0.25f,
                    "A storm sealed in a guardian's cloak; it crackles around the Warden while they fly.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura")), Rarity.Epic),
                Item<TetherCoil>("TetherCoil", "Wyrmhide Tether", 25f, 1f,
                    "A coil of the Order's own tether rope, wound with Heartstone wire; it hauls the Warden through anything in the way.",
                    i => { }, Rarity.Legendary),

                // Long-range attack items (2026-11-08): something to fight with from a safe distance.
                Item<SkyboltCrossbow>("SkyboltCrossbow", "Elven Skybow", 14f, 1.1f,
                    "A Warden's crossbow strung with tether wire; it reaches farther than any wing.",
                    i => SetRef(i, "projectilePrefab", shard), Rarity.Common),
                Item<ChainLightning>("ChainLightning", "Stormcaller's Jar", 22f, 1.6f,
                    "A storm caught in a jar; uncork it and it runs from one wing to the next.",
                    i => SetRef(i, "boltMaterial", Mat("M_Beam")), Rarity.Rare),
                Item<Starfall>("Starfall", "Starfall Grimoire", 30f, 2.5f,
                    "An old star chart of the Order; read aloud, its stars come down.",
                    i => SetRef(i, "projectilePrefab", spark), Rarity.Epic),

                // Build items (2026-11-08): one per element, so every school has an attack of its own.
                Item<SkyboltCrossbow>("SalamanderFamiliar", "Salamander Familiar", 11f, 0.9f,
                    "A little fire lizard that rides on your shoulder and spits at whatever comes near.",
                    i => { SetRef(i, "projectilePrefab", spark); SetFloat(i, "range", 30f); SetFloat(i, "boltSpeed", 32f); }, Rarity.Common),
                Item<ShardBurst>("FrostWyrmBreath", "Frost Wyrm's Breath", 9f, 1.4f,
                    "A frost wyrm's last breath, kept in a horn; blow it and winter rushes out ahead of you.",
                    i => { SetRef(i, "projectilePrefab", shard); SetInt(i, "baseShards", 5); SetInt(i, "shardsPerLevel", 1);
                           SetFloat(i, "shardSpeed", 28f); SetFloat(i, "shardLifetime", 0.45f); SetFloat(i, "coneAngle", 50f); }, Rarity.Rare),
                Item<RingPulse>("CrimsonSickle", "Crimson Reaper's Sickle", 34f, 2.6f,
                    "The reaper lent it out once and never got it back; it sweeps a red circle around you.",
                    i => Ring(i, 6.5f, 0.5f, 0.2f, new Color(1f, 0.15f, 0.2f)), Rarity.Epic),
                Item<RingPulse>("ThunderingBell", "Thundering Bell", 10f, 3f,
                    "A storm giant's dinner bell; every ring rolls out as thunder.",
                    i => Ring(i, 11f, 1f, 0.35f, new Color(1f, 0.9f, 0.3f)), Rarity.Common),

                // School charms (2026-11-08): they make one school's effect stronger, the core of a build.
                SchoolCharm("EmberBrazier", "Ember Brazier", School.Flame, 0.25f, "A brazier that never cools; every flame you light burns hotter."),
                SchoolCharm("FrostbiteRune", "Frostbite Rune", School.Frost, 0.06f, "A rune carved from glacier ice; the cold bites deeper into whatever it slows."),
                SchoolCharm("LightningRod", "Lightning Rod", School.Storm, 0.06f, "A copper rod from a storm giant's roof; shocked foes crackle with more pain."),
                SchoolCharm("HemomancerKnot", "Hemomancer's Knot", School.Blood, 0.25f, "A blood mage's knotted cord; wounds you open stay open longer and deeper."),
                SchoolCharm("ArcaneFocus", "Arcane Focus", School.Arcane, 0.1f, "A crystal that hums with spellwork; Arcane items strike harder through it."),
                // Bridge passives (2026-11-08): two schools each, the easy way into a duo.
                Charm("SteamGauntlet", "Steamforged Gauntlet", Rarity.Rare, "A dwarven gauntlet that hisses: fire inside, frost outside.", StatType.Damage),
                Charm("ThunderTotem", "Thunderblood Totem", Rarity.Rare, "A totem painted with storm blood; it drinks from every thunderclap.", StatType.Lifesteal),
                Charm("GlacierHeart", "Glacier Heart", Rarity.Rare, "The frozen heart of a glacier giant, slow and unbreakable.", StatType.MaxHealth, StatType.Armor),
                Charm("WildfireTorch", "Wildfire Torch", Rarity.Rare, "A torch lit from a forest fire that never stopped running.", StatType.FlightSpeed),
                Charm("StormglassOrb", "Stormglass Orb", Rarity.Rare, "A glass orb with a tiny storm inside, humming with spells.", StatType.AttackSpeed),
                Charm("MidasLeech", "Midas Leech", Rarity.Rare, "A golden leech: whatever it drinks turns to coin.", StatType.Lifesteal, StatType.Luck),
                Charm("LuckyHorseshoe", "Unicorn Horseshoe", Rarity.Rare, "A horseshoe from a unicorn; luck and the wind follow its keeper.", StatType.Luck, StatType.FlightSpeed),

                // Passive items (2026-11-08): rarer means more useful. Fantasy names later the same day (ids unchanged).
                Charm("HeartLocket", "Dragonheart Ember", Rarity.Common, "An ember from a dragon's heart, still warm; it beats along with yours.", StatType.MaxHealth),
                Charm("GiantsRing", "Giant's Toe Ring", Rarity.Common, "A hill giant's toe ring, worn as a belt; you feel as sturdy as its owner.", StatType.MaxHealth, StatType.Armor),
                Charm("IronHide", "Troll Hide Cloak", Rarity.Common, "Cured troll hide that still shrugs off blows out of habit.", StatType.Armor),
                Charm("GaleFeather", "Griffin Plume", Rarity.Common, "A plume from a griffin's crest; the wind makes way for its wearer.", StatType.FlightSpeed, StatType.WalkSpeed),
                Charm("Lodestone", "Greedy Imp in a Jar", Rarity.Common, "A little imp who cannot stand to see treasure lying about; it fetches.", StatType.PickupRadius),
                Charm("FairyDust", "Pixie Dust Pouch", Rarity.Common, "A pinch of pixie dust: loot floats to you and lessons sink in.", StatType.PickupRadius, StatType.XPGain),
                Charm("MendingMoss", "Dryad's Plush Bear", Rarity.Common, "A plush bear stuffed with dryad moss; hug it and the scratches close.", StatType.Regen),
                Charm("SagesLens", "Owl Sage's Monocle", Rarity.Common, "An old owl's monocle; through it every fight is a lesson.", StatType.XPGain),
                Charm("KrakenTentacle", "Kraken Tentacle", Rarity.Common, "A dried kraken tentacle braided into the hook rope; it reaches a little farther.", StatType.HookRange, StatType.ZipRange),
                Charm("LeechJar", "Leech in a Jar", Rarity.Common, "A fat, friendly leech; it shares what it drinks.", StatType.Lifesteal),
                Charm("Whetstone", "Dwarven Whetstone", Rarity.Rare, "Every edge you carry comes away dwarf-sharp.", StatType.Damage),
                Charm("QuickDraw", "Hasty Pixie Wings", Rarity.Rare, "Borrowed pixie wings that flutter impatiently; everything you swing, swings sooner.", StatType.AttackSpeed, StatType.AttackSpeed),
                Charm("LuckyCoin", "Leprechaun's Coin", Rarity.Rare, "Always lands on the side of rarer chests, and never stays in your pocket long.", StatType.Luck, StatType.Luck),
                Charm("WizardHourglass", "Archmage's Hourglass", Rarity.Rare, "Sand that runs a little faster for its owner: quicker attacks, quicker Zips.", StatType.AttackSpeed, StatType.ZipCooldown),
                Charm("VampireFang", "Vampire Lord's Fang", Rarity.Rare, "The fang of a vampire lord on a silver chain; what you cut, you drink.", StatType.Lifesteal, StatType.Lifesteal),
                Item<SoulHarvest>("SoulHarvest", "Soul Lantern", 0f, 1f,
                    "A lantern that keeps a spark of every foe you fell, and gives it back as life.", i => { }, Rarity.Rare),
                Item<HuntersEye>("HuntersEye", "Basilisk Eye", 0f, 1f,
                    "A basilisk's eye set in amber; it finds the weak spot of every wing.", i => { }, Rarity.Rare),
                Item<ThornMail>("ThornMail", "Bramble Knight Mail", 15f, 1f,
                    "Mail grown by a bramble knight; whatever bites you bleeds for it.", i => { }, Rarity.Rare),
                Item<WardShell>("WardShell", "Turtle Spirit Shell", 0f, 1f,
                    "The shell of an ancient sky turtle's spirit; it turns one blow aside.",
                    i => SetRef(i, "bubbleMaterial", Mat("M_Aura")), Rarity.Epic),
                Item<WindSigil>("WindSigil", "Sylph Sigil", 0f, 1f,
                    "A sylph's sigil; the air spirits lend you a second breath.", i => { }, Rarity.Epic),
                Item<BerserkerBlood>("BerserkerBlood", "Minotaur Blood", 0f, 1f,
                    "A vial of minotaur blood; the closer to falling, the harder you hit.", i => { }, Rarity.Epic),
                Charm("BloodMoonChalice", "Blood Moon Chalice", Rarity.Epic, "A chalice that fills under the blood moon; drink deep from every blow.",
                    StatType.Lifesteal, StatType.Lifesteal, StatType.Lifesteal, StatType.Damage),
                Item<PhoenixFeather>("PhoenixFeather", "Phoenix Feather", 0f, 1f,
                    "The last feather of the tower's firebird; once in a long while it refuses to let its keeper die.", i => { }, Rarity.Legendary),
                Item<HeartstoneCore>("HeartstoneCore", "Heartstone Core", 40f, 7f,
                    "A shard of the Heartstone itself; while the Warden flies it pulses with light that burns and heals.", i => { }, Rarity.Legendary),
            };
            return items;
        }

        /// <summary>A passive stat charm: the given stat cards, once per item level.</summary>
        private static StatCharm Charm(string id, string displayName, Rarity rarity, string lore, params StatType[] picks) =>
            Item<StatCharm>(id, displayName, 0f, 1f, lore, charm =>
            {
                var serialized = new SerializedObject(charm);
                SerializedProperty list = serialized.FindProperty("picksPerLevel");
                list.arraySize = picks.Length;
                for (int i = 0; i < picks.Length; i++) list.GetArrayElementAtIndex(i).enumValueIndex = (int)picks[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }, rarity);

        /// <summary>The schools of every item: set bonuses, and the status its hits leave (SynergySystem).</summary>
        private static readonly Dictionary<string, School[]> ItemSchools = new Dictionary<string, School[]>
        {
            { "OrbitBlades", new[] { School.Frost, School.Guardian } },
            { "ShardBurst", new[] { School.Frost, School.Arcane } },
            { "ArcLaser", new[] { School.Flame } },
            { "RamStrike", new[] { School.Wild, School.Guardian } },
            { "HomingSparks", new[] { School.Arcane, School.Flame } },
            { "StormAura", new[] { School.Storm } },
            { "TetherCoil", new[] { School.Wild, School.Storm } },
            { "SkyboltCrossbow", new[] { School.Wild, School.Blood } },
            { "ChainLightning", new[] { School.Storm, School.Arcane } },
            { "Starfall", new[] { School.Arcane, School.Flame } },
            { "SalamanderFamiliar", new[] { School.Flame } },
            { "FrostWyrmBreath", new[] { School.Frost } },
            { "CrimsonSickle", new[] { School.Blood } },
            { "ThunderingBell", new[] { School.Storm } },
            { "HeartLocket", new[] { School.Flame, School.Guardian } },
            { "GiantsRing", new[] { School.Guardian } },
            { "IronHide", new[] { School.Guardian, School.Wild } },
            { "GaleFeather", new[] { School.Wild } },
            { "Lodestone", new[] { School.Fortune } },
            { "FairyDust", new[] { School.Fortune, School.Arcane } },
            { "MendingMoss", new[] { School.Wild } },
            { "SagesLens", new[] { School.Arcane, School.Fortune } },
            { "KrakenTentacle", new[] { School.Frost, School.Wild } },
            { "LeechJar", new[] { School.Blood } },
            { "Whetstone", new[] { School.Flame } },
            { "QuickDraw", new[] { School.Wild, School.Arcane } },
            { "LuckyCoin", new[] { School.Fortune } },
            { "WizardHourglass", new[] { School.Arcane } },
            { "VampireFang", new[] { School.Blood } },
            { "SoulHarvest", new[] { School.Blood, School.Arcane } },
            { "HuntersEye", new[] { School.Wild } },
            { "ThornMail", new[] { School.Guardian, School.Blood } },
            { "WardShell", new[] { School.Guardian, School.Frost } },
            { "WindSigil", new[] { School.Storm, School.Wild } },
            { "BerserkerBlood", new[] { School.Blood } },
            { "BloodMoonChalice", new[] { School.Blood } },
            { "PhoenixFeather", new[] { School.Flame } },
            { "HeartstoneCore", new[] { School.Arcane, School.Storm } },
            { "EmberBrazier", new[] { School.Flame } },
            { "FrostbiteRune", new[] { School.Frost } },
            { "LightningRod", new[] { School.Storm } },
            { "HemomancerKnot", new[] { School.Blood } },
            { "ArcaneFocus", new[] { School.Arcane } },
            { "LuckyHorseshoe", new[] { School.Fortune, School.Wild } },
            { "SteamGauntlet", new[] { School.Flame, School.Frost } },
            { "ThunderTotem", new[] { School.Storm, School.Blood } },
            { "GlacierHeart", new[] { School.Frost, School.Guardian } },
            { "WildfireTorch", new[] { School.Flame, School.Wild } },
            { "StormglassOrb", new[] { School.Storm, School.Arcane } },
            { "MidasLeech", new[] { School.Blood, School.Fortune } },
        };

        private static SchoolCharm SchoolCharm(string id, string displayName, School school, float boost, string lore) =>
            Item<SchoolCharm>(id, displayName, 0f, 1f, lore, charm => { SetEnum(charm, "school", (int)school); SetFloat(charm, "boostPerLevel", boost); }, Rarity.Rare);

        private static void Ring(RingPulse pulse, float radius, float radiusPerLevel, float damagePerLevel, Color color)
        {
            SetFloat(pulse, "radius", radius);
            SetFloat(pulse, "radiusPerLevel", radiusPerLevel);
            SetFloat(pulse, "damagePerLevel", damagePerLevel);
            SetRef(pulse, "ringMaterial", ToonSetup.ParticleMaterial());
            var serialized = new SerializedObject(pulse);
            serialized.FindProperty("ringColor").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>What each item does, in one plain sentence (shown on the cards above its stat lines).</summary>
        private static readonly Dictionary<string, string> Summaries = new Dictionary<string, string>
        {
            { "OrbitBlades", "Blades circle you and cut every enemy they pass." },
            { "ShardBurst", "Shards burst out in every direction around you." },
            { "ArcLaser", "A beam sweeps in front of you and burns what it touches." },
            { "RamStrike", "Flying fast, you hit everything you crash into; the faster, the harder." },
            { "HomingSparks", "Sparks fly out and chase the nearest enemies." },
            { "StormAura", "A storm around you keeps hurting enemies close by." },
            { "TetherCoil", "Your Zip cuts through enemies and recharges faster." },
            { "SkyboltCrossbow", "Fires fast bolts at the nearest enemies from far away. Safe at low health." },
            { "ChainLightning", "Lightning strikes a distant enemy and jumps on to the ones near it." },
            { "Starfall", "Stars fall on enemies far away and burst over everything around them." },
            { "HeartLocket", "More max health." },
            { "IronHide", "Armor: every hit hurts less." },
            { "GaleFeather", "Fly and walk faster." },
            { "Lodestone", "Pick up gems and gold from farther away." },
            { "MendingMoss", "Heal more health every second." },
            { "SagesLens", "Earn more experience." },
            { "Whetstone", "All your attacks deal more damage." },
            { "QuickDraw", "All your attack items fire more often." },
            { "LuckyCoin", "Rarer chests appear more often." },
            { "VampireFang", "Heal a share of all the damage you deal." },
            { "LeechJar", "Heal a small share of the damage you deal." },
            { "BloodMoonChalice", "Heal a big share of the damage you deal, and deal more." },
            { "GiantsRing", "More max health and armor." },
            { "FairyDust", "Pick up from farther away and earn more experience." },
            { "WizardHourglass", "Your attack items and your Zip recharge faster." },
            { "KrakenTentacle", "Longer hook and Zip range." },
            { "SalamanderFamiliar", "Spits fireballs at the nearest enemies; they burn." },
            { "FrostWyrmBreath", "Breathes a fan of frost ahead of you that chills everything it hits." },
            { "CrimsonSickle", "A wide sweep around you that makes everything in it bleed." },
            { "ThunderingBell", "Thunder rolls out around you and shocks every enemy it reaches." },
            { "EmberBrazier", "Burns deal more damage. Pairs with Flame items." },
            { "FrostbiteRune", "Chilled enemies take more damage. Pairs with Frost items." },
            { "LightningRod", "Shocked enemies take more damage. Pairs with Storm items." },
            { "HemomancerKnot", "Bleeding deals more damage. Pairs with Blood items." },
            { "ArcaneFocus", "Arcane items deal more damage." },
            { "LuckyHorseshoe", "More luck and faster flight." },
            { "SteamGauntlet", "More damage. Counts for Flame and Frost." },
            { "ThunderTotem", "Lifesteal. Counts for Storm and Blood." },
            { "GlacierHeart", "Health and armor. Counts for Frost and Guardian." },
            { "WildfireTorch", "Faster flight. Counts for Flame and Wild." },
            { "StormglassOrb", "Faster attacks. Counts for Storm and Arcane." },
            { "MidasLeech", "Lifesteal and luck. Counts for Blood and Fortune." },
            { "SoulHarvest", "Heal a little for every enemy you defeat." },
            { "HuntersEye", "A chance for any hit to deal double damage." },
            { "ThornMail", "When you are hit in the air, enemies close by take damage back." },
            { "WardShell", "A shield blocks one hit completely, then recharges." },
            { "WindSigil", "In the air, refills your double jump and dash every few seconds." },
            { "BerserkerBlood", "Deal much more damage while below half health." },
            { "PhoenixFeather", "When your health reaches zero, come back to life once, then it recharges." },
            { "HeartstoneCore", "While you fly, pulses damage every enemy around you and heal you." },
        };

        private static T Item<T>(string id, string displayName, float damage, float cooldown, string lore, System.Action<T> extra, Rarity rarity)
            where T : ItemBase
        {
            var root = new GameObject("PF_Item_" + id);
            var item = root.AddComponent<T>();
            SetString(item, "displayName", displayName);
            SetEnum(item, "rarity", (int)rarity);
            SetString(item, "summary", Summaries.TryGetValue(id, out string summary) ? summary : "");
            if (ItemSchools.TryGetValue(id, out School[] itemSchools))
            {
                var serialized = new SerializedObject(item);
                SerializedProperty list = serialized.FindProperty("schools");
                list.arraySize = itemSchools.Length;
                for (int i = 0; i < itemSchools.Length; i++) list.GetArrayElementAtIndex(i).enumValueIndex = (int)itemSchools[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            SetString(item, "lore", lore);
            SetFloat(item, "baseDamage", damage);
            SetFloat(item, "cooldown", cooldown);
            extra(item);
            return SavePrefab(root, $"{Folder}/PF_Item_{id}.prefab").GetComponent<T>();
        }

        private static Projectile CreateProjectile(string name, PrimitiveType shape, Vector3 scale, string material, float hitRadius)
        {
            var root = new GameObject(name);
            var projectile = root.AddComponent<Projectile>();
            SetFloat(projectile, "hitRadius", hitRadius);
            GameObject visual = Part(root.transform, "Visual", shape, Vector3.zero, scale, Mat(material));
            visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Hookfall.Core.Layers.SetRecursive(root, Hookfall.Core.Layers.NoOutline);
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = Mat(material);
            trail.time = 0.15f;
            trail.startWidth = scale.x * 0.8f;
            trail.endWidth = 0f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            return SavePrefab(root, $"{Folder}/{name}.prefab").GetComponent<Projectile>();
        }

        public static Chest CreateChest()
        {
            var root = new GameObject("PF_Chest");
            var chest = root.AddComponent<Chest>();
            GameObject visual = InstantiateModel("Chest", root.transform);
            Transform lid;
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), Mat("M_ChestWood"));
                Part(visual.transform, "Band", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.24f, 0.12f, 0.84f), Mat("M_ChestTrim"));
                lid = Pivot(visual.transform, "Lid", new Vector3(0f, 0.7f, -0.4f));
                Part(lid, "Top", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0.4f), new Vector3(1.2f, 0.3f, 0.8f), Mat("M_ChestWood"));
                Part(lid, "Lock", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.82f), new Vector3(0.2f, 0.25f, 0.06f), Mat("M_ChestTrim"));
            }
            else lid = EnemyPrefabs.FindDeep(visual.transform, "Lid");
            SetRef(chest, "lid", lid);

            // A spotlight from above and a visible cone of light, so a chest island is found at a glance.
            var lightGo = new GameObject("Spotlight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, BeamHeight, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 26f;
            spot.innerSpotAngle = 14f;
            spot.range = BeamHeight + 8f;
            spot.intensity = 9f;
            spot.color = new Color(1f, 0.86f, 0.55f);
            spot.shadows = LightShadows.None;
            SetRef(chest, "spot", spot);

            var beam = new GameObject("Light Beam") { layer = Hookfall.Core.Layers.NoOutline };
            beam.transform.SetParent(root.transform, false);
            beam.AddComponent<MeshFilter>().sharedMesh = BeamMesh();
            var beamRenderer = beam.AddComponent<MeshRenderer>();
            beamRenderer.sharedMaterial = BeamMaterial();
            beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
            return SavePrefab(root, $"{Root}/Prefabs/World/PF_Chest.prefab").GetComponent<Chest>();
        }

        private const float BeamHeight = 14f;

        /// <summary>An open cone, narrow at the top and wide at the chest, fading in towards the ground.</summary>
        private static Mesh BeamMesh()
        {
            string path = Root + "/Art/Meshes/MESH_ChestBeam.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            EnsureFolder(Root + "/Art/Meshes");
            const int segments = 24;
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[segments * 6];
            Color top = new Color(1f, 0.86f, 0.5f, 0f), bottom = new Color(1f, 0.86f, 0.5f, 0.32f);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i] = dir * 0.35f + Vector3.up * BeamHeight;
                vertices[segments + i] = dir * 2.4f + Vector3.up * 0.05f;
                colors[i] = top;
                colors[segments + i] = bottom;
                int n = (i + 1) % segments, t = i * 6;
                triangles[t] = i; triangles[t + 1] = n; triangles[t + 2] = segments + i;
                triangles[t + 3] = n; triangles[t + 4] = segments + n; triangles[t + 5] = segments + i;
            }
            var mesh = new Mesh { name = "MESH_ChestBeam", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Unlit, see-through both ways, coloured by the mesh's vertex colours.</summary>
        private static Material BeamMaterial()
        {
            string path = MaterialsPath + "/M_LightBeam.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);   // additive: light adds up, never darkens
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>The cache box and its beam, as children of the TowerCache object in the scene.</summary>
        public static void BuildCacheVisuals(TowerCache cache)
        {
            GameObject visual = InstantiateModel("TowerCache", cache.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(cache.transform, false);
                Part(visual.transform, "Box", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 1f), Mat("M_ChestTrim"));
                Part(visual.transform, "Gem", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.5f, Mat("M_CacheGlow"));
            }
            GameObject beam = Part(cache.transform, "Beam", PrimitiveType.Cylinder, new Vector3(0f, 60f, 0f), new Vector3(1.6f, 60f, 1.6f), Mat("M_CacheBeam"));
            beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            SetRef(cache, "cacheVisual", visual);
            SetRef(cache, "beam", beam);
        }

        private static void Transparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
