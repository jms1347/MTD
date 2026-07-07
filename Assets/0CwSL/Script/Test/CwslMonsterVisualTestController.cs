using System.Collections;
using UnityEngine;

/// <summary>몬스터 비주얼·이펙트·걷기 애니메이션 테스트용 컨트롤러.</summary>
[ExecuteAlways]
public class CwslMonsterVisualTestController : MonoBehaviour
{
    private static readonly CwslMonsterType[] PreviewTypes =
    {
        CwslMonsterType.Melee,
        CwslMonsterType.Ranged,
        CwslMonsterType.InkSniper,
        CwslMonsterType.Suicide,
        CwslMonsterType.StickySuicide,
        CwslMonsterType.KoreaUniversitySoldier,
        CwslMonsterType.NexusMelee,
        CwslMonsterType.NexusRanged,
        CwslMonsterType.NexusSuicide,
        CwslMonsterType.MidBoss,
        CwslMonsterType.DefenseBoss,
        CwslMonsterType.SeniorCoach,
        CwslMonsterType.BossHongmyeongbo
    };

    [SerializeField] private CwslGameAssets assets;
    [SerializeField] private CwslMonsterType previewType = CwslMonsterType.Suicide;
    [SerializeField] private bool autoWalkInPlayMode = true;
    [SerializeField] private bool showHudInPlayMode = true;
    [SerializeField] private Vector3 spawnOffset = new(4f, 0f, 0f);
    [SerializeField] private float spawnSpacing = 2.4f;

    private Transform previewRoot;
    private CwslMonsterVisualTestWalker walker;
    private Transform allyPreviewRoot;
    private Vector2 scroll;
    private bool guiFoldout = true;

    private void OnEnable()
    {
        EnsureAssetsLoaded();
        if (!Application.isPlaying)
            RebuildPreview();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        EnsureAssetsLoaded();
        RebuildPreview();
        if (autoWalkInPlayMode)
            SetAutoWalk(true);
    }

    private void EnsureAssetsLoaded()
    {
        if (assets != null)
            return;

#if UNITY_EDITOR
        assets = UnityEditor.AssetDatabase.LoadAssetAtPath<CwslGameAssets>("Assets/0CwSL/Data/CwslGameAssets.asset");
#endif
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showHudInPlayMode)
            return;

        const int width = 300;
        var area = new Rect(12f, 12f, width, Screen.height - 24f);
        GUILayout.BeginArea(area, GUI.skin.box);
        guiFoldout = GUILayout.Toggle(guiFoldout, "몬스터 비주얼 테스트", GUI.skin.button);
        if (!guiFoldout)
        {
            GUILayout.EndArea();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("타입 선택 후 Rebuild 또는 아래 버튼");
        foreach (var type in PreviewTypes)
        {
            if (GUILayout.Button(type.ToString(), previewType == type ? GUI.skin.box : GUI.skin.button))
            {
                previewType = type;
                RebuildPreview();
            }
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("미리보기 재생성"))
            RebuildPreview();

        autoWalkInPlayMode = GUILayout.Toggle(autoWalkInPlayMode, "자동 걷기 (다리 애니)");
        if (GUILayout.Button(autoWalkInPlayMode ? "걷기 중지" : "걷기 시작"))
            SetAutoWalk(!autoWalkInPlayMode);

        GUILayout.Space(8f);
        GUILayout.Label("이펙트 / 애니");
        if (GUILayout.Button("근접 돌진 (Windup+Hit)"))
            PlayMeleeLunge();
        if (GUILayout.Button("자폭 폭발 VFX"))
            PlaySuicideExplosion();
        if (GUILayout.Button("몬스터 사망 VFX"))
            PlayDeathVfx();
        if (GUILayout.Button("스턴 VFX"))
            PlayStunVfx();
        if (GUILayout.Button("화상 VFX (UkDefense)"))
            PlayStatusVfx(CwslMonsterStatusKind.Burning);
        if (GUILayout.Button("동상 VFX (UkDefense)"))
            PlayStatusVfx(CwslMonsterStatusKind.Slowed);
        if (GUILayout.Button("감전 VFX (UkDefense)"))
            PlayStatusVfx(CwslMonsterStatusKind.Shocked);
        if (GUILayout.Button("중독 VFX (UkDefense)"))
            PlayStatusVfx(CwslMonsterStatusKind.Poisoned);
        if (GUILayout.Button("상태 VFX 전부 제거"))
            ClearStatusVfx();
        if (GUILayout.Button("장착형 + 아군 병사 셋업"))
            SetupStickyAttachDemo();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    [ContextMenu("Rebuild Preview")]
    public void RebuildPreview()
    {
        ClearChildren(previewRoot);
        ClearChildren(allyPreviewRoot);

        var center = transform.position + spawnOffset;
        previewRoot = CreatePreview(previewType, center);
        walker = previewRoot != null ? previewRoot.GetComponent<CwslMonsterVisualTestWalker>() : null;

        if (Application.isPlaying && autoWalkInPlayMode)
            SetAutoWalk(true);
    }

    public void SetAutoWalk(bool enabled)
    {
        autoWalkInPlayMode = enabled;
        if (walker == null)
            return;

        if (enabled)
            walker.Begin(transform.position + spawnOffset);
        else
            walker.Stop();
    }

    public void PlayMeleeLunge()
    {
        var visual = previewRoot != null ? previewRoot.Find("Visual") : null;
        if (visual == null)
            return;

        var slimeVisual = visual.GetComponent<CwslSlimeMeleeVisual>();
        if (slimeVisual != null)
        {
            if (!Application.isPlaying)
                return;

            StartCoroutine(SlimeMeleeRoutine(slimeVisual));
            return;
        }

        var lunge = visual.GetComponent<CwslMeleeLungeVisual>();
        if (lunge == null)
            lunge = visual.gameObject.AddComponent<CwslMeleeLungeVisual>();

        if (!Application.isPlaying)
            return;

        StartCoroutine(MeleeLungeRoutine(lunge));
    }

    public void PlaySuicideExplosion()
    {
        var position = ResolvePreviewBodyPosition();
        if (Application.isPlaying && CwslGameSession.Instance != null)
            CwslVfxSpawner.SpawnSuicideExplosion(position);
        else
            SpawnEffectVfx(assets != null ? assets.suicideExplosionVfx : null, position, CwslGameConstants.SuicideExplosionScale);
    }

    public void PlayDeathVfx()
    {
        var position = ResolvePreviewBodyPosition();
        SpawnEffectVfx(ResolveDeathVfxPrefab(), position + Vector3.up * 0.5f, 1f);
    }

    public void PlayStunVfx()
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        CwslMonsterStunVisual.Ensure(previewRoot.gameObject)
            .PlayStun(ResolvePreviewBodyPosition(), CwslGameConstants.TankShieldSlamStunDuration);
    }

