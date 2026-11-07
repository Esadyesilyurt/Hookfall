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

        [SetUp] public void SaveRecord() => record.Save();
        [TearDown] public void RestoreRecord() => record.Restore();

        /// <summary>A test Warden hovers in place, which the horde punishes fast; keep it alive so the run goes on.</summary>
        private static void KeepAlive(GameObject player)
        {
            Type statsType = Type.GetType("Hookfall.Player.PlayerStats, Assembly-CSharp");
            Component stats = player.GetComponent(statsType);
            statsType.GetProperty("CurrentHealth").SetValue(stats, statsType.GetProperty("MaxHealth").GetValue(stats));
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
            for (int i = 0; i < 40; i++) grant.Invoke(combat, new[] { Enum.ToObject(rarityType, 2), (object)stats });
            var items = (ICollection)combatType.GetProperty("Items").GetValue(combat);
            Assert.AreEqual(6, items.Count, "Chests should fill all six item slots");

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

        /// <summary>Jumps the run clock to 4:55 so wyverns, golems and the first elite wave all appear.</summary>
        [UnityTest]
        public IEnumerator LateRunSpawnsEveryEnemy()
        {
            SceneManager.LoadScene("Arena");
            yield return null;

            Type gmType = Type.GetType("Hookfall.Core.GameManager, Assembly-CSharp");
            object gm = gmType.GetProperty("Instance").GetValue(null);
            gmType.GetProperty("RunTime").SetValue(gm, 295f);

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
    }
}
