using System.Collections.Generic;
using UnityEngine;

/// <summary>UkDefense DefenseCombatCatalog 몬스터 상태 VFX (머리/몸/발 부착).</summary>
public class CwslMonsterStatusVfx : MonoBehaviour
{
    private sealed class Bundle
    {
        public GameObject Head;
        public GameObject Body;
        public GameObject Foot;
    }

    private readonly Dictionary<CwslMonsterStatusKind, Bundle> bundles = new();

    public static CwslMonsterStatusVfx Ensure(GameObject root)
    {
        if (root == null)
            return null;

        var visual = root.GetComponent<CwslMonsterStatusVfx>();
        if (visual == null)
            visual = root.AddComponent<CwslMonsterStatusVfx>();

        return visual;
    }

    public void SetStatusActive(CwslMonsterStatusKind kind, bool active)
    {
        if (active)
            EnsureStatus(kind);
        else
            ClearStatus(kind);
    }

    public void ClearAll()
    {
        foreach (CwslMonsterStatusKind kind in System.Enum.GetValues(typeof(CwslMonsterStatusKind)))
            ClearStatus(kind);
    }

    private void EnsureStatus(CwslMonsterStatusKind kind)
    {
        if (bundles.ContainsKey(kind))
            return;

        var assets = CwslVfxSpawner.GetAssets();
        if (assets == null)
            return;

        GameObject headPrefab = null;
        GameObject bodyPrefab = null;
        GameObject footPrefab = null;
        var headY = 1.15f;
        var bodyY = 0.45f;
        var footY = 0.08f;
        var bodyScale = 0.55f;
        var footScale = 0.42f;

        switch (kind)
        {
            case CwslMonsterStatusKind.Burning:
                bodyPrefab = assets.monsterBurnStatusVfx;
                bodyScale = 0.55f;
                break;
            case CwslMonsterStatusKind.Slowed:
                footPrefab = assets.monsterSlowStatusVfx;
                footY = 0.04f;
                footScale = 0.55f;
                break;
            case CwslMonsterStatusKind.Shocked:
                headPrefab = assets.monsterShockStatusVfx;
                break;
            case CwslMonsterStatusKind.Poisoned:
                bodyPrefab = assets.monsterPoisonStatusVfx;
                bodyScale = 0.65f;
                break;
        }

        if (headPrefab == null && bodyPrefab == null && footPrefab == null)
            return;

        var scaleY = Mathf.Max(0.45f, transform.lossyScale.y);
        var bundle = new Bundle();

        if (headPrefab != null)
        {
            bundle.Head = CwslVfxSpawner.AttachMonsterStatusEffect(
                headPrefab,
                transform,
                new Vector3(0f, headY * scaleY, 0f),
                1f,
                CwslEtfxVfxOrientation.HeadStatusAttachRotation);
        }

        if (bodyPrefab != null)
        {
            bundle.Body = CwslVfxSpawner.AttachMonsterStatusEffect(
                bodyPrefab,
                transform,
                new Vector3(0f, bodyY * scaleY, 0f),
                bodyScale);
        }

        if (footPrefab != null)
        {
            bundle.Foot = CwslVfxSpawner.AttachMonsterStatusEffect(
                footPrefab,
                transform,
                new Vector3(0f, footY * scaleY, 0f),
                footScale);
        }

        bundles[kind] = bundle;
    }

    private void ClearStatus(CwslMonsterStatusKind kind)
    {
        if (!bundles.TryGetValue(kind, out var bundle))
            return;

        if (bundle.Head != null)
            Destroy(bundle.Head);
        if (bundle.Body != null)
            Destroy(bundle.Body);
        if (bundle.Foot != null)
            Destroy(bundle.Foot);

        bundles.Remove(kind);
    }

    private void OnDestroy()
    {
        ClearAll();
    }
}
