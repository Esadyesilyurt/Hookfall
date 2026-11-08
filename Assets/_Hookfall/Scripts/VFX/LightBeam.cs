// Hookfall - a soft column of light (additive, fading upwards) that marks a place from across the map: the boss's
// beacon and the portal's beam. Built at run time with vertex colours, drawn with the M_LightBeam material.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.VFX
{
    public static class LightBeam
    {
        public static GameObject Build(Transform parent, Material material, float height, float radius, Color color)
        {
            var go = new GameObject("Light Beam") { layer = Layers.NoOutline };
            go.transform.SetParent(parent, false);
            const int segments = 20;
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[segments * 6];
            Color top = new Color(color.r, color.g, color.b, 0f);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = dir * radius;
                vertices[i * 2 + 1] = dir * (radius * 1.6f) + Vector3.up * height;
                colors[i * 2] = color;
                colors[i * 2 + 1] = top;
                int n = (i + 1) % segments;
                int t = i * 6;
                triangles[t] = i * 2; triangles[t + 1] = i * 2 + 1; triangles[t + 2] = n * 2;
                triangles[t + 3] = n * 2; triangles[t + 4] = i * 2 + 1; triangles[t + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "Beam", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }
    }
}
