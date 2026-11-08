// Hookfall - shared helpers for the editor builders: folders, materials, models, serialized-field wiring.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEditor;
using UnityEngine;

namespace Hookfall.EditorTools
{
    public static class BuildUtil
    {
        public const string Root = "Assets/_Hookfall";
        public const string MaterialsPath = Root + "/Art/Materials";
        public const string ModelsPath = Root + "/Art/Models";

        public static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/{name}.mat");

        /// <summary>Loads a model from Art/Models (FBX made by Tools/Blender), or null when it has not been made yet.</summary>
        public static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsPath}/{name}.fbx");

        public static Material GetOrCreateMaterial(string name, Color color, bool unlit)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            // Lit materials use the toon Shader Graph (GDD 8.1); URP/Lit only if the graph is missing.
            Shader shader = unlit ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find(ToonSetup.ToonShader) ?? Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (value != null && property.objectReferenceValue == null)
                Debug.LogError($"[Hookfall] Could not assign {value.name} to '{field}' on {target.GetType().Name}.");
        }

        public static void SetString(Object target, string field, string value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AddToList(Object target, string field, Object value)
        {
            SerializedProperty property = Find(target, field, out SerializedObject serialized);
            if (property == null) return;
            property.arraySize++;
            property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(Object target, string field, out SerializedObject serialized)
        {
            serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) Debug.LogError($"[Hookfall] Field '{field}' not found on {target.GetType().Name}.");
            return property;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        /// <summary>A primitive with no collider, as a child of parent.</summary>
        public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material,
            Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>An empty pivot child; parts placed under it rotate around the pivot (wings, arms).</summary>
        public static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        /// <summary>Saves a temporary root as a prefab (replacing any old version) and returns the saved asset.</summary>
        public static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Instantiates a model under parent, or returns null so the caller can build a placeholder.</summary>
        /// <summary>Writes a generated file only when its bytes changed: rewriting identical textures on every build
        /// once failed on a file Windows still had mapped (IOException 1224, 2026-11-08).</summary>
        public static void WriteIfChanged(string path, byte[] bytes)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] old = System.IO.File.ReadAllBytes(path);
                if (old.Length == bytes.Length && System.Linq.Enumerable.SequenceEqual(old, bytes)) return;
            }
            System.IO.File.WriteAllBytes(path, bytes);
        }

        public static GameObject InstantiateModel(string modelName, Transform parent)
        {
            GameObject model = Model(modelName);
            if (model == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Visual";
            instance.transform.SetParent(parent, false);
            return instance;
        }
    }
}
