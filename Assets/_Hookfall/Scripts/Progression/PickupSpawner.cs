// Hookfall - drops and pools XP gems (small, medium, large) and gold coins (GDD 4.7, 7.3).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace Hookfall.Progression
{
    public class PickupSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerStats player;
        [SerializeField] private Pickup smallGem;
        [SerializeField] private Pickup mediumGem;
        [SerializeField] private Pickup largeGem;
        [SerializeField] private Pickup coin;
        [Tooltip("XP value from which the medium and large gems are used (Bat 1, Wyvern 3, Golem 5).")]
        [SerializeField] private float mediumGemFrom = 3f;
        [SerializeField] private float largeGemFrom = 5f;
        [SerializeField] private float scatter = 0.8f;

        private readonly Dictionary<Pickup, ObjectPool<Pickup>> pools = new Dictionary<Pickup, ObjectPool<Pickup>>();
        private readonly Dictionary<Pickup, Pickup> prefabOf = new Dictionary<Pickup, Pickup>();
        private XPSystem xp;
        private GoldWallet gold;

        public static PickupSpawner Instance { get; private set; }
        public float PickupRadius => player != null ? player.PickupRadius : 4f;

        public event Action<PickupKind> Collected;

        private void Awake()
        {
            Instance = this;
            if (player == null) return;
            xp = player.GetComponent<XPSystem>();
            gold = player.GetComponent<GoldWallet>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void DropXP(Vector3 position, float amount)
        {
            if (amount <= 0f) return;
            Pickup prefab = amount >= largeGemFrom ? largeGem : amount >= mediumGemFrom ? mediumGem : smallGem;
            Spawn(prefab, position, amount);
        }

        public void DropGold(Vector3 position, int amount)
        {
            if (amount > 0) Spawn(coin, position, amount);
        }

        private void Spawn(Pickup prefab, Vector3 position, float amount)
        {
            if (prefab == null || player == null) return;
            Pickup pickup = PoolFor(prefab).Get();
            pickup.transform.position = position + UnityEngine.Random.insideUnitSphere * scatter;
            pickup.Init(this, player.transform, amount);
        }

        public void Collect(Pickup pickup, float value)
        {
            if (pickup.Kind == PickupKind.XP) xp?.AddXP(value);
            else gold?.Add(Mathf.RoundToInt(value));
            Collected?.Invoke(pickup.Kind);
            Release(pickup);
        }

        public void Release(Pickup pickup)
        {
            if (prefabOf.TryGetValue(pickup, out Pickup prefab)) pools[prefab].Release(pickup);
            else Destroy(pickup.gameObject);
        }

        private ObjectPool<Pickup> PoolFor(Pickup prefab)
        {
            if (pools.TryGetValue(prefab, out ObjectPool<Pickup> pool)) return pool;
            pool = new ObjectPool<Pickup>(
                () =>
                {
                    Pickup p = Instantiate(prefab, transform);
                    prefabOf[p] = prefab;
                    return p;
                },
                p => p.gameObject.SetActive(true),
                p => p.gameObject.SetActive(false),
                p => Destroy(p.gameObject),
                false, 64, 2000);
            pools[prefab] = pool;
            return pool;
        }
    }
}
