// Hookfall - a golem's boulder: flies straight at 25 m/s, hurts the player on contact, breaks on islands (GDD 5.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Boulder : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 1.3f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float spinSpeed = 360f;
        [Tooltip("Set for a shot that is not stone (a wasp's venom): no rock burst or crack when it ends.")]
        [SerializeField] private bool quiet;

        /// <summary>The prefab this shot was pooled from (set by the EnemyDirector).</summary>
        public Boulder SourcePrefab { get; set; }
        public bool Quiet => quiet;

        private EnemyDirector director;
        private PlayerStats target;
        private Vector3 velocity;
        private float damage;
        private float age;

        public void Launch(EnemyDirector owner, PlayerStats player, Vector3 position, Vector3 launchVelocity, float hitDamage)
        {
            director = owner;
            target = player;
            transform.position = position;
            velocity = launchVelocity;
            damage = hitDamage;
            age = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Vector3 from = transform.position;
            Vector3 to = from + velocity * dt;

            if (age > lifetime || Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerController>() == null)
            {
                director.ReleaseBoulder(this, true);
                return;
            }

            transform.position = to;
            transform.Rotate(Vector3.right, spinSpeed * dt, Space.Self);
            if (target != null && (target.transform.position - to).sqrMagnitude < hitRadius * hitRadius)
            {
                target.TakeDamageFrom(damage, to - velocity.normalized * 10f);   // where it flew in from
                director.ReleaseBoulder(this, true);
            }
        }
    }
}
