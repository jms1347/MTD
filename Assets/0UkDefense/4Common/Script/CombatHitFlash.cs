using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 피격 시 모델 전체가 짧게 하얗게 번쩍이는 피드백 (Endless Defense 스타일).
/// MaterialPropertyBlock으로 색·발광·톤 셰이더 밝기를 올립니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class CombatHitFlash : MonoBehaviour
{
    private struct MaterialSlot
    {
        public Renderer renderer;
        public int materialIndex;
        public Color baseColor;
        public float baseColBright;
        public float baseShnIntense;
        public bool hasColor;
        public bool hasBaseColor;
        public bool hasEmission;
        public bool hasColBright;
        public bool hasShnIntense;
    }

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int ColBrightId = Shader.PropertyToID("_ColBright");
    private static readonly int ShnIntenseId = Shader.PropertyToID("_ShnIntense");

    [SerializeField] private Transform visualRoot;
    [SerializeField] private float flashDuration = 0.14f;
    [SerializeField] private float peakIntensity = 1f;
    [SerializeField] private float hdrBoost = 3.2f;
    [SerializeField] private float emissionStrength = 2.2f;

    private Health health;
    private MaterialPropertyBlock propertyBlock;
    private readonly List<MaterialSlot> slots = new();
    private Coroutine flashRoutine;
    private bool isCached;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (visualRoot == null)
            visualRoot = transform.Find("Visual");
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;

        StopFlash();
    }

    public void BindVisualRoot(Transform root)
    {
        visualRoot = root;
        isCached = false;
        StopFlash();
    }

    public void Play()
    {
        if (health != null && !health.IsAlive)
            return;

        EnsureCached();
        if (slots.Count == 0)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(RunFlash());
    }

    public void ClearForSpawn()
    {
        StopFlash();
        isCached = false;
    }

    private void HandleDamaged(float amount)
    {
        if (amount <= 0f)
            return;

        Play();
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ClearFlash();
    }

    private IEnumerator RunFlash()
    {
        ApplyFlash(peakIntensity);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / flashDuration);
            ApplyFlash(t * t * peakIntensity);
            yield return null;
        }

        ClearFlash();
        flashRoutine = null;
    }

    private void ApplyFlash(float intensity)
    {
        if (slots.Count == 0)
            return;

        intensity = Mathf.Clamp01(intensity);
        if (intensity <= 0.001f)
        {
            ClearFlash();
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        float hdr = 1f + intensity * hdrBoost;
        Color emission = Color.white * (intensity * emissionStrength);

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.renderer == null)
                continue;

            Color flashed = Color.Lerp(slot.baseColor, Color.white, intensity * 0.92f);
            flashed.r *= hdr;
            flashed.g *= hdr;
            flashed.b *= hdr;

            propertyBlock.Clear();
            if (slot.hasColor)
                propertyBlock.SetColor(ColorId, flashed);
            if (slot.hasBaseColor)
                propertyBlock.SetColor(BaseColorId, flashed);
            if (slot.hasEmission)
                propertyBlock.SetColor(EmissionColorId, emission);
            if (slot.hasColBright)
                propertyBlock.SetFloat(ColBrightId, slot.baseColBright + intensity * 1.15f);
            if (slot.hasShnIntense)
                propertyBlock.SetFloat(ShnIntenseId, slot.baseShnIntense + intensity * 1.6f);

            slot.renderer.SetPropertyBlock(propertyBlock, slot.materialIndex);
        }
    }

    private void ClearFlash()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.renderer != null)
                slot.renderer.SetPropertyBlock(null, slot.materialIndex);
        }
    }

    private void EnsureCached()
    {
        if (isCached)
            return;

        isCached = true;
        slots.Clear();

        if (visualRoot == null)
            visualRoot = transform.Find("Visual");

        if (visualRoot == null)
            return;

        var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            var renderer = renderers[r];
            if (renderer == null)
                continue;

            if (renderer.gameObject.name.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                var material = materials[m];
                if (material == null)
                    continue;

                var slot = new MaterialSlot
                {
                    renderer = renderer,
                    materialIndex = m,
                    hasColor = material.HasProperty(ColorId),
                    hasBaseColor = material.HasProperty(BaseColorId),
                    hasEmission = material.HasProperty(EmissionColorId),
                    hasColBright = material.HasProperty(ColBrightId),
                    hasShnIntense = material.HasProperty(ShnIntenseId)
                };

                if (slot.hasColor)
                    slot.baseColor = material.GetColor(ColorId);
                else if (slot.hasBaseColor)
                    slot.baseColor = material.GetColor(BaseColorId);
                else
                    slot.baseColor = Color.white;

                if (slot.hasColBright)
                    slot.baseColBright = material.GetFloat(ColBrightId);
                if (slot.hasShnIntense)
                    slot.baseShnIntense = material.GetFloat(ShnIntenseId);

                if (slot.hasColor || slot.hasBaseColor || slot.hasEmission || slot.hasColBright || slot.hasShnIntense)
                    slots.Add(slot);
            }
        }
    }
}
