// Hookfall - shared enemy logic: health, rewards, pursuit steering and the fallbacks in GDD 5.7.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.Progression;
using UnityEngine;

namespace Hookfall.Enemies
{
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Base stats (scaled by the EnemyDirector over time)")]
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float contactDamage = 5f;
        [SerializeField] private float xpReward = 1f;
        [SerializeField] private int goldReward = 1;

        [Header("Fallbacks (GDD 5.7)")]
        [SerializeField] private float despawnDistance = 120f;
        [SerializeField] private float abyssHeight = -70f;
        [SerializeField] private float stuckCheckInterval = 3f;
        [SerializeField] private float stuckMinDistance = 1f;

        private readonly StateMachine machine = new StateMachine();
        private float damageMultiplier = 1f;
        private float stuckTimer;
        private Vector3 lastCheckedPosition;

        public float Health { get; private set; }
        public bool IsDead { get; private set; }
        public float MoveSpeed => moveSpeed;
        public float Damage => contactDamage * damageMultiplier;
        public string CurrentStateName => machine.Current?.Name ?? "None";

        public EnemyDirector Director { get; private set; }
        public PlayerController Player { get; private set; }
        public PlayerStats PlayerStats { get; private set; }
        private XPSystem playerXP;
        private GoldWallet playerGold;

        /// <summary>Called by the EnemyDirector right after spawning.</summary>
        public void Init(EnemyDirector director, PlayerController player, float healthMultiplier, float damageMult)
        {
            Director = director;
            Player = player;
            PlayerStats = player.GetComponent<PlayerStats>();
            playerXP = player.GetComponent<XPSystem>();
            playerGold = player.GetComponent<GoldWallet>();

            Health = maxHealth * healthMultiplier;
            damageMultiplier = damageMult;
            lastCheckedPosition = transform.position;

            BuildStates();
            machine.ChangeState(DefaultState);
        }

        protected abstract void BuildStates();
        protected abstract IEnemyState DefaultState { get; }

        public void ChangeState(IEnemyState next) => machine.ChangeState(next);

        protected virtual void Update()
        {
            if (IsDead || Player == null) return;

            // Fallback: an invalid state resets to the default one.
            if (machine.Current == null) machine.ChangeState(DefaultState);
            machine.Tick(Time.deltaTime);
            RunFallbacks();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            Health -= amount;
            if (Health <= 0f) Die(true);
        }

        private void Die(bool giveReward)
        {
            IsDead = true;
            if (giveReward)
            {
                if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
                // TODO (Week 10): drop XP gems and coins to pick up instead of granting them directly.
                if (playerXP != null) playerXP.AddXP(xpReward);
                if (playerGold != null) playerGold.Add(goldReward);
            }
            if (Director != null) Director.OnEnemyRemoved(this);
            Destroy(gameObject);
        }

        // ---------- Steering ----------

        /// <summary>Pursuit: aim at where the player will be, not where they are.</summary>
        public Vector3 PredictedPlayerPosition(float lookAheadSeconds)
        {
            return Player.transform.position + Player.Body.linearVelocity * lookAheadSeconds;
        }

        public void SteerTowards(Vector3 target, float speed, float deltaTime)
        {
            Vector3 toTarget = target - transform.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;
            Vector3 direction = toTarget.normalized;
            // TODO (Week 11): add separation from neighbours and short raycasts to steer around islands.
            transform.position += direction * (speed * deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
        }

        public float DistanceToPlayer => Vector3.Distance(transform.position, Player.transform.position);

        // ---------- Fallbacks (GDD 5.7) ----------

        private void RunFallbacks()
        {
            if (transform.position.y < abyssHeight)
            {
                Die(false);   // fell into the abyss: no reward
                return;
            }

            if (DistanceToPlayer > despawnDistance)
            {
                Director?.Relocate(this);   // too far: respawn closer, out of view
                lastCheckedPosition = transform.position;
                stuckTimer = 0f;
                return;
            }

            stuckTimer += Time.deltaTime;
            if (stuckTimer < stuckCheckInterval) return;
            if (Vector3.Distance(transform.position, lastCheckedPosition) < stuckMinDistance)
                transform.position += Vector3.up * 3f;   // stuck: short upward escape
            lastCheckedPosition = transform.position;
            stuckTimer = 0f;
        }
    }
}
