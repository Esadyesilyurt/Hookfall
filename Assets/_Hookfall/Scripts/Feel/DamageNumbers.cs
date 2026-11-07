// Hookfall - pooled damage numbers that pop up from hit enemies, rise and fade; bigger and warmer for big hits.
// Limited per frame so a 300-enemy horde stays readable.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Enemies;
using TMPro;
using UnityEngine;

namespace Hookfall.Feel
{
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private Material fontMaterial;
        [SerializeField] private int maxPerFrame = 8;
        [SerializeField] private int poolSize = 80;
        [SerializeField] private float lifetime = 0.65f;
        [SerializeField] private float rise = 2.2f;
        [SerializeField] private float bigHit = 40f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color bigColor = new Color(1f, 0.78f, 0.25f);

        private struct Number
        {
            public TextMeshPro text;
            public Vector3 origin;
            public float age;
            public float scale;
        }

        private readonly List<Number> numbers = new List<Number>();
        private int next;
        private int shownThisFrame;
        private Transform cam;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Damage Number") { layer = Core.Layers.NoOutline };
                go.transform.SetParent(transform, false);
                var tmp = go.AddComponent<TextMeshPro>();
                tmp.fontSize = 6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;
                if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                go.SetActive(false);
                numbers.Add(new Number { text = tmp, age = lifetime });
            }
        }

        private void OnEnable() => EnemyDirector.Hit += OnHit;
        private void OnDisable() => EnemyDirector.Hit -= OnHit;

        private void OnHit(EnemyBase enemy, float damage)
        {
            if (++shownThisFrame > (EnemyDirector.LowPerformance ? 2 : maxPerFrame)) return;
            Number n = numbers[next];
            next = (next + 1) % numbers.Count;
            bool big = damage >= bigHit;
            n.text.text = Mathf.RoundToInt(damage).ToString();
            n.text.color = big ? bigColor : normalColor;
            n.scale = big ? 1.5f : 1f;
            n.origin = enemy.transform.position + Vector3.up * (enemy.HitRadius + 0.3f) + Random.insideUnitSphere * 0.4f;
            n.age = 0f;
            n.text.gameObject.SetActive(true);
            numbers[(next + numbers.Count - 1) % numbers.Count] = n;
        }

        private void LateUpdate()
        {
            shownThisFrame = 0;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            for (int i = 0; i < numbers.Count; i++)
            {
                Number n = numbers[i];
                if (n.age >= lifetime) continue;
                n.age += Time.deltaTime;
                float t = n.age / lifetime;
                Transform tr = n.text.transform;
                tr.position = n.origin + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));
                if (cam != null) tr.rotation = Quaternion.LookRotation(tr.position - cam.position);
                float pop = t < 0.15f ? Mathf.Lerp(1.6f, 1f, t / 0.15f) : 1f;
                tr.localScale = Vector3.one * (n.scale * pop);
                Color c = n.text.color;
                c.a = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                n.text.color = c;
                if (n.age >= lifetime) n.text.gameObject.SetActive(false);
                numbers[i] = n;
            }
        }
    }
}
