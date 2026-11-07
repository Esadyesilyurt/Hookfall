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
                e.FindPropertyRelative("volume").floatValue = 1f;
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
