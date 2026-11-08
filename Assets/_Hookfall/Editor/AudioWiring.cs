// Hookfall - connects licensed audio files to the AudioManager by name: Audio/SFX/SFX_<SoundId>.*,
// SFX_WindLoop, SFX_HeartbeatLoop, and Audio/Music/MUS_Layer1..n. Missing files are simply skipped.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using System.IO;
using Hookfall.Audio;
using UnityEditor;
using UnityEngine;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class AudioWiring
    {
        public static void AssignClips(AudioManager audio)
        {
            Dictionary<string, AudioClip> sfx = Load(Root + "/Audio/SFX");
            Dictionary<string, AudioClip> music = Load(Root + "/Audio/Music");

            var serialized = new SerializedObject(audio);
            SerializedProperty list = serialized.FindProperty("clips");
            list.arraySize = 0;
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (!sfx.TryGetValue("SFX_" + id, out AudioClip clip)) continue;
                list.arraySize++;
                SerializedProperty e = list.GetArrayElementAtIndex(list.arraySize - 1);
                e.FindPropertyRelative("id").enumValueIndex = (int)id;
                e.FindPropertyRelative("clip").objectReferenceValue = clip;
                (float volume, float interval, int voices) mix = Mix(id);
                e.FindPropertyRelative("volume").floatValue = mix.volume;
                e.FindPropertyRelative("minInterval").floatValue = mix.interval;
                e.FindPropertyRelative("maxVoices").intValue = mix.voices;
            }
            if (sfx.TryGetValue("SFX_WindLoop", out AudioClip wind)) serialized.FindProperty("windLoop").objectReferenceValue = wind;
            if (sfx.TryGetValue("SFX_HeartbeatLoop", out AudioClip beat)) serialized.FindProperty("heartbeatLoop").objectReferenceValue = beat;

            SerializedProperty layers = serialized.FindProperty("musicLayers");
            layers.arraySize = 0;
            var names = new List<string>(music.Keys);
            names.Sort(StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (!name.StartsWith("MUS_Layer")) continue;
                layers.arraySize++;
                layers.GetArrayElementAtIndex(layers.arraySize - 1).objectReferenceValue = music[name];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[Hookfall] Audio: {list.arraySize} sound effects and {layers.arraySize} music layers connected.");
        }

        /// <summary>The mix: frequent sounds are quiet and spaced out, rare ones a little louder (2026-11-08, after the
        /// first playtest found the sound tiring).</summary>
        private static (float volume, float interval, int voices) Mix(SoundId id)
        {
            switch (id)
            {
                case SoundId.WeaponHit: return (0.22f, 0.09f, 2);
                case SoundId.EnemyDeath: return (0.28f, 0.07f, 3);
                case SoundId.XPPickup: return (0.2f, 0.06f, 2);
                case SoundId.GoldPickup: return (0.3f, 0.08f, 2);
                case SoundId.PlayerHurt: return (0.45f, 0.25f, 1);
                case SoundId.BoulderBreak: return (0.35f, 0.15f, 2);
                case SoundId.GolemSlam: return (0.5f, 0.2f, 2);
                case SoundId.HookThrow: return (0.35f, 0.05f, 2);
                case SoundId.HookAttach: return (0.4f, 0.05f, 2);
                case SoundId.HookRelease: return (0.3f, 0.05f, 2);
                case SoundId.WallJump: return (0.4f, 0.1f, 1);
                case SoundId.Trick: return (0.3f, 0.4f, 1);
                // The boss and the portal (2026-11-08): rare and big, but the roar is loud by nature.
                case SoundId.BossRoar: return (0.7f, 0.5f, 1);
                case SoundId.BossVolley: return (0.45f, 0.3f, 2);
                case SoundId.BossSlam: return (0.7f, 0.3f, 1);
                case SoundId.BossDeath: return (0.85f, 1f, 1);
                case SoundId.PortalOpen: return (0.6f, 1f, 1);
                case SoundId.PortalEnter: return (0.7f, 1f, 1);
                case SoundId.StageStart: return (0.65f, 1f, 1);
                default: return (0.55f, 0.1f, 2);   // rare events: chests, level-up, tower cache, elite wave, run end
            }
        }

        private static Dictionary<string, AudioClip> Load(string folder)
        {
            var clips = new Dictionary<string, AudioClip>();
            if (!AssetDatabase.IsValidFolder(folder)) return clips;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                clips[Path.GetFileNameWithoutExtension(path)] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return clips;
        }
    }
}
