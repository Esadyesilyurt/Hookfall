// Hookfall - plays the licensed sound effects and music (GDD 8.4): hook sounds, wind that rises with speed, hits,
// deaths, pickups, chests, the tower cache, a low-health heartbeat, and layered music that fills up as the run gets
// harder and is fullest during elite waves. The same sound plays at most 8 times at once. No AI-generated audio.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.World;
using UnityEngine;

namespace Hookfall.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public struct SoundClip
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Tooltip("The same sound waits at least this long before playing again (0 = no limit).")]
            public float minInterval;
            [Tooltip("At most this many copies at once (0 = the manager's default).")]
            public int maxVoices;
        }

        [SerializeField] private List<SoundClip> clips = new List<SoundClip>();
        [Tooltip("The same sound plays at most this many times at once (GDD 8.4).")]
        [SerializeField] private int maxVoicesPerSound = 8;

        [Header("Loops")]
        [SerializeField] private AudioClip windLoop;
        [SerializeField] private float windFullSpeed = 55f;
        [SerializeField] private AudioClip heartbeatLoop;
        [SerializeField, Range(0f, 1f)] private float heartbeatBelowHealth = 0.3f;

        [Header("Music")]
        [Tooltip("Music layers, all the same length; layer n joins after n × minutesPerLayer minutes.")]
        [SerializeField] private List<AudioClip> musicLayers = new List<AudioClip>();
        [SerializeField] private float minutesPerLayer = 2f;
        [SerializeField] private float eliteFullMusicSeconds = 40f;
        [SerializeField] private float musicFadeSpeed = 0.5f;

        [Header("Sources of events")]
        [SerializeField] private PlayerController player;
        [SerializeField] private GrappleHook hook;
        [SerializeField] private PickupSpawner pickups;
        [SerializeField] private ChestSpawner chests;
        [SerializeField] private TowerCache towerCache;
        [SerializeField] private LevelUpSystem levelUp;

        private readonly Dictionary<SoundId, SoundClip> byId = new Dictionary<SoundId, SoundClip>();
        private readonly Dictionary<SoundId, List<AudioSource>> voices = new Dictionary<SoundId, List<AudioSource>>();
        private readonly List<AudioSource> music = new List<AudioSource>();
        private AudioSource wind, heartbeat;
        private PlayerStats stats;
        private float eliteMusicTimer;
        private readonly Dictionary<SoundId, float> lastPlayed = new Dictionary<SoundId, float>();
        private Player.WardenAnimator warden;

        private void Awake()
        {
            foreach (SoundClip c in clips) if (c.clip != null) byId[c.id] = c;
            wind = Loop(windLoop);
            heartbeat = Loop(heartbeatLoop);
            foreach (AudioClip layer in musicLayers) if (layer != null) music.Add(Loop(layer));
            if (player != null)
            {
                stats = player.GetComponent<PlayerStats>();
                warden = player.GetComponentInChildren<Player.WardenAnimator>();
            }
        }

        private void Start()
        {
            // Start every music layer together so they stay in sync; volume decides what is heard.
            foreach (AudioSource m in music) m.Play();
            if (GameManager.Instance != null) GameManager.Instance.RunEnded += OnRunEnded;
        }

        private void OnEnable()
        {
            if (hook != null)
            {
                hook.Thrown += OnHookThrown;
                hook.Attached += OnHookAttached;
                hook.Released += OnHookReleased;
            }
            EnemyDirector.Hit += OnHit;
            EnemyDirector.Died += OnDied;
            EnemyDirector.Slammed += OnSlam;
            EnemyDirector.BoulderBroke += OnBoulder;
            EnemyDirector.EliteWave += OnEliteWave;
            if (pickups != null) pickups.Collected += OnCollected;
            if (chests != null) chests.ChestOpened += OnChest;
            if (towerCache != null) { towerCache.Opened += OnCache; towerCache.Appeared += OnCacheAppeared; }
            if (levelUp != null) levelUp.OfferReady += OnLevelUp;
            if (player != null && player.TryGetComponent(out PlayerStats s)) s.Damaged += OnDamaged;
            if (player != null) player.WallJumped += OnWallJump;
            if (warden != null) warden.TrickStarted += OnTrick;
        }

        private void OnDisable()
        {
            if (hook != null)
            {
                hook.Thrown -= OnHookThrown;
                hook.Attached -= OnHookAttached;
                hook.Released -= OnHookReleased;
            }
            EnemyDirector.Hit -= OnHit;
            EnemyDirector.Died -= OnDied;
            EnemyDirector.Slammed -= OnSlam;
            EnemyDirector.BoulderBroke -= OnBoulder;
            EnemyDirector.EliteWave -= OnEliteWave;
            if (pickups != null) pickups.Collected -= OnCollected;
            if (chests != null) chests.ChestOpened -= OnChest;
            if (towerCache != null) { towerCache.Opened -= OnCache; towerCache.Appeared -= OnCacheAppeared; }
            if (levelUp != null) levelUp.OfferReady -= OnLevelUp;
            if (player != null && player.TryGetComponent(out PlayerStats s)) s.Damaged -= OnDamaged;
            if (player != null) player.WallJumped -= OnWallJump;
            if (warden != null) warden.TrickStarted -= OnTrick;
            if (GameManager.Instance != null) GameManager.Instance.RunEnded -= OnRunEnded;
        }

        private void Update()
        {
            float effects = GameSettings.EffectsVolume;
            bool playing = Time.timeScale > 0f;
            if (wind != null) wind.volume = playing && player != null ? Mathf.Clamp01(player.Speed / windFullSpeed) * effects * 0.4f : 0f;
            if (heartbeat != null)
                heartbeat.volume = stats != null && !stats.IsDead && stats.CurrentHealth / stats.MaxHealth < heartbeatBelowHealth ? effects : 0f;

            float minutes = GameManager.Instance != null ? GameManager.Instance.RunTime / 60f : 0f;
            eliteMusicTimer -= Time.unscaledDeltaTime;
            int layers = eliteMusicTimer > 0f ? music.Count : 1 + Mathf.FloorToInt(minutes / Mathf.Max(0.1f, minutesPerLayer));
            float target = GameSettings.MusicVolume;
            for (int i = 0; i < music.Count; i++)
                music[i].volume = Mathf.MoveTowards(music[i].volume, i < layers ? target : 0f, musicFadeSpeed * Time.unscaledDeltaTime);
        }

        public void Play(SoundId id, Vector3? position = null)
        {
            if (!byId.TryGetValue(id, out SoundClip sound)) return;
            // Hits and pickups come in hundreds a second: space them out so they never become noise.
            float now = Time.unscaledTime;
            if (sound.minInterval > 0f && lastPlayed.TryGetValue(id, out float last) && now - last < sound.minInterval) return;
            AudioSource source = FreeVoice(id, sound.maxVoices > 0 ? sound.maxVoices : maxVoicesPerSound);
            if (source == null) return;
            source.transform.position = position ?? (player != null ? player.transform.position : transform.position);
            source.spatialBlend = position.HasValue ? 0.7f : 0f;
            source.clip = sound.clip;
            source.volume = (sound.volume > 0f ? sound.volume : 1f) * GameSettings.EffectsVolume;
            source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            source.Play();
            lastPlayed[id] = now;
        }

        private AudioSource FreeVoice(SoundId id, int maxVoices)
        {
            if (!voices.TryGetValue(id, out List<AudioSource> list)) voices[id] = list = new List<AudioSource>();
            foreach (AudioSource s in list) if (!s.isPlaying) return s;
            if (list.Count >= maxVoices) return null;
            var go = new GameObject($"SFX {id}");
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.minDistance = 8f;
            list.Add(source);
            return source;
        }

        private AudioSource Loop(AudioClip clip)
        {
            if (clip == null) return null;
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            source.Play();
            return source;
        }

        private void OnHookThrown() => Play(SoundId.HookThrow);
        private void OnHookAttached() => Play(SoundId.HookAttach, hook.AnchorPoint);
        private void OnHookReleased() => Play(SoundId.HookRelease);
        private void OnHit(EnemyBase enemy, float damage) => Play(SoundId.WeaponHit, enemy.transform.position);
        private void OnDied(EnemyBase enemy, bool killed) { if (killed) Play(SoundId.EnemyDeath, enemy.transform.position); }
        private void OnSlam(Golem golem) => Play(SoundId.GolemSlam, golem.transform.position);
        private void OnBoulder(Vector3 position) => Play(SoundId.BoulderBreak, position);
        private void OnEliteWave() { eliteMusicTimer = eliteFullMusicSeconds; Play(SoundId.EliteWave); }
        private void OnCollected(PickupKind kind) => Play(kind == PickupKind.XP ? SoundId.XPPickup : SoundId.GoldPickup);
        private void OnChest(Vector3 position, Rarity rarity) => Play(SoundId.ChestOpen);
        private void OnCache(Vector3 position, Rarity rarity) => Play(SoundId.TowerCacheOpen);
        private void OnCacheAppeared() => Play(SoundId.TowerCacheBeam);
        private void OnLevelUp() => Play(SoundId.LevelUp);
        private void OnDamaged(float amount) => Play(SoundId.PlayerHurt);
        private void OnRunEnded() => Play(SoundId.RunEnd);
        private void OnWallJump() => Play(SoundId.WallJump);
        private void OnTrick() => Play(SoundId.Trick);
    }
}
