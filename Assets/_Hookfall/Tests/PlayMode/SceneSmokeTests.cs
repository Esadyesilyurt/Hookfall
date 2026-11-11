// Hookfall - play-mode smoke tests: each scene loads and runs with no errors in the Console (CLAUDE.md: main must play clean).
// Game types are reached by reflection because a test assembly cannot reference Assembly-CSharp.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hookfall.Tests
{
    public class SceneSmokeTests
    {
        private readonly RecordGuard record = new RecordGuard();

        [SetUp]
        public void SaveRecord()
        {
            record.Save();
            // Every test starts as the Warden (one double jump, one dash), whoever the player picked last: two tests
            // failed on 2026-11-10 because the saved choice was Lumen. TearDown puts the player's choice back.
            PlayerPrefs.SetString("hf_character", "Warden");
        }
        [TearDown] public void RestoreRecord() => record.Restore();

        /// <summary>A test Warden hovers in place, which the horde punishes fast; keep it alive so the run goes on.</summary>
        private static void KeepAlive(GameObject player)
        {
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);
            statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
            // A fall into the abyss would end the run and stop time, and a test waiting for a physics step would hang.
            statsType.GetMethod("SetAbyssKillHeight").Invoke(stats, new object[] { -100000f });
            NoBoss();
        }

        /// <summary>Keeps the stage boss away: its entrance stops time, and a test waiting on a physics step would hang.</summary>
        internal static void NoBoss()
        {
            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            object stage = stageType.GetProperty("Instance").GetValue(null);
            if (stage != null) stageType.GetField("firstBossGold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(stage, 100000000);
        }

        /// <summary>Moves the Warden at once: setting only the transform can be undone by interpolation before the next physics step.</summary>
        private static void Teleport(GameObject player, Vector3 position)
        {
            player.GetComponent<Rigidbody>().position = position;
            player.transform.position = position;
        }

        [UnityTest]
        public IEnumerator MainMenuLoadsClean()
        {
            SceneManager.LoadScene("MainMenu");
            yield return new WaitForSecondsRealtime(3f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ArenaRunsClean()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Arena has no Player");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            // Keep the player in the air above the tower for a while so combat, kills and pickups all run.
            float end = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < end)
            {
                var body = player.GetComponent<Rigidbody>();
                KeepAlive(player);
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    body.linearVelocity = Vector3.zero;
                }
                // Answer any level-up screen with the first card.
                if (Time.timeScale == 0f && levelUp != null)
                    levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            int kills = (int)gmType.GetProperty("Kills").GetValue(gmType.GetProperty("Instance").GetValue(null));
            Assert.Greater(kills, 0, "25 s in the air with Orbit Blades should kill at least one bat");
        }

        /// <summary>Gives every item through the chest code, flies for a while, then opens the tower cache.</summary>
        [UnityTest]
        public IEnumerator AllItemsAndTowerCacheRunClean()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            GameObject player = GameObject.Find("Player");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type rarityType = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component stats = player.GetComponent(statsType);
            var grant = combatType.GetMethod("Grant");
            for (int i = 0; i < 200; i++) grant.Invoke(combat, new[] { Enum.ToObject(rarityType, 2), (object)stats });   // 40 left a 6% chance of five attack items
            var items = (ICollection)combatType.GetProperty("Items").GetValue(combat);
            int passives = 0;
            foreach (Component item in items) if ((bool)item.GetType().GetProperty("IsPassive").GetValue(item)) passives++;
            Assert.AreEqual(6, items.Count - passives, "Chests should fill all six attack slots");
            Assert.AreEqual(6, passives, "Chests should fill all six passive slots");

            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            object gm = gmType.GetProperty("Instance").GetValue(null);
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            float end = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < end)
            {
                KeepAlive(player);
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(25f, 0f, 0f);   // fast enough for Ram Strike
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }

            // Tower cache: make it ready, stand on the tower top for 5 s.
            Type cacheType = Type.GetType("Hookfall.World.TowerCache, Assembly-CSharp");
            var cache = (Component)UnityEngine.Object.FindAnyObjectByType(cacheType);
            cacheType.GetMethod("SetReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(cache, new object[] { true });
            var ready = cacheType.GetProperty("Ready");
            bool opened = false;
            end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end && !opened)
            {
                opened = !(bool)ready.GetValue(cache);   // the cache stops being ready once it opens
                if (opened)
                {
                    Type enemyBase = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
                    foreach (Component e in (IList)enemyBase.GetField("Active").GetValue(null))
                        Assert.Greater(Vector3.Distance(e.transform.position, cache.transform.position), 45f, "Opening the cache should destroy every enemy within 45 m");
                }
                if (Time.timeScale == 0f && levelUp != null) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                KeepAlive(player);
                player.transform.position = cache.transform.position + new Vector3(1.2f, 1.02f, 0f);
                player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                yield return null;
            }
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Assert.IsTrue(opened, "Standing on the tower top for 5 s should open the tower cache. " +
                $"State {gmType.GetProperty("State").GetValue(gm)}, timeScale {Time.timeScale}, " +
                $"airborne {pcType.GetProperty("IsAirborne").GetValue(player.GetComponent(pcType))}, " +
                $"player {player.transform.position}, cache {cache.transform.position}");
            gmType.GetMethod("CloseModal").Invoke(gm, null);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Right-click Zip (power swing): held from 35 m away, it swings and then reels the Warden in close.</summary>
        [UnityTest]
        public IEnumerator ZipHaulsThePlayer()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type hookType = Type.GetType("Hookfall.Player.GrappleHook, Assembly-CSharp");
            Component hook = player.GetComponent(hookType);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Islands are random: find a clear spot about 35 m out with a free line of sight to the tower.
            Vector3 target = new Vector3(0f, 10f, 0f);
            Vector3 spot = Vector3.zero;
            for (int i = 0; i < 72 && spot == Vector3.zero; i++)
            {
                float a = i * 25f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 35f, 12f + (i % 4) * 5f, Mathf.Sin(a) * 35f);
                if (Physics.CheckSphere(p, 3f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Raycast(p, (target - p).normalized, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore)
                    && Vector3.Distance(hit.point, target) < 6f) spot = p;   // the first thing hit is the tower
            }
            Assert.AreNotEqual(Vector3.zero, spot, "No clear spot with a view of the tower");
            player.transform.position = spot;
            player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
            var aim = new GameObject("Test Aim").transform;
            aim.position = player.transform.position;
            aim.LookAt(target);
            hookType.GetField("aimSource", flags).SetValue(hook, aim);

            float start = Vector3.Distance(player.transform.position, target);
            hookType.GetMethod("TryThrow", flags).Invoke(hook, new object[] { true });
            int chargesAfterThrow = (int)hookType.GetProperty("ZipCharges").GetValue(hook);
            var zipping = hookType.GetProperty("IsZipping");
            bool sawZip = false;
            float closest = start;
            float end = Time.realtimeSinceStartup + 6f;   // with no mouse the power swing is "held" until zipMaxTime
            while (Time.realtimeSinceStartup < end)
            {
                bool z = (bool)zipping.GetValue(hook);
                sawZip |= z;
                closest = Mathf.Min(closest, Vector3.Distance(player.transform.position, target));
                if (sawZip && !z) break;
                yield return null;
            }
            UnityEngine.Object.Destroy(aim.gameObject);
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(sawZip, "The Zip never started");
            Assert.Less(closest, start - 20f, "The Zip should haul the Warden most of the way to the anchor");
            Assert.AreEqual(0, chargesAfterThrow, "The Zip should use its charge");
        }

        /// <summary>Wall jump: touching the tower's side in the air, Space throws the Warden up and away from the wall.</summary>
        [UnityTest]
        public IEnumerator WallJumpPushesOffAndUp()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Press into the tower's side at mid height until the wall contact registers.
            bool touching = false;
            float end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end && !touching)
            {
                KeepAlive(player);
                player.transform.position = new Vector3(0f, 8f, -3.65f);
                body.linearVelocity = new Vector3(0f, 0f, 4f);
                yield return new WaitForFixedUpdate();
                touching = (bool)pcType.GetProperty("TouchingWall").GetValue(controller);
            }
            Assert.IsTrue(touching, "Pressing into the tower side should count as touching a wall");

            pcType.GetField("wallJumpQueued", flags).SetValue(controller, true);   // Space, without a keyboard
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            LogAssert.NoUnexpectedReceived();
            Assert.Greater(body.linearVelocity.y, 8f, "A wall jump should launch upwards");
            Assert.Less(body.linearVelocity.z, 0f, "A wall jump should push away from the wall");
        }

        /// <summary>Air tricks: flying fast with no rope out, the Warden starts a flip.</summary>
        [UnityTest]
        public IEnumerator FastFlightStartsATrick()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type animatorType = Type.GetType("Hookfall.Player.WardenAnimator, Assembly-CSharp");
            var animator = player.GetComponentInChildren(animatorType);
            Assert.IsNotNull(animator, "The Warden model should have a WardenAnimator");
            bool started = false;
            Action onTrick = () => started = true;
            var trickEvent = animatorType.GetEvent("TrickStarted");
            trickEvent.AddEventHandler(animator, onTrick);

            float end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end && !started)
            {
                KeepAlive(player);
                player.transform.position = new Vector3(0f, 60f, -60f);   // high and clear of islands
                player.GetComponent<Rigidbody>().linearVelocity = new Vector3(30f, 2f, 0f);
                yield return null;
            }
            trickEvent.RemoveEventHandler(animator, onTrick);
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(started, "Flying at 30 m/s with no rope should start a trick");
        }

        /// <summary>Double jump and dash: one of each per flight, both refilled on landing.</summary>
        [UnityTest]
        public IEnumerator DoubleJumpAndDashOncePerFlight()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Func<string, int> left = name => (int)pcType.GetProperty(name).GetValue(controller);

            // Start on an island so both moves are filled, then fly high and clear of everything.
            float end = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < end && (bool)pcType.GetProperty("IsAirborne").GetValue(controller))
            {
                KeepAlive(player);
                yield return new WaitForFixedUpdate();
            }
            Assert.AreEqual(1, left("AirJumpsLeft"), "Standing on an island should fill the double jump");
            Assert.AreEqual(1, left("DashesLeft"), "Standing on an island should fill the dash");
            player.transform.position = new Vector3(0f, 70f, -60f);
            body.linearVelocity = new Vector3(0f, -5f, 0f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            pcType.GetField("airJumpQueued", flags).SetValue(controller, true);   // Space, without a keyboard
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.y, 7f, "A double jump should turn a fall into a climb");
            Assert.AreEqual(0, left("AirJumpsLeft"), "The double jump should be spent");

            body.linearVelocity = new Vector3(0f, -5f, 0f);
            pcType.GetField("airJumpQueued", flags).SetValue(controller, true);
            yield return new WaitForFixedUpdate();
            Assert.Less(body.linearVelocity.y, 0f, "A second double jump in the same flight should do nothing");

            pcType.GetField("dashQueued", flags).SetValue(controller, true);   // Left Shift
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.magnitude, 25f, "A dash should launch the Warden");
            Assert.AreEqual(0, left("DashesLeft"), "The dash should be spent");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Looking straight up from an island: the camera dips under the Warden but stays out of the island.</summary>
        [UnityTest]
        public IEnumerator CameraLooksUpWithoutEnteringIslands()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            float end = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < end && (bool)pcType.GetProperty("IsAirborne").GetValue(controller))
            {
                KeepAlive(player);
                yield return null;
            }
            Assert.IsFalse((bool)pcType.GetProperty("IsAirborne").GetValue(controller), "The Warden should start on an island");

            // Tilt the orbit to the bottom of its range, as a mouse pushed all the way would.
            Type orbitType = Type.GetType("Unity.Cinemachine.CinemachineOrbitalFollow, Unity.Cinemachine");
            Component orbit = GameObject.Find("Player Camera").GetComponent(orbitType);
            var axisField = orbitType.GetField("VerticalAxis");
            object axis = axisField.GetValue(orbit);
            float bottom = ((Vector2)axis.GetType().GetField("Range").GetValue(axis)).x;
            Assert.LessOrEqual(bottom, -75f, "The camera should be able to go far enough under the Warden to look up");
            axis.GetType().GetField("Value").SetValue(axis, bottom);
            axisField.SetValue(orbit, axis);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                KeepAlive(player);
                yield return null;
            }
            Vector3 cam = Camera.main.transform.position;
            Vector3 head = player.transform.position + Vector3.up * 1.6f;
            Assert.Less(cam.y, head.y, "Looking up, the camera should sit below the Warden's head");
            Assert.IsFalse(Physics.Linecast(head, cam, 1 << 9, QueryTriggerInteraction.Ignore),
                "No island, tower or prop should stand between the camera and the Warden");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>A fast Warden: bats chase at most of its speed, and faster when far behind.</summary>
        [UnityTest]
        public IEnumerator BatsKeepUpWithAFastWarden()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            var active = (IList)enemyType.GetField("Active").GetValue(null);
            Component bat = null;
            float end = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < end && bat == null)
            {
                KeepAlive(player);
                foreach (Component e in active) if (e.GetType().Name == "Bat") { bat = e; break; }
                yield return null;
            }
            Assert.IsNotNull(bat, "Bats should spawn");

            player.transform.position = new Vector3(0f, 70f, -60f);
            player.GetComponent<Rigidbody>().linearVelocity = new Vector3(50f, 0f, 0f);
            yield return new WaitForFixedUpdate();
            float speed = (float)enemyType.GetMethod("ChaseSpeed").Invoke(bat, new object[] { 18f });
            Assert.GreaterOrEqual(speed, 0.75f * player.GetComponent<Rigidbody>().linearVelocity.magnitude,
                "A bat should chase at most of a fast Warden's speed");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>A chest offers 3 different choices, pauses the game, and gives only the one picked.</summary>
        [UnityTest]
        public IEnumerator ChestOffersThreeChoices()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type rarityType = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            Type uiType = Type.GetType("Hookfall.UI.ChestResultUI, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component stats = player.GetComponent(statsType);
            var items = (ICollection)combatType.GetProperty("Items").GetValue(combat);
            int before = items.Count;

            uiType.GetMethod("Offer").Invoke(null, new[] { combat, (object)stats, Enum.ToObject(rarityType, 1) });
            object ui = uiType.GetProperty("Instance").GetValue(null);
            var choices = (ICollection)uiType.GetField("choices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ui);
            Assert.AreEqual(3, choices.Count, "A chest should offer 3 choices");
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (object choice in choices)
            {
                var item = (Component)choice.GetType().GetField("Item").GetValue(choice);
                names.Add(item != null ? item.name : "stat " + choice.GetType().GetField("FallbackStat").GetValue(choice));
            }
            Assert.AreEqual(3, names.Count, "The 3 choices should all be different");
            Assert.AreEqual(0f, Time.timeScale, "The chest screen should pause the game");
            Assert.AreEqual(before, items.Count, "Nothing should be given before a pick");

            object picked = null;
            int k = 0;
            foreach (object choice in choices) if (k++ == 1) picked = choice;
            bool pickedNew = (bool)picked.GetType().GetField("IsNewItem").GetValue(picked);
            yield return new WaitForSecondsRealtime(0.4f);   // past the accidental-pick guard
            uiType.GetMethod("Pick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(ui, new object[] { 1 });
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "Picking should resume the game");
            Assert.AreEqual(before + (pickedNew ? 1 : 0), items.Count, "Only the picked choice should be given");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Ledge climb: flying into an island's side a little below its top lands the Warden on that island.</summary>
        [UnityTest]
        public IEnumerator LedgeClimbLandsOnTheIsland()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            Type islandType = Type.GetType("Hookfall.World.Island, Assembly-CSharp");
            var all = (IList)islandType.GetField("All").GetValue(null);
            bool climbed = false;
            Action onClimb = () => climbed = true;
            var climbEvent = pcType.GetEvent("LedgeClimbed");
            climbEvent.AddEventHandler(controller, onClimb);
            GameObject sky = GameObject.Find("Sky Features");
            if (sky != null) sky.SetActive(false);   // an updraft beside the rim would lift the Warden past the side

            // Try inner islands until one has a clear side: start 3 m below the top, 1.5 m out from the rim, flying in.
            Component landedOn = null;
            foreach (Component island in all)
            {
                if (islandType.GetProperty("Kind").GetValue(island).ToString() != "Inner") continue;
                Vector3 top = (Vector3)islandType.GetProperty("Top").GetValue(island);
                float width = (float)islandType.GetProperty("Width").GetValue(island);
                Vector3 outward = new Vector3(1f, 0f, 0.3f).normalized;
                climbed = false;
                float end = Time.realtimeSinceStartup + 3f;
                player.transform.position = top + outward * (width * 0.5f + 1.5f) + Vector3.down * 3f;
                body.linearVelocity = -outward * 8f;
                yield return new WaitForFixedUpdate();
                while (Time.realtimeSinceStartup < end && landedOn == null)
                {
                    KeepAlive(player);
                    if (!climbed && !(bool)pcType.GetProperty("IsAirborne").GetValue(controller)) break;   // landed somewhere else first
                    if (climbed && !(bool)pcType.GetProperty("IsAirborne").GetValue(controller))
                        landedOn = (Component)pcType.GetProperty("CurrentIsland").GetValue(controller);
                    yield return new WaitForFixedUpdate();
                }
                if (landedOn != null) break;
            }
            climbEvent.RemoveEventHandler(controller, onClimb);
            LogAssert.NoUnexpectedReceived();
            Assert.IsNotNull(landedOn, "Flying into an island side 3 m below its top should climb onto an island");
        }

        /// <summary>The tower: flying into the parapet 2 m below the deck climbs the Warden onto the deck.</summary>
        [UnityTest]
        public IEnumerator LedgeClimbReachesTheTowerTop()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            Type islandType = Type.GetType("Hookfall.World.Island, Assembly-CSharp");
            GameObject sky = GameObject.Find("Sky Features");
            if (sky != null) sky.SetActive(false);
            Component tower = null;
            foreach (Component island in (IList)islandType.GetField("All").GetValue(null))
                if (island.name == "Tower") tower = island;
            Assert.IsNotNull(tower, "The arena needs a tower");
            Vector3 top = (Vector3)islandType.GetProperty("Top").GetValue(tower);
            Assert.Greater(top.y, 10f, "The tower's top should be its deck, not its base");
            float width = (float)islandType.GetProperty("Width").GetValue(tower);
            Vector3 outward = new Vector3(1f, 0f, 0.3f).normalized;
            Vector3 start = top + outward * (width * 0.5f + 1.5f) + Vector3.down * 2f;
            player.transform.position = start;
            body.position = start;
            body.linearVelocity = -outward * 8f;
            Component landedOn = null;
            float end = Time.realtimeSinceStartup + 3f;
            yield return new WaitForFixedUpdate();
            while (Time.realtimeSinceStartup < end && landedOn == null)
            {
                KeepAlive(player);
                if (!(bool)pcType.GetProperty("IsAirborne").GetValue(controller))
                    landedOn = (Component)pcType.GetProperty("CurrentIsland").GetValue(controller);
                yield return new WaitForFixedUpdate();
            }
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(tower, landedOn, "Flying into the tower's parapet should climb onto the deck");
            Assert.Greater(body.position.y, top.y - 0.5f, "The Warden should stand on the deck");
        }

        /// <summary>Full attack slots: a chest still offers new items, and one takes the place of the item given up.</summary>
        [UnityTest]
        public IEnumerator NewItemReplacesAChosenItemWhenFull()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type rewardType = Type.GetType("Hookfall.Combat.ChestReward, Assembly-CSharp");
            Type statType = Type.GetType("Hookfall.Player.StatType, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type rarityType = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component stats = player.GetComponent(statsType);
            var items = (IList)combatType.GetProperty("Items").GetValue(combat);
            Func<Component, bool> passive = c => (bool)c.GetType().GetProperty("IsPassive").GetValue(c);
            Func<Component, object> newReward = prefab => Activator.CreateInstance(rewardType,
                prefab.GetType().GetProperty("Rarity").GetValue(prefab), prefab, true, 1, Enum.ToObject(statType, 0));
            var claim = combatType.GetMethod("Claim");
            var owned = new System.Collections.Generic.HashSet<string>();
            foreach (Component item in items) owned.Add(item.name);
            Component spare = null;
            foreach (Component prefab in (IList)combatType.GetField("itemPrefabs", flags).GetValue(combat))
            {
                if (passive(prefab) || owned.Contains(prefab.name)) continue;
                int attacks = 0;
                foreach (Component item in items) if (!passive(item)) attacks++;
                if (attacks < 6) { claim.Invoke(combat, new[] { newReward(prefab), stats, null }); owned.Add(prefab.name); }
                else if (spare == null) spare = prefab;
            }
            Assert.IsNotNull(spare, "There should be more attack items than slots");
            Assert.IsFalse((bool)combatType.GetMethod("HasFreeSlot").Invoke(combat, new object[] { false }), "Attack slots should be full");

            bool offersNew = false;
            var roll = combatType.GetMethod("RollChest");
            for (int i = 0; i < 20 && !offersNew; i++)
                foreach (object r in (IList)roll.Invoke(combat, new[] { Enum.Parse(rarityType, "Common") }))
                    if ((bool)rewardType.GetField("IsNewItem").GetValue(r)) offersNew = true;
            Assert.IsTrue(offersNew, "Chests should still offer new items when the slots are full");

            int count = items.Count;
            int slot = -1;
            for (int i = 0; i < items.Count; i++) if (!passive((Component)items[i])) slot = i;
            Component given = (Component)items[slot];
            string givenName = given.name;
            claim.Invoke(combat, new[] { newReward(spare), stats, given });
            yield return null;
            Assert.AreEqual(count, items.Count, "Replacing should keep the number of items");
            Assert.AreEqual(spare.name, ((Component)items[slot]).name, "The new item should take the given-up item's slot");
            foreach (Component item in items) Assert.AreNotEqual(givenName, item.name, "The given-up item should be gone");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Speed cap: 40 m/s before Flight speed picks; Zip and release boosts add at most 15%.</summary>
        [UnityTest]
        public IEnumerator AirSpeedIsCapped()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            float cap = (float)pcType.GetProperty("MaxAirSpeed").GetValue(controller);
            Assert.AreEqual(40f, cap, 0.01f, "The base air speed cap should be 40 m/s");

            player.transform.position = new Vector3(0f, 70f, -60f);
            pcType.GetMethod("Boost").Invoke(controller, new object[] { 500f });
            body.linearVelocity = new Vector3(200f, 0f, 0f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(body.linearVelocity.magnitude, cap * 1.15f + 1f, "Boosts should not lift the speed far past the cap");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Sky features: every kind is placed; updrafts lift, channels carry, crystals refill, anchors take the hook.</summary>
        [UnityTest]
        public IEnumerator SkyFeaturesWork()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;   // SkyFeatures places everything in Start
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type pcType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            Component controller = player.GetComponent(pcType);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject sky = GameObject.Find("Sky Features");
            Assert.IsNotNull(sky, "The arena should have Sky Features");
            Func<string, Component[]> find = name => sky.GetComponentsInChildren(Type.GetType($"Hookfall.World.{name}, Assembly-CSharp"));
            Component[] updrafts = find("Updraft"), channels = find("AirChannel"), crystals = find("RefillCrystal"), anchors = find("HookAnchor");
            Assert.Greater(updrafts.Length, 0, "No updraft was placed");
            Assert.Greater(channels.Length, 0, "No air channel was placed");
            Assert.Greater(crystals.Length, 0, "No refill crystal was placed");
            Assert.Greater(anchors.Length, 0, "No hook anchor was placed");

            // Updraft: from rest a third of the way up the column, the Warden rises.
            Component updraft = updrafts[0];
            Vector3 bottom = (Vector3)updraft.GetType().GetProperty("Bottom").GetValue(updraft);
            float height = (float)updraft.GetType().GetProperty("Height").GetValue(updraft);
            Teleport(player, bottom + Vector3.up * (height * 0.3f));
            body.linearVelocity = Vector3.zero;
            for (float t = 0f; t < 0.5f; t += Time.fixedDeltaTime)
            {
                KeepAlive(player);
                yield return new WaitForFixedUpdate();
            }
            object airborne = pcType.GetProperty("IsAirborne").GetValue(controller);
            object island = pcType.GetProperty("CurrentIsland").GetValue(controller);
            Assert.Greater(body.linearVelocity.y, 1f, $"An updraft should lift the Warden (at {body.position}, column {bottom} + {height} m, " +
                $"airborne {airborne}, island {island}, kinematic {body.isKinematic})");

            // Air channel: from rest at its entry, the Warden is carried along it.
            Component channel = channels[0];
            Vector3 entry = (Vector3)channel.GetType().GetProperty("Entry").GetValue(channel);
            Vector3 dir = ((Vector3)channel.GetType().GetProperty("Exit").GetValue(channel) - entry).normalized;
            Teleport(player, entry + dir);
            body.linearVelocity = Vector3.zero;
            for (float t = 0f; t < 0.6f; t += Time.fixedDeltaTime)
            {
                KeepAlive(player);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(Vector3.Dot(body.position - entry, dir), 8f, "An air channel should carry the Warden along it");

            // Refill crystal: with both moves spent, touching it refills them and it goes away for a while.
            Component crystal = crystals[0];
            pcType.GetField("airJumpsLeft", flags).SetValue(controller, 0);
            pcType.GetField("dashesLeft", flags).SetValue(controller, 0);
            bool taken = false;
            for (int i = 0; i < 20 && !taken; i++)
            {
                KeepAlive(player);
                Teleport(player, crystal.transform.position);
                body.linearVelocity = Vector3.zero;
                yield return new WaitForFixedUpdate();
                yield return null;
                taken = !(bool)crystal.GetType().GetProperty("Ready").GetValue(crystal);
            }
            Assert.IsTrue(taken, "Touching a refill crystal in flight should use it");
            Assert.AreEqual(1, (int)pcType.GetProperty("AirJumpsLeft").GetValue(controller), "The crystal should refill the double jump");
            Assert.AreEqual(1, (int)pcType.GetProperty("DashesLeft").GetValue(controller), "The crystal should refill the dash");

            // Hook anchor: the hook accepts its collider.
            Type hookType = Type.GetType("Hookfall.Player.GrappleHook, Assembly-CSharp");
            Collider rock = anchors[0].GetComponentInChildren<Collider>();
            Assert.IsNotNull(rock, "A hook anchor needs a collider");
            Assert.IsTrue((bool)hookType.GetMethod("IsHookable").Invoke(null, new object[] { rock }), "The hook should catch a hook anchor");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Every item and passive at once: all run clean; the Ward Shell blocks a hit and the Phoenix Feather revives.</summary>
        [UnityTest]
        public IEnumerator EveryItemAndPassiveRunsClean()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type rewardType = Type.GetType("Hookfall.Combat.ChestReward, Assembly-CSharp");
            Type statType = Type.GetType("Hookfall.Player.StatType, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component stats = player.GetComponent(statsType);
            var prefabs = (IList)combatType.GetField("itemPrefabs", flags).GetValue(combat);
            var items = (ICollection)combatType.GetProperty("Items").GetValue(combat);

            // Claim every item directly (past the slot limits) so each one runs at least once.
            var owned = new System.Collections.Generic.HashSet<string>();
            foreach (Component item in items) owned.Add(item.name);
            foreach (Component prefab in prefabs)
            {
                if (owned.Contains(prefab.name)) continue;
                object reward = Activator.CreateInstance(rewardType, prefab.GetType().GetProperty("Rarity").GetValue(prefab), prefab, true, 1, Enum.ToObject(statType, 0));
                combatType.GetMethod("Claim").Invoke(combat, new[] { reward, stats, null });
            }
            Assert.AreEqual(prefabs.Count, items.Count, "Every item should be owned");
            yield return null;

            // Ward Shell: the first hit is blocked.
            var health = statsType.GetProperty("CurrentHealth");
            var takeDamage = statsType.GetMethod("TakeDamage");
            var lastHit = statsType.GetField("lastHitTime", flags);
            lastHit.SetValue(stats, -999f);
            float before = (float)health.GetValue(stats);
            takeDamage.Invoke(stats, new object[] { 30f });
            Assert.AreEqual(before, (float)health.GetValue(stats), 0.01f, "The Ward Shell should block the first hit");
            Assert.AreEqual(0, (int)statsType.GetProperty("ShieldCharges").GetValue(stats), "The Ward Shell's charge should be spent");

            // Phoenix Feather: a killing blow revives instead.
            lastHit.SetValue(stats, -999f);
            takeDamage.Invoke(stats, new object[] { 100000f });
            Assert.Greater((float)health.GetValue(stats), 0f, "The Phoenix Feather should revive the Warden");
            Assert.AreEqual(0, (int)statsType.GetProperty("Revives").GetValue(stats), "The Phoenix Feather's revive should be spent");

            // Fly around with everything working.
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);
            float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end)
            {
                KeepAlive(player);
                if (player.transform.position.y < 6f)
                {
                    Teleport(player, new Vector3(0f, 12f, -10f));
                    player.GetComponent<Rigidbody>().linearVelocity = new Vector3(25f, 0f, 0f);
                }
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Rarer chests offer rarer items: a Legendary chest's choices start with Legendary items.</summary>
        [UnityTest]
        public IEnumerator LegendaryChestOffersLegendaryItems()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type rarityType = Type.GetType("Hookfall.Combat.Rarity, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            var offer = (IList)combatType.GetMethod("RollChest").Invoke(combat, new[] { Enum.ToObject(rarityType, 3) });
            Assert.AreEqual(3, offer.Count, "A chest offers 3 choices");
            foreach (object choice in offer)
            {
                var item = (Component)choice.GetType().GetField("Item").GetValue(choice);
                Assert.IsNotNull(item, "A Legendary chest early in a run should offer items");
                Assert.AreEqual("Legendary", item.GetType().GetProperty("Rarity").GetValue(item).ToString(),
                    $"{item.name} should be Legendary: there are three Legendary items and none is owned yet");
                Assert.AreEqual(1, (int)choice.GetType().GetField("LevelsGained").GetValue(choice), "Rarer chests give rarer items, not more levels");
            }
        }

        /// <summary>Characters: an unlocked pick gets its model, health and starting items; a locked pick falls back to the Warden.</summary>
        [UnityTest]
        public IEnumerator CharacterLoadoutAppliesTheChosenCharacter()
        {
            PlayerPrefs.SetInt("hf_total_kills", 100000);   // Bulwark's quest done (RecordGuard restores it)
            PlayerPrefs.SetString("hf_character", "Bulwark");
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            GameObject player = GameObject.Find("Player");
            Assert.IsTrue(player.transform.Find("Bulwark").gameObject.activeSelf, "The Bulwark model should be shown");
            Assert.IsFalse(player.transform.Find("Warden").gameObject.activeSelf, "The Warden model should be hidden");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Assert.AreEqual(150f, (float)statsType.GetProperty("MaxHealth").GetValue(player.GetComponent(statsType)), 0.01f, "Bulwark has 1.5x health");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            var names = new System.Collections.Generic.List<string>();
            foreach (Component item in (ICollection)combatType.GetProperty("Items").GetValue(player.GetComponent(combatType))) names.Add(item.name);
            CollectionAssert.Contains(names, "PF_Item_WardShell", "Bulwark starts with the Ward Shell");

            PlayerPrefs.SetInt("hf_max_level", 0);   // Lumen's quest not done
            PlayerPrefs.SetString("hf_character", "Lumen");
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            player = GameObject.Find("Player");
            Assert.IsTrue(player.transform.Find("Warden").gameObject.activeSelf, "A locked character should fall back to the Warden");
            Assert.IsFalse(player.transform.Find("Lumen").gameObject.activeSelf, "A locked character's model stays hidden");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Language: with Turkish chosen, the main menu shows Turkish labels.</summary>
        [UnityTest]
        public IEnumerator TurkishMenuIsTranslated()
        {
            Type locType = Type.GetType("Hookfall.Core.Loc, Assembly-CSharp");
            locType.GetProperty("Turkish").SetValue(null, true);
            try
            {
                SceneManager.LoadScene("MainMenu");
                yield return null;
                var texts = new System.Collections.Generic.List<string>();
                // The test assembly has no TextMeshPro reference: read every text component by reflection.
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (Component c in root.GetComponentsInChildren<Component>(true))
                        if (c != null && c.GetType().Name == "TextMeshProUGUI") texts.Add((string)c.GetType().GetProperty("text").GetValue(c));
                CollectionAssert.Contains(texts, "Oyna", "The Play button should read Oyna in Turkish");
                CollectionAssert.Contains(texts, "BEKÇİNİ SEÇ", "The character select title should be Turkish");
                LogAssert.NoUnexpectedReceived();
            }
            finally { locType.GetProperty("Turkish").SetValue(null, false); }
        }

        /// <summary>Builds: the Warden's Guardian affinity plus his blades reach Guardian 2; burns hurt and chills slow;
        /// with four Storm items a shocked enemy's death sets off a lightning burst.</summary>
        [UnityTest]
        public IEnumerator SynergiesAndStatusesWork()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type synergyType = Type.GetType("Hookfall.Combat.SynergySystem, Assembly-CSharp");
            Type schoolType = Type.GetType("Hookfall.Combat.School, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component synergy = player.GetComponent(synergyType);
            Assert.IsNotNull(synergy, "The player needs a SynergySystem");
            Func<string, int> count = school => (int)synergyType.GetMethod("Count").Invoke(synergy, new[] { Enum.Parse(schoolType, school) });
            Assert.GreaterOrEqual(count("Guardian"), 2, "Guardian affinity + Moonblade Halo should make Guardian 2");
            Assert.GreaterOrEqual((float)statsType.GetProperty("Armor").GetValue(player.GetComponent(statsType)), 3f, "Guardian 2 gives +3 armor");

            // Statuses on a live enemy.
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            var active = (IList)enemyType.GetField("Active").GetValue(null);
            Component enemy = null;
            float end = Time.realtimeSinceStartup + 8f;
            while (enemy == null && Time.realtimeSinceStartup < end) { KeepAlive(player); if (active.Count > 0) enemy = (Component)active[0]; yield return null; }
            Assert.IsNotNull(enemy, "Enemies should spawn");
            float before = (float)enemyType.GetProperty("Health").GetValue(enemy);
            enemyType.GetMethod("Burn").Invoke(enemy, new object[] { 4f, 3f });
            enemyType.GetMethod("Chill").Invoke(enemy, new object[] { 0.5f, 3f });
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { KeepAlive(player); yield return null; }
            Assert.Less((float)enemyType.GetProperty("Health").GetValue(enemy), before, "A burn should deal damage over time");
            Assert.IsTrue((bool)enemyType.GetProperty("Chilled").GetValue(enemy), "Chill should hold for its duration");

            // Storm 4: claim four Storm items, shock an enemy and kill it: a lightning burst follows.
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type rewardType = Type.GetType("Hookfall.Combat.ChestReward, Assembly-CSharp");
            Type statType = Type.GetType("Hookfall.Player.StatType, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            foreach (Component prefab in (IList)combatType.GetField("itemPrefabs", flags).GetValue(combat))
                if (Array.IndexOf(new[] { "PF_Item_StormAura", "PF_Item_ChainLightning", "PF_Item_ThunderingBell", "PF_Item_LightningRod" }, prefab.name) >= 0)
                    combatType.GetMethod("Claim").Invoke(combat, new[] { Activator.CreateInstance(rewardType, prefab.GetType().GetProperty("Rarity").GetValue(prefab), prefab, true, 1, Enum.ToObject(statType, 0)), player.GetComponent(statsType), null });
            Assert.GreaterOrEqual(count("Storm"), 4, "Four Storm items should make Storm 4");
            int chainsBefore = (int)synergyType.GetProperty("ChainsFired").GetValue(synergy);
            Component victim = null;
            foreach (Component e in active) if (!(bool)enemyType.GetProperty("IsDead").GetValue(e)) { victim = e; break; }
            Assert.IsNotNull(victim, "There should be an enemy to shock");
            enemyType.GetMethod("Shock").Invoke(victim, new object[] { 3f });
            enemyType.GetMethod("TakeDamage").Invoke(victim, new object[] { 100000f, false });
            for (int i = 0; i < 5; i++) { KeepAlive(player); yield return null; }
            bool chainFired = (int)synergyType.GetProperty("ChainsFired").GetValue(synergy) > chainsBefore;
            Assert.IsTrue(chainFired, "A shocked enemy's death with Storm 4 should fire a lightning burst");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Power-ups: each kind does its job when the Warden touches it.</summary>
        [UnityTest]
        public IEnumerator PowerUpsWork()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            Type spawnerType = Type.GetType("Hookfall.World.PowerUpSpawner, Assembly-CSharp");
            Type kindType = Type.GetType("Hookfall.World.PowerUpKind, Assembly-CSharp");
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type synergyType = Type.GetType("Hookfall.Combat.SynergySystem, Assembly-CSharp");
            object spawner = spawnerType.GetProperty("Instance").GetValue(null);
            Assert.IsNotNull(spawner, "The arena needs a PowerUpSpawner");
            var active = (IList)enemyType.GetField("Active").GetValue(null);
            float end = Time.realtimeSinceStartup + 8f;
            while (active.Count < 3 && Time.realtimeSinceStartup < end) { KeepAlive(player); yield return null; }
            Assert.Greater(active.Count, 0, "Enemies should spawn");

            IEnumerator Touch(string kind)
            {
                Vector3 at = body.position;
                spawnerType.GetMethod("SpawnAt").Invoke(spawner, new[] { (object)at, Enum.Parse(kindType, kind) });
                yield return null;
                yield return null;
            }

            yield return Touch("Timefrost");
            Assert.IsTrue((bool)enemyType.GetProperty("Frozen").GetValue(active[0]), "Timefrost should freeze the enemies");

            Component stats = player.GetComponent(statsType);
            statsType.GetProperty("CurrentHealth").SetValue(stats, 10f);
            yield return Touch("HealingDew");
            Assert.Greater((float)statsType.GetProperty("CurrentHealth").GetValue(stats), 40f, "Healing Dew should heal");

            yield return Touch("BerserkRage");
            Assert.IsTrue((bool)synergyType.GetProperty("Frenzied").GetValue(player.GetComponent(synergyType)), "Berserk Rage should start the frenzy");

            // A near pass is enough: one 10 m away flies in and is used.
            object far = spawnerType.GetMethod("SpawnAt").Invoke(spawner, new[] { (object)(body.position + Vector3.right * 10f), Enum.Parse(kindType, "TreasureGale") });
            float homeEnd = Time.realtimeSinceStartup + 3f;
            while ((UnityEngine.Object)far != null && Time.realtimeSinceStartup < homeEnd) { KeepAlive(player); yield return null; }
            Assert.IsTrue((UnityEngine.Object)far == null, "A power-up 10 m away should fly to the Warden and be used");

            // Enemy damage grows with the Warden's level.
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Type xpType = Type.GetType("Hookfall.Progression.XPSystem, Assembly-CSharp");
            object director = UnityEngine.Object.FindAnyObjectByType(directorType);
            var scaleAt = directorType.GetMethod("DamageScaleAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float atLevel1 = (float)scaleAt.Invoke(director, new object[] { 0f });
            xpType.GetProperty("Level").SetValue(player.GetComponent(xpType), 21);
            Assert.AreEqual(atLevel1 + 2f, (float)scaleAt.Invoke(director, new object[] { 0f }), 0.01f, "Level 21 should add +200% enemy damage");

            yield return Touch("HeartstoneNova");
            Assert.AreEqual(0, active.Count, "Heartstone Nova should destroy every enemy");
            KeepAlive(player);
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Duos: Flame 2 and Frost 2 unlock Thermal Shock.</summary>
        [UnityTest]
        public IEnumerator DuoUnlocksAcrossSchools()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type rewardType = Type.GetType("Hookfall.Combat.ChestReward, Assembly-CSharp");
            Type statType = Type.GetType("Hookfall.Player.StatType, Assembly-CSharp");
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Type synergyType = Type.GetType("Hookfall.Combat.SynergySystem, Assembly-CSharp");
            Type duoType = Type.GetType("Hookfall.Combat.Duo, Assembly-CSharp");
            Component combat = player.GetComponent(combatType);
            Component synergy = player.GetComponent(synergyType);
            Func<bool> thermal = () => (bool)synergyType.GetMethod("HasDuo").Invoke(synergy, new[] { Enum.Parse(duoType, "ThermalShock") });
            Assert.IsFalse(thermal(), "No duo at the start");
            foreach (Component prefab in (IList)combatType.GetField("itemPrefabs", flags).GetValue(combat))
                if (prefab.name == "PF_Item_SteamGauntlet" || prefab.name == "PF_Item_ArcLaser")
                    combatType.GetMethod("Claim").Invoke(combat, new[] { Activator.CreateInstance(rewardType, prefab.GetType().GetProperty("Rarity").GetValue(prefab), prefab, true, 1, Enum.ToObject(statType, 0)), player.GetComponent(statsType), null });
            Assert.IsTrue(thermal(), "Moonblade Halo + Steamforged Gauntlet (Frost 2) and the gauntlet + Sunlance Prism (Flame 2) should unlock Thermal Shock");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Stages: the boss bar fills, the boss rises in a cutscene, plain bats stay below its zone while wyverns
        /// climb, Heartstone Nova only wounds it, its death opens the portal and offers a Legendary pick, and the portal
        /// starts stage 2 on a new map at level 1 with the same items.</summary>
        [UnityTest]
        public IEnumerator BossPortalAndNextStage()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Type cutsceneType = Type.GetType("Hookfall.Feel.CutsceneDirector, Assembly-CSharp");
            Type xpType = Type.GetType("Hookfall.Progression.XPSystem, Assembly-CSharp");
            Type walletType = Type.GetType("Hookfall.Progression.GoldWallet, Assembly-CSharp");
            Type combatType = Type.GetType("Hookfall.Combat.CombatSystem, Assembly-CSharp");
            Type generatorType = Type.GetType("Hookfall.World.IslandGenerator, Assembly-CSharp");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Type chestUiType = Type.GetType("Hookfall.UI.ChestResultUI, Assembly-CSharp");
            Type powerType = Type.GetType("Hookfall.World.PowerUpSpawner, Assembly-CSharp");
            Type kindType = Type.GetType("Hookfall.World.PowerUpKind, Assembly-CSharp");
            object stage = stageType.GetProperty("Instance").GetValue(null);
            Assert.IsNotNull(stage, "The arena needs a StageDirector");
            Assert.AreEqual(1, (int)stageType.GetProperty("Stage").GetValue(stage));
            object gm = gmType.GetProperty("Instance").GetValue(null);
            Func<string> state = () => gmType.GetProperty("State").GetValue(gm).ToString();
            var cutscenes = UnityEngine.Object.FindAnyObjectByType(cutsceneType);
            var skip = cutsceneType.GetField("skip", flags);
            Component levelUp = player.GetComponent(levelUpType);
            Component xp = player.GetComponent(xpType);
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);
            void Alive()
            {
                statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
                statsType.GetMethod("SetAbyssKillHeight").Invoke(stats, new object[] { -100000f });
            }
            float chestShownAt = -1f;
            bool rewardPicked = false;
            void Screens()
            {
                if (state() == "LevelUp") levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                if (state() == "ChestResult")
                {
                    if (chestShownAt < 0f) chestShownAt = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - chestShownAt > 0.4f)
                    {
                        object ui = chestUiType.GetProperty("Instance").GetValue(null);
                        chestUiType.GetMethod("Pick", flags).Invoke(ui, new object[] { 0 });
                        rewardPicked = true;
                        chestShownAt = -1f;
                    }
                }
                if (state() == "Cutscene") skip.SetValue(cutscenes, true);
            }

            // A few levels first, so the reset has something to undo.
            xpType.GetMethod("AddXP").Invoke(xp, new object[] { 120f });
            for (int i = 0; i < 30; i++) { Screens(); yield return null; }
            Assert.Greater((int)xpType.GetProperty("Level").GetValue(xp), 2, "120 XP should give a few levels");

            // The gold bar fills: the boss rises in a cutscene.
            int target = (int)stageType.GetProperty("GoldTarget").GetValue(stage);
            walletType.GetMethod("Add").Invoke(player.GetComponent(walletType), new object[] { target });
            bool sawCutscene = false;
            object boss = null;
            float end = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < end)
            {
                Alive();
                sawCutscene |= state() == "Cutscene";
                Screens();
                boss = stageType.GetProperty("Boss").GetValue(stage);
                if (boss != null && (bool)boss.GetType().GetProperty("Fighting").GetValue(boss) && state() == "Playing") break;
                yield return null;
            }
            Assert.IsTrue(sawCutscene, "The boss's entrance should play as a cutscene");
            Assert.IsNotNull(boss, "The boss should rise when the gold bar is full");
            Assert.IsTrue((bool)boss.GetType().GetProperty("Fighting").GetValue(boss), "The boss should fight after its entrance");
            Assert.AreEqual("Playing", state(), "The game should run again after the cutscene");
            Assert.IsNotNull(GameObject.Find("Boss Arena"), "The boss should bring its own islands");

            // Only wyverns and elites climb into the boss zone.
            Vector3 centre = (Vector3)stageType.GetProperty("ArenaCentre").GetValue(stage);
            object director = UnityEngine.Object.FindAnyObjectByType(directorType);
            var escort = directorType.GetMethod("SpawnEscort");
            var bat = (Component)escort.Invoke(director, new object[] { false, centre + new Vector3(20f, 0f, 0f), false });
            var wyvern = (Component)escort.Invoke(director, new object[] { true, centre + new Vector3(-20f, 0f, 0f), false });
            yield return null;
            Assert.Less(bat.transform.position.y, centre.y - 25f, "A plain bat should stay below the boss arena");
            Assert.Greater(wyvern.transform.position.y, centre.y - 15f, "A wyvern may fly up to the boss");

            // Heartstone Nova only wounds the boss.
            float max = (float)enemyType.GetProperty("MaxHealth").GetValue(boss);
            powerType.GetMethod("Use").Invoke(powerType.GetProperty("Instance").GetValue(null), new[] { Enum.Parse(kindType, "HeartstoneNova"), (object)centre });
            float health = (float)enemyType.GetProperty("Health").GetValue(boss);
            Assert.IsFalse((bool)enemyType.GetProperty("IsDead").GetValue(boss), "Heartstone Nova must not kill the boss");
            Assert.Less(health, max * 0.95f, "Heartstone Nova should wound the boss");

            // It dies: the portal rises where it stood and its reward is offered.
            enemyType.GetMethod("TakeDamage").Invoke(boss, new object[] { max * 5f, false });
            Assert.IsTrue((bool)enemyType.GetProperty("IsDead").GetValue(boss), "The boss should die");
            object portal = null;
            end = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < end && !(portal != null && rewardPicked))
            {
                Alive();
                Screens();
                portal = stageType.GetProperty("Portal").GetValue(stage);
                yield return null;
            }
            Assert.IsNotNull(portal, "A portal should open where the boss fell");
            Assert.IsTrue(rewardPicked, "The boss should offer its reward");

            // Through the portal: stage 2.
            var items = new System.Collections.Generic.List<string>();
            foreach (Component item in (IList)combatType.GetProperty("Items").GetValue(player.GetComponent(combatType))) items.Add(item.name);
            object generator = UnityEngine.Object.FindAnyObjectByType(generatorType);
            int seed = (int)generatorType.GetProperty("UsedSeed").GetValue(generator);
            Vector3 portalCentre = (Vector3)portal.GetType().GetProperty("Centre").GetValue(portal);
            Teleport(player, portalCentre);
            end = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < end)
            {
                Alive();
                Screens();
                if ((int)stageType.GetProperty("Stage").GetValue(stage) == 2 && stageType.GetProperty("Current").GetValue(stage).ToString() == "Gathering"
                    && state() == "Playing") break;
                yield return null;
            }
            Assert.AreEqual(2, (int)stageType.GetProperty("Stage").GetValue(stage), "The portal should start stage 2");
            Assert.AreEqual("Playing", state(), "The game should run on stage 2");
            Assert.AreEqual(1, (int)xpType.GetProperty("Level").GetValue(xp), "The Warden starts stage 2 at level 1");
            var after = new System.Collections.Generic.List<string>();
            foreach (Component item in (IList)combatType.GetProperty("Items").GetValue(player.GetComponent(combatType))) after.Add(item.name);
            CollectionAssert.AreEqual(items, after, "Items stay through the portal");
            Assert.AreNotEqual(seed, (int)generatorType.GetProperty("UsedSeed").GetValue(generator), "Stage 2 should have a new map");
            Assert.IsNull(GameObject.Find("Boss Arena"), "The boss arena goes with the old stage");
            Assert.Less(Vector3.Distance(player.transform.position, new Vector3(0f, 1.6f, -7f)), 4f, "The Warden starts at the foot of the tower");
            Assert.Greater((float)stageType.GetProperty("HealthScale").GetValue(stage), 1f, "Enemies are tougher on stage 2");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>The doubled map: many more islands, reaching much further, with large islands that stay empty: no
        /// golem, no chest, and no flyer spawns on them or inside any island (chests and flyers from 2026-11-09).</summary>
        [UnityTest]
        public IEnumerator BigMapKeepsLargeIslandsEmpty()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            NoBoss();
            Type islandType = Type.GetType("Hookfall.World.Island, Assembly-CSharp");
            int count = 0, large = 0;
            float reach = 0f;
            foreach (Component island in (IList)islandType.GetField("All").GetValue(null))
            {
                count++;
                if (islandType.GetProperty("Kind").GetValue(island).ToString() == "Large") large++;
                Vector3 p = island.transform.position;
                reach = Mathf.Max(reach, new Vector2(p.x, p.z).magnitude);
            }
            Assert.Greater(count, 70, "The doubled map should have many more islands");
            Assert.Greater(large, 3, "The map should have large islands");
            Assert.Greater(reach, 220f, "Islands should reach twice as far");

            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            object director = UnityEngine.Object.FindAnyObjectByType(directorType);
            var spawnGolem = directorType.GetMethod("SpawnGolem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Type golemType = Type.GetType("Hookfall.Enemies.Golem, Assembly-CSharp");
            int golems = 0;
            for (int i = 0; i < 30; i++)
            {
                object golem = spawnGolem.Invoke(director, new object[] { 5f });
                if (golem == null) continue;
                golems++;
                object home = golemType.GetProperty("HomeIsland").GetValue(golem);
                Assert.AreNotEqual("Large", islandType.GetProperty("Kind").GetValue(home).ToString(), "Golems never spawn on large islands");
            }
            Assert.Greater(golems, 0, "Golems should still find islands");

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type chestSpawnerType = Type.GetType("Hookfall.World.ChestSpawner, Assembly-CSharp");
            object chests = UnityEngine.Object.FindAnyObjectByType(chestSpawnerType);
            for (int i = 0; i < 40; i++) chestSpawnerType.GetMethod("Spawn", flags).Invoke(chests, null);
            int placed = 0;
            foreach (Component chest in (IList)chestSpawnerType.GetProperty("Chests").GetValue(chests))
            {
                placed++;
                string kind = islandType.GetProperty("Kind").GetValue(chest.GetType().GetProperty("Island").GetValue(chest)).ToString();
                Assert.IsFalse(kind == "Large" || kind == "Tower" || kind == "Boss", $"No chest goes on a {kind} island");
            }
            Assert.Greater(placed, 20, "Chests should still find islands");

            // Flyers, with the Warden standing on a large island: at most a rare point after every retry failed.
            Component largeIsland = null;
            foreach (Component island in (IList)islandType.GetField("All").GetValue(null))
                if (islandType.GetProperty("Kind").GetValue(island).ToString() == "Large") { largeIsland = island; break; }
            GameObject player = GameObject.Find("Player");
            KeepAlive(player);
            Teleport(player, (Vector3)islandType.GetProperty("Top").GetValue(largeIsland) + Vector3.up * 1.5f);
            yield return new WaitForFixedUpdate();
            var inside = directorType.GetMethod("InsideIsland", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            int blocked = 0;
            for (int i = 0; i < 200; i++)
            {
                var point = (Vector3)directorType.GetMethod("PickSpawnPoint", flags).Invoke(director, null);
                if ((bool)inside.Invoke(null, new object[] { point })) blocked++;
            }
            Assert.LessOrEqual(blocked, 2, $"{blocked} of 200 spawn points were inside an island or on a large one");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Test codes typed in a run (2026-11-09): BOSSGEL calls the boss, BOSSOL kills it, and the run is marked.</summary>
        [UnityTest]
        public IEnumerator TestCodesCallAndKillTheBoss()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            KeepAlive(GameObject.Find("Player"));
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type codesType = Type.GetType("Hookfall.World.CheatCodes, Assembly-CSharp");
            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            Type cutsceneType = Type.GetType("Hookfall.Feel.CutsceneDirector, Assembly-CSharp");
            object codes = UnityEngine.Object.FindAnyObjectByType(codesType);
            Assert.IsNotNull(codes, "The arena should listen for test codes");
            void Enter(string text) { foreach (char c in text) codesType.GetMethod("OnText", flags).Invoke(codes, new object[] { c }); }
            object stage = stageType.GetProperty("Instance").GetValue(null);

            Enter("xxbossgel");
            object boss = null;
            float end = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < end)
            {
                var cutscenes = UnityEngine.Object.FindAnyObjectByType(cutsceneType);
                if (cutscenes != null) cutsceneType.GetField("skip", flags).SetValue(cutscenes, true);
                boss = stageType.GetProperty("Boss").GetValue(stage);
                if (boss != null && (bool)boss.GetType().GetProperty("Fighting").GetValue(boss) && Time.timeScale > 0f) break;
                yield return null;
            }
            Assert.IsNotNull(boss, "BOSSGEL should call the boss");
            Assert.IsTrue((bool)boss.GetType().GetProperty("Fighting").GetValue(boss), "The boss should be fighting after its entrance");

            Enter("BossOl");
            Assert.IsTrue((bool)boss.GetType().GetProperty("IsDead").GetValue(boss), "BOSSOL should kill the boss");
            Assert.IsTrue((bool)codesType.GetProperty("Used").GetValue(null), "A run with a code keeps off the record");
            yield return null;
            Time.timeScale = 1f;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>The boss stands higher, with three islands circling it that carry a Warden standing on them (2026-11-09).</summary>
        [UnityTest]
        public IEnumerator BossIslandsCircleAndCarryTheWarden()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            KeepAlive(player);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            Type cutsceneType = Type.GetType("Hookfall.Feel.CutsceneDirector, Assembly-CSharp");
            Type orbitType = Type.GetType("Hookfall.World.OrbitingIsland, Assembly-CSharp");
            Type islandType = Type.GetType("Hookfall.World.Island, Assembly-CSharp");
            object stage = stageType.GetProperty("Instance").GetValue(null);
            stageType.GetMethod("SummonBoss").Invoke(stage, null);
            object boss = null;
            float end = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < end)
            {
                KeepAlive(player);
                var cutscenes = UnityEngine.Object.FindAnyObjectByType(cutsceneType);
                if (cutscenes != null) cutsceneType.GetField("skip", flags).SetValue(cutscenes, true);
                boss = stageType.GetProperty("Boss").GetValue(stage);
                if (boss != null && (bool)boss.GetType().GetProperty("Fighting").GetValue(boss) && Time.timeScale > 0f) break;
                yield return null;
            }
            Assert.IsNotNull(boss, "The boss should rise");
            Assert.Greater(((Component)boss).transform.position.y, 100f, "The boss stands higher than before (78 m)");

            UnityEngine.Object[] orbits = UnityEngine.Object.FindObjectsByType(orbitType, FindObjectsSortMode.None);
            Assert.AreEqual(3, orbits.Length, "Three islands circle the boss");
            var ring = (Component)orbits[0];
            Vector3 start = ring.transform.position;
            Teleport(player, (Vector3)islandType.GetProperty("Top").GetValue(ring.GetComponent(islandType)) + Vector3.up * 1.2f);

            end = Time.realtimeSinceStartup + 2.5f;
            while (Time.realtimeSinceStartup < end) { KeepAlive(player); yield return null; }
            Assert.Greater(Vector3.Distance(start, ring.transform.position), 3f, "The island should move along its circle");
            Type controllerType = Type.GetType("Hookfall.Player.PlayerController, Assembly-CSharp");
            object current = controllerType.GetProperty("CurrentIsland").GetValue(player.GetComponent(controllerType));
            Assert.AreSame(ring.GetComponent(islandType), current, "The Warden should still stand on the moving island");
            Time.timeScale = 1f;
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>A Warden with only the Orbit Blades flying at 38 m/s across the big map still meets the horde: enemies
        /// keep up and come within reach (2026-11-09: they trailed 30 m behind, out of view, and the sky looked empty).</summary>
        [UnityTest]
        public IEnumerator HordeKeepsUpWithAFastWarden()
        {
            PlayerPrefs.SetString("hf_character", "Warden");
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);
            // The frame-rate fallback depends on the machine running the test, not on the game.
            directorType.GetField("minFps", flags).SetValue(UnityEngine.Object.FindAnyObjectByType(directorType), 0f);
            var active = (IList)Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp").GetField("Active").GetValue(null);

            // Above every island, so nothing blocks the Warden; legs of 200 m and more, like a player crossing the map.
            Vector3[] route = { new Vector3(0f, 55f, -60f), new Vector3(170f, 57f, 30f), new Vector3(-30f, 55f, 190f), new Vector3(-210f, 58f, -40f), new Vector3(50f, 56f, -220f) };
            Teleport(player, route[0]);
            int leg = 1, samples = 0;
            float flown = 0f, close = 0f;
            while (flown < 30f)
            {
                KeepAlive(player);
                if (Time.timeScale == 0f && levelUp != null) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                if (Vector3.Distance(body.position, route[leg]) < 8f) leg = (leg + 1) % route.Length;
                body.linearVelocity = (route[leg] - body.position).normalized * 38f;
                flown += Time.deltaTime;
                if (flown > 10f && Time.timeScale > 0f)
                {
                    foreach (Component enemy in active) if (Vector3.Distance(enemy.transform.position, body.position) < 8f) close++;
                    samples++;
                }
                yield return null;
            }
            close /= Mathf.Max(1, samples);
            int kills = (int)gmType.GetProperty("Kills").GetValue(gmType.GetProperty("Instance").GetValue(null));
            // Before the fix: about 4 within 8 m and 0-3 kills; after it about 7.6 and 24 (2026-11-09).
            Assert.Greater(close, 3f, $"Enemies should keep up with a Warden at 38 m/s (on average {close:0.0} within 8 m)");
            Assert.GreaterOrEqual(kills, 10, $"The Orbit Blades should reach the horde in fast flight ({kills} kills in 30 s)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Jumps the stage clock to 4:55 so wyverns, golems and the first elite wave all appear.</summary>
        [UnityTest]
        public IEnumerator LateRunSpawnsEveryEnemy()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            Type stageType = Type.GetType("Hookfall.World.StageDirector, Assembly-CSharp");
            stageType.GetProperty("StageTime").SetValue(stageType.GetProperty("Instance").GetValue(null), 295f);   // the waves follow the stage clock

            GameObject player = GameObject.Find("Player");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);

            bool sawWyvern = false, sawGolem = false, sawElite = false;
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            var active = (IList)enemyType.GetField("Active").GetValue(null);

            float end = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < end && !(sawWyvern && sawGolem && sawElite))
            {
                KeepAlive(player);
                if (player.transform.position.y < 6f)
                {
                    player.transform.position = new Vector3(0f, 12f, -10f);
                    player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                }
                if (Time.timeScale == 0f && levelUp != null)
                    levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });

                foreach (object e in active)
                {
                    string name = e.GetType().Name;
                    sawWyvern |= name == "Wyvern";
                    sawGolem |= name == "Golem";
                    sawElite |= (bool)enemyType.GetProperty("IsElite").GetValue(e);
                }
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(sawWyvern, "No wyvern spawned");
            Assert.IsTrue(sawGolem, "No golem spawned");
            Assert.IsTrue(sawElite, "No elite wave spawned");
        }

        /// <summary>The wider horde (2026-11-10): a Blightpuff bursts near the Warden, a Razorwing flock makes its run and
        /// wheels, a Venomwing spits, a Gloom Jelly shocks and leaves three young; and the elite traits: a barrier takes
        /// the first blows, a Splitting elite leaves two behind, an Explosive one bursts.</summary>
        [UnityTest]
        public IEnumerator WiderHordeAndEliteTraitsWork()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            GameObject player = GameObject.Find("Player");
            Type directorType = Type.GetType("Hookfall.Enemies.EnemyDirector, Assembly-CSharp");
            Type enemyType = Type.GetType("Hookfall.Enemies.EnemyBase, Assembly-CSharp");
            Type affixType = Type.GetType("Hookfall.Enemies.EliteAffix, Assembly-CSharp");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType);
            var director = (Component)UnityEngine.Object.FindAnyObjectByType(directorType);
            var active = (IList)enemyType.GetField("Active").GetValue(null);
            var spawn = directorType.GetMethod("SpawnFlyer", flags);
            Func<string, Vector3, Component> make = (field, at) =>
                (Component)spawn.Invoke(director, new[] { directorType.GetField(field, flags).GetValue(director), (object)at, 0f });
            Func<Component, string> state = e => (string)enemyType.GetProperty("CurrentStateName").GetValue(e);

            int bursts = 0, shocks = 0;
            directorType.GetEvent("Exploded").AddEventHandler(null, (Action<Vector3, float>)((p, r) => bursts++));
            directorType.GetEvent("Pulsed").AddEventHandler(null, (Action<Vector3, float>)((p, r) => shocks++));

            // The Warden hangs in open sky, well above every island.
            Vector3 hold = new Vector3(40f, 62f, 40f);
            Func<float, Func<bool>, IEnumerator> wait = null;
            wait = (seconds, done) => Hold(player, levelUp, levelUpType, hold, seconds, done);

            make("blightpuffPrefab", hold + new Vector3(7f, 0f, 0f));
            yield return wait(8f, () => bursts > 0);
            Assert.Greater(bursts, 0, "A Blightpuff beside the Warden should burst");

            int birds = (int)directorType.GetMethod("SpawnRazorFlock", flags).Invoke(director, new object[] { 0f, false });
            Assert.GreaterOrEqual(birds, 5, "A Razorwing flock should be at least five birds");
            bool strafed = false, wheeled = false;
            yield return wait(14f, () =>
            {
                foreach (Component e in active)
                {
                    if (e.GetType().Name != "Razorwing") continue;
                    strafed |= state(e) == "Strafe";
                    wheeled |= state(e) == "Wheel";
                }
                return strafed && wheeled;
            });
            Assert.IsTrue(strafed, "The Razorwings should make an attack run");
            Assert.IsTrue(wheeled, "A Razorwing should wheel round after its pass");

            Component wasp = make("venomwingPrefab", hold + new Vector3(16f, 4f, 0f));
            bool spat = false;
            yield return wait(9f, () => spat |= wasp != null && state(wasp) == "Spit");
            Assert.IsTrue(spat, "A Venomwing in range should wind up to spit");

            Component jelly = make("jellyPrefab", hold + new Vector3(4f, 0f, 0f));
            int before = shocks;
            yield return wait(6f, () => shocks > before);
            Assert.Greater(shocks, before, "A Gloom Jelly beside the Warden should shock");
            if (!(bool)enemyType.GetProperty("IsDead").GetValue(jelly))
                enemyType.GetMethod("TakeDamage").Invoke(jelly, new object[] { 1000000f, false });
            int young = 0;
            foreach (Component e in active)
                if (e.GetType().Name == "GloomJelly" && (bool)e.GetType().GetProperty("IsYoung").GetValue(e)) young++;
            Assert.AreEqual(3, young, "A killed Gloom Jelly should leave three young");

            // Elite traits, on bats set well away from the Warden's blades.
            object[] Elite(int affix) => new object[] { 3f, 1.5f, 1.4f, 3f, Color.magenta, Enum.ToObject(affixType, affix), Color.magenta };
            Component shielded = make("batPrefab", hold + new Vector3(0f, 30f, 0f));
            enemyType.GetMethod("MakeElite").Invoke(shielded, Elite(1));
            float barrier = (float)enemyType.GetProperty("Shield").GetValue(shielded);
            Assert.Greater(barrier, 0f, "A Shielded elite should have a barrier");
            enemyType.GetMethod("TakeDamage").Invoke(shielded, new object[] { barrier * 0.5f, true });
            Assert.AreEqual((float)enemyType.GetProperty("MaxHealth").GetValue(shielded), (float)enemyType.GetProperty("Health").GetValue(shielded), 0.01f,
                "The barrier should take the blow, not the elite's health");

            Vector3 far = hold + new Vector3(0f, 30f, 60f);
            Component splitter = make("batPrefab", far);
            enemyType.GetMethod("MakeElite").Invoke(splitter, Elite(4));
            enemyType.GetMethod("TakeDamage").Invoke(splitter, new object[] { 1000000f, false });
            int left = 0;
            foreach (Component e in active)
                if (e.GetType().Name == "Bat" && (e.transform.position - far).sqrMagnitude < 9f) left++;
            Assert.GreaterOrEqual(left, 2, "A Splitting elite should leave two of its kind behind");

            before = bursts;
            Component bomb = make("batPrefab", hold + new Vector3(0f, 30f, -60f));
            enemyType.GetMethod("MakeElite").Invoke(bomb, Elite(3));
            enemyType.GetMethod("TakeDamage").Invoke(bomb, new object[] { 1000000f, false });
            yield return wait(3f, () => bursts > before);
            Assert.Greater(bursts, before, "An Explosive elite should burst when it dies");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>The run systems of 2026-11-11: shrines appear and bless a Warden who stands by one; a level-up can be
        /// rerolled, a card banished for good, and the level skipped for gold.</summary>
        [UnityTest]
        public IEnumerator ShrinesAndChoiceTokensWork()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Type levelUpType = Type.GetType("Hookfall.Progression.LevelUpSystem, Assembly-CSharp");
            Type tokensType = Type.GetType("Hookfall.Progression.ChoiceTokens, Assembly-CSharp");
            Type shrineType = Type.GetType("Hookfall.World.ShrineSpawner, Assembly-CSharp");
            Type goldType = Type.GetType("Hookfall.Progression.GoldWallet, Assembly-CSharp");
            Type xpType = Type.GetType("Hookfall.Progression.XPSystem, Assembly-CSharp");
            Component levelUp = player.GetComponent(levelUpType), tokens = player.GetComponent(tokensType), gold = player.GetComponent(goldType);
            object spawner = shrineType.GetProperty("Instance").GetValue(null);
            var shrines = (System.Collections.Generic.IReadOnlyList<Transform>)shrineType.GetProperty("Shrines").GetValue(spawner);
            Func<IList> offer = () => (IList)levelUpType.GetProperty("Offer").GetValue(levelUp);
            Func<string, int> left = name => (int)tokensType.GetProperty(name).GetValue(tokens);

            float end = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < end && shrines.Count == 0) { KeepAlive(player); yield return null; }
            Assert.AreEqual(3, shrines.Count, "Three shrines should stand on the islands of a stage");

            // Stand by a shrine: its blessing comes and the shrine goes.
            Teleport(player, shrines[0].position + new Vector3(1.2f, 1.5f, 0f));
            end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end && shrines.Count == 3)
            {
                KeepAlive(player);
                if (Time.timeScale == 0f) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
            Assert.AreEqual(2, shrines.Count, "A shrine should give its blessing to a Warden who stands by it");

            // A level-up: reroll, banish, skip.
            while (offer().Count > 0) { levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 }); yield return null; }
            xpType.GetMethod("AddXP").Invoke(player.GetComponent(xpType), new object[] { 5f });
            end = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < end && offer().Count == 0)
            {
                KeepAlive(player);
                xpType.GetMethod("AddXP").Invoke(player.GetComponent(xpType), new object[] { 20f });
                yield return null;
            }
            Assert.AreEqual(3, offer().Count, "A level-up should offer three cards");
            int rerolls = left("Rerolls"), banishes = left("Banishes");
            Assert.IsTrue((bool)levelUpType.GetMethod("Reroll").Invoke(levelUp, null), "A reroll should be allowed");
            Assert.AreEqual(rerolls - 1, left("Rerolls"));
            string gone = offer()[0].ToString();
            object goneCard = offer()[0];
            Assert.IsTrue((bool)levelUpType.GetMethod("Banish").Invoke(levelUp, new object[] { 0 }), "A banish should be allowed");
            Assert.AreEqual(banishes - 1, left("Banishes"));
            for (int i = 0; i < 30; i++)
            {
                foreach (object card in offer()) Assert.AreNotEqual(goneCard, card, "A banished card should never be offered again: " + gone);
                if (!(bool)levelUpType.GetMethod("Reroll").Invoke(levelUp, null)) tokensType.GetMethod("Add").Invoke(tokens, new object[] { 5, 0 });
            }
            int before = (int)goldType.GetProperty("Gold").GetValue(gold);
            int pending = (int)levelUpType.GetProperty("PendingLevels").GetValue(levelUp);
            levelUpType.GetMethod("Skip").Invoke(levelUp, null);
            Assert.AreEqual(before + left("SkipGold"), (int)goldType.GetProperty("Gold").GetValue(gold), "Skipping a level should pay gold");
            Assert.AreEqual(pending - 1, (int)levelUpType.GetProperty("PendingLevels").GetValue(levelUp));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Keeps the Warden alive and in one place in the sky for a while, or until done says so.</summary>
        private static IEnumerator Hold(GameObject player, Component levelUp, Type levelUpType, Vector3 at, float seconds, Func<bool> done)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && !done())
            {
                KeepAlive(player);
                var body = player.GetComponent<Rigidbody>();
                body.position = at;
                player.transform.position = at;
                body.linearVelocity = Vector3.zero;
                if (Time.timeScale == 0f && levelUp != null) levelUpType.GetMethod("Pick").Invoke(levelUp, new object[] { 0 });
                yield return null;
            }
        }
    }
}