    public void PlayStatusVfx(CwslMonsterStatusKind kind)
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        CwslVisualTestAssetsContext.Set(assets);
        CwslMonsterStatusVfx.Ensure(previewRoot.gameObject)?.SetStatusActive(kind, true);
    }

    public void ClearStatusVfx()
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        CwslMonsterStatusVfx.Ensure(previewRoot.gameObject)?.ClearAll();
    }

    public void SetupStickyAttachDemo()
    {
        previewType = CwslMonsterType.StickySuicide;
        var center = transform.position + spawnOffset;
        ClearChildren(previewRoot);
        ClearChildren(allyPreviewRoot);

        allyPreviewRoot = CreatePreview(CwslMonsterType.KoreaUniversitySoldier, center + Vector3.left * spawnSpacing);
        previewRoot = CreatePreview(CwslMonsterType.StickySuicide, center + Vector3.right * (spawnSpacing * 0.35f));

        if (allyPreviewRoot != null && previewRoot != null && Application.isPlaying)
            StartCoroutine(StickyApproachRoutine(allyPreviewRoot, previewRoot));
    }

    private IEnumerator SlimeMeleeRoutine(CwslSlimeMeleeVisual slimeVisual)
    {
        slimeVisual.PlayWindup();
        yield return new WaitForSeconds(0.14f);
        slimeVisual.PlayHit();
    }

    private IEnumerator MeleeLungeRoutine(CwslMeleeLungeVisual lunge)
    {
        lunge.PlayWindup();
        yield return new WaitForSeconds(0.14f);
        lunge.PlayHit();
    }

    private IEnumerator StickyApproachRoutine(Transform ally, Transform sticky)
    {
        var walkerComponent = sticky.GetComponent<CwslMonsterVisualTestWalker>();
        walkerComponent?.Stop();

        var target = ally.position + Vector3.up * 1.05f;
        while (Vector3.Distance(sticky.position, target) > 0.08f)
        {
            var flat = target - sticky.position;
            flat.y = 0f;
            sticky.position += flat.normalized * (Time.deltaTime * 3.2f);
            if (flat.sqrMagnitude > 0.0001f)
                sticky.rotation = Quaternion.LookRotation(flat.normalized);
            yield return null;
        }

        sticky.position = target;

        if (Application.isPlaying)
        {
            var fuseBurn = sticky.GetComponentInChildren<CwslStickyMineFuseBurnVisual>(true);
            fuseBurn?.BeginAttach(ally, 3f);
        }
    }

    private Transform CreatePreview(CwslMonsterType type, Vector3 position)
    {
        var prefab = ResolvePrefab(type);
        GameObject instance;
        if (prefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.identity;
            }
            else
#endif
            {
                instance = Instantiate(prefab, position, Quaternion.identity, transform);
            }

            DisableNetworkComponents(instance);
        }
        else
        {
            instance = new GameObject("Preview_" + type);
            instance.transform.SetParent(transform, false);
            instance.transform.position = position;
            CwslMonsterVisualBuilder.Build(instance.transform, type);
        }

        instance.name = "Preview_" + type;
        CwslMonsterVisualRefresh.Refresh(instance.transform, type);
        EnsurePreviewExtras(instance.transform, type);
        ApplyThreatLight(instance.transform, type);

        if (instance.GetComponent<CwslMonsterVisualTestWalker>() == null)
            instance.AddComponent<CwslMonsterVisualTestWalker>();

        return instance.transform;
    }

    private static void EnsurePreviewExtras(Transform root, CwslMonsterType type)
    {
        var visual = root.Find("Visual");
        if (visual == null)
            return;

        if (type is CwslMonsterType.Melee or CwslMonsterType.NexusMelee or CwslMonsterType.KoreaUniversitySoldier
            or CwslMonsterType.MidBoss)
        {
            if (visual.GetComponent<CwslMeleeLungeVisual>() == null)
                visual.gameObject.AddComponent<CwslMeleeLungeVisual>();
        }

        if (root.GetComponent<CwslMonsterStunVisual>() == null)
            root.gameObject.AddComponent<CwslMonsterStunVisual>();
    }

    private static void ApplyThreatLight(Transform root, CwslMonsterType type)
    {
        var color = CwslMonsterVisualPalette.GetThreatLightColor(type);
        var isSuicide = type is CwslMonsterType.Suicide or CwslMonsterType.NexusSuicide or CwslMonsterType.StickySuicide;
        var isRanged = type is CwslMonsterType.Ranged or CwslMonsterType.NexusRanged
            or CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper;
        var isNexus = CwslMonsterTypeUtil.IsNexusPriority(type);
        if (!isSuicide && !isRanged && !isNexus)
            return;

        var range = isSuicide ? 5.5f : isNexus ? 4.2f : 3.2f;
        var intensity = isSuicide ? 3.2f : isNexus ? 2.4f : 1.4f;
        var offsetY = isSuicide ? 0.8f : 1.0f;
        CwslThreatLight.Ensure(root, color, range, intensity, new Vector3(0f, offsetY, 0f));
    }

    private GameObject ResolvePrefab(CwslMonsterType type)
    {
        if (assets == null)
            return null;

        return type switch
        {
            CwslMonsterType.Ranged or CwslMonsterType.NexusRanged => assets.rangedMonsterPrefab,
            CwslMonsterType.InkSniper or CwslMonsterType.NexusInkSniper =>
                assets.inkSniperMonsterPrefab != null
                    ? assets.inkSniperMonsterPrefab
                    : assets.rangedMonsterPrefab,
            CwslMonsterType.Suicide or CwslMonsterType.NexusSuicide => assets.suicideMonsterPrefab,
            CwslMonsterType.StickySuicide => assets.stickySuicideMonsterPrefab,
            CwslMonsterType.Melee => assets.meleeMonsterPrefab,
            CwslMonsterType.NexusMelee =>
                assets.nexusMeleeMonsterPrefab != null
                    ? assets.nexusMeleeMonsterPrefab
                    : assets.meleeMonsterPrefab,
            CwslMonsterType.KoreaUniversitySoldier => assets.koreaUniversitySoldierPrefab,
            CwslMonsterType.MidBoss => assets.midBossMonsterPrefab,
            CwslMonsterType.DefenseBoss => assets.defenseBossMonsterPrefab,
            CwslMonsterType.SeniorCoach => assets.seniorCoachMonsterPrefab,
            CwslMonsterType.BossHongmyeongbo => assets.bossPrefab,
            _ => null
        };
    }

    private static void DisableNetworkComponents(GameObject root)
    {
        foreach (var behaviour in root.GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>(true))
            behaviour.enabled = false;
    }

    private GameObject ResolveDeathVfxPrefab()
    {
        if (assets == null)
            return null;

        return assets.enemyDeathVfx
               ?? assets.bossDeathVfx
               ?? assets.suicideBomberDeathVfx;
    }

    private Vector3 ResolvePreviewBodyPosition()
    {
        if (previewRoot == null)
            return transform.position + spawnOffset + Vector3.up * 0.58f;

        var visual = previewRoot.Find("Visual");
        if (visual != null)
        {
            var anchor = visual.Find("ExplosionAnchor");
            if (anchor != null)
                return anchor.position;

            return visual.TransformPoint(new Vector3(0f, 0.58f, 0f));
        }

        return previewRoot.position + Vector3.up * 0.58f;
    }

    private static void SpawnEffectVfx(GameObject prefab, Vector3 position, float scale)
    {
        if (prefab == null)
        {
            CwslSimpleVfx.SpawnBurst(position, new Color(1f, 0.45f, 0.1f), 1f, 0.4f);
            return;
        }

        var instance = Instantiate(prefab, position, Quaternion.identity);
        if (Mathf.Abs(scale - 1f) > 0.001f)
            instance.transform.localScale = Vector3.one * scale;

        RestartEffectParticles(instance);
        Destroy(instance, 4f);
    }

    private static void RestartEffectParticles(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private static void SpawnVfx(GameObject prefab, Vector3 position, float scale)
    {
        SpawnEffectVfx(prefab, position, scale);
    }

    private void ClearChildren(Transform target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target.gameObject);
        else
            DestroyImmediate(target.gameObject);

        if (target == previewRoot)
            previewRoot = null;
        if (target == allyPreviewRoot)
            allyPreviewRoot = null;
    }
}
