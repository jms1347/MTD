using System.Collections;
using UnityEngine;

/// <summary>캐릭터 비주얼·스킬·상태 애니메이션 테스트용 컨트롤러.</summary>
[ExecuteAlways]
public class CwslPlayerVisualTestController : MonoBehaviour
{
    private static readonly CwslCharacterId[] PreviewCharacters =
    {
        CwslCharacterId.Tank,
        CwslCharacterId.MissileTank,
        CwslCharacterId.RedMage,
        CwslCharacterId.MomentumRammer,
        CwslCharacterId.CrowdGatherer
    };

    [SerializeField] private CwslGameAssets assets;
    [SerializeField] private CwslCharacterId previewCharacter = CwslCharacterId.Tank;
    [SerializeField] private bool showHudInPlayMode = true;
    [SerializeField] private Vector3 spawnOffset = new(-4f, 0f, 0f);
    [SerializeField] private float dummyMonsterSpacing = 2.8f;

    private Transform previewRoot;
    private CwslPlayerVisualTestWalker walker;
    private CwslPlayerVisualTestFortifyMock fortifyMock;
    private Transform dummyMonsterRoot;
    private Vector2 scroll;
    private bool guiFoldout = true;
    private bool fortifyActive;
    private bool useWasdMovement = true;

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
        CwslVisualTestAssetsContext.Set(assets);
        RebuildPreview();
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

        const int width = 320;
        var area = new Rect(Screen.width - width - 12f, 12f, width, Screen.height - 24f);
        GUILayout.BeginArea(area, GUI.skin.box);
        guiFoldout = GUILayout.Toggle(guiFoldout, "캐릭터 비주얼 테스트", GUI.skin.button);
        if (!guiFoldout)
        {
            GUILayout.EndArea();
            return;
        }

        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("캐릭터 선택");
        foreach (var character in PreviewCharacters)
        {
            if (GUILayout.Button(character.ToString(), previewCharacter == character ? GUI.skin.box : GUI.skin.button))
            {
                previewCharacter = character;
                fortifyActive = false;
                RebuildPreview();
            }
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("미리보기 재생성"))
            RebuildPreview();

        useWasdMovement = GUILayout.Toggle(useWasdMovement, "WASD 이동");
        if (walker != null)
            walker.SetActive(useWasdMovement);

        GUILayout.Space(8f);
        GUILayout.Label("기본 / 스킬");
        if (GUILayout.Button("일반 공격"))
            PlayBasicAttack();
        if (GUILayout.Button("Q — 방패 강화 토글"))
            ToggleFortify();
        if (GUILayout.Button("W — 돌진 (탱커)"))
            PlayTankDash();
        if (GUILayout.Button("E — 지면 강타 (탱커)"))
            PlayTankSlam();
        if (GUILayout.Button("R — 휠윈드 (탱커)"))
            PlayTankWhirlwind();
        if (GUILayout.Button("F — 보조 스킬"))
            PlayAuxSkill();

        GUILayout.Space(8f);
        GUILayout.Label("상태");
        if (GUILayout.Button("스턴 VFX (캐릭터)"))
            PlayPlayerStun();
        if (GUILayout.Button("스턴 VFX (더미 몬스터)"))
            PlayDummyMonsterStun();
        if (GUILayout.Button("더미 몬스터 재생성"))
            RebuildDummyMonster();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    [ContextMenu("Rebuild Preview")]
    public void RebuildPreview()
    {
        ClearChildren(previewRoot);
        var center = transform.position + spawnOffset;
        previewRoot = CreatePreview(previewCharacter, center);
        walker = previewRoot != null ? previewRoot.GetComponent<CwslPlayerVisualTestWalker>() : null;
        fortifyMock = previewRoot != null ? previewRoot.GetComponent<CwslPlayerVisualTestFortifyMock>() : null;
        if (walker != null)
            walker.SetActive(useWasdMovement);
        if (fortifyMock != null)
            fortifyMock.SetFortifyActive(fortifyActive);

        if (Application.isPlaying)
            RebuildDummyMonster();
    }

    public void RebuildDummyMonster()
    {
        ClearChildren(dummyMonsterRoot);
        if (!Application.isPlaying || previewRoot == null)
            return;

        var position = previewRoot.position + previewRoot.forward * dummyMonsterSpacing;
        dummyMonsterRoot = CreateDummyMonster(position);
    }

    public void PlayBasicAttack()
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        var visual = previewRoot.Find("Visual");
        if (visual == null)
            return;

        var target = previewRoot.position + previewRoot.forward * 2.4f;
        switch (previewCharacter)
        {
            case CwslCharacterId.Tank:
                var bash = visual.GetComponent<CwslPlayerShieldBashVisual>();
                if (bash != null)
                    StartCoroutine(TankBasicAttackRoutine(bash, target));
                break;
            case CwslCharacterId.MissileTank:
                visual.GetComponent<CwslPlayerGunShootVisual>()?.PlayShoot(target, false);
                break;
            case CwslCharacterId.RedMage:
                visual.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
                break;
            case CwslCharacterId.MomentumRammer:
                StartCoroutine(RammerChargeRoutine());
                break;
            case CwslCharacterId.CrowdGatherer:
                CwslGatherChargeVisual.BeginBlackHoleZone(
                    previewRoot.position + previewRoot.forward * 1.2f,
                    CwslGameConstants.GatherBlackHoleZoneRadius);
                break;
        }
    }

    public void ToggleFortify()
    {
        if (previewCharacter != CwslCharacterId.Tank || fortifyMock == null)
            return;

        fortifyActive = !fortifyActive;
        fortifyMock.SetFortifyActive(fortifyActive);
        if (fortifyActive)
            CwslVfxSpawner.SpawnFortifyAura(previewRoot);
    }

    public void PlayTankDash()
    {
        if (!Application.isPlaying || previewCharacter != CwslCharacterId.Tank || previewRoot == null)
            return;

        var visual = previewRoot.Find("Visual");
        if (visual == null)
            return;

        var direction = previewRoot.forward;
        var dashWave = visual.GetComponent<CwslTankShieldDashWaveVisual>();
        if (dashWave == null)
            dashWave = visual.gameObject.AddComponent<CwslTankShieldDashWaveVisual>();
        dashWave.PlayDashWave(direction, fortifyActive, CwslGameConstants.TankShieldDashDuration);
        StartCoroutine(SimulateDashMove(direction));
    }

    public void PlayTankSlam()
    {
        if (!Application.isPlaying || previewCharacter != CwslCharacterId.Tank || previewRoot == null)
            return;

        var visual = previewRoot.Find("Visual")?.GetComponent<CwslTankShieldSkillVisual>();
        if (visual == null)
            return;

        visual.PlaySlam(fortifyActive);
        CwslCameraShake.Play(
            CwslGameConstants.TankShieldSlamShakeDuration * (fortifyActive ? 1.2f : 1f),
            CwslGameConstants.TankShieldSlamShakeMagnitude
                * CwslTankSkillEmpower.GetPowerMultiplier(fortifyActive));
    }

    public void PlayTankWhirlwind()
    {
        if (!Application.isPlaying || previewCharacter != CwslCharacterId.Tank || previewRoot == null)
            return;

        previewRoot.Find("Visual")?.GetComponent<CwslTankShieldSkillVisual>()
            ?.PlayWhirlwind(CwslGameConstants.TankShieldWhirlwindDuration, fortifyActive);
    }

    public void PlayAuxSkill()
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        switch (previewCharacter)
        {
            case CwslCharacterId.MissileTank:
                previewRoot.Find("Visual")?.GetComponent<CwslPlayerGunShootVisual>()
                    ?.PlayShoot(previewRoot.position + previewRoot.forward * 3f, true);
                break;
            case CwslCharacterId.RedMage:
                previewRoot.Find("Visual")?.GetComponent<CwslPlayerStaffCastVisual>()?.PlayCast();
                break;
            case CwslCharacterId.MomentumRammer:
                previewRoot.Find("Visual")?.GetComponent<CwslPlayerRammerBrakeVisual>()?.PlayBrake();
                break;
            case CwslCharacterId.CrowdGatherer:
                CwslGatherChargeVisual.PlayPull(
                    previewRoot.position + previewRoot.forward * 1.2f,
                    CwslGameConstants.GatherMinRadius);
                break;
        }
    }

    public void PlayPlayerStun()
    {
        if (!Application.isPlaying || previewRoot == null)
            return;

        var position = previewRoot.position;
        CwslRammerStunFeedback.PlaySound(position);
        CwslVfxSpawner.SpawnRammerStunExplosion(position);

        if (previewCharacter == CwslCharacterId.MomentumRammer)
        {
            previewRoot.Find("Visual")?.GetComponent<CwslPlayerRammerStunVisual>()?.PlayStunVfx(position);
            return;
        }

        CwslMonsterStunVisual.Ensure(previewRoot.gameObject).PlayStun(position, 2f);
    }

    public void PlayDummyMonsterStun()
    {
        if (!Application.isPlaying || dummyMonsterRoot == null)
            RebuildDummyMonster();

        if (dummyMonsterRoot == null)
            return;

        CwslMonsterStunVisual.Ensure(dummyMonsterRoot.gameObject)
            .PlayStun(dummyMonsterRoot.position, CwslGameConstants.TankShieldSlamStunDuration);
    }

    private IEnumerator RammerChargeRoutine()
    {
        var direction = previewRoot.forward;
        var origin = previewRoot.position;
        var duration = 0.5f;
        var distance = 3.6f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            previewRoot.position = origin + direction * (distance * t);
            yield return null;
        }
    }

    private IEnumerator TankBasicAttackRoutine(CwslPlayerShieldBashVisual bash, Vector3 target)
    {
        bash.PlayWindup(target);
        yield return new WaitForSeconds(0.34f);
        bash.PlayImpact(target, fortifyActive);
    }

    private IEnumerator SimulateDashMove(Vector3 direction)
    {
        var duration = CwslGameConstants.TankShieldDashDuration;
        var distance = CwslGameConstants.TankShieldDashDistance;
        var origin = previewRoot.position;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            previewRoot.position = origin + direction * (distance * t);
            yield return null;
        }
    }

    private Transform CreatePreview(CwslCharacterId character, Vector3 position)
    {
        var instance = new GameObject("Preview_" + character);
        instance.transform.SetParent(transform, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var color = ResolveCharacterColor(character);
        switch (character)
        {
            case CwslCharacterId.MissileTank:
                CwslMonsterVisualBuilder.BuildMissileTankPlayer(instance.transform, color);
                break;
            case CwslCharacterId.RedMage:
                CwslMonsterVisualBuilder.BuildRedMagePlayer(instance.transform, new Color(0.9f, 0.15f, 0.1f));
                break;
            case CwslCharacterId.MomentumRammer:
                CwslMonsterVisualBuilder.BuildMomentumRammerPlayer(instance.transform, color);
                break;
            case CwslCharacterId.CrowdGatherer:
                CwslMonsterVisualBuilder.BuildCrowdGathererPlayer(instance.transform, color);
                break;
            default:
                CwslMonsterVisualBuilder.BuildPlayer(instance.transform, color);
                break;
        }

        if (character == CwslCharacterId.Tank)
            instance.AddComponent<CwslPlayerVisualTestFortifyMock>();

        if (instance.GetComponent<CwslPlayerVisualTestWalker>() == null)
            instance.AddComponent<CwslPlayerVisualTestWalker>();

        return instance.transform;
    }

    private Transform CreateDummyMonster(Vector3 position)
    {
        var instance = new GameObject("Preview_DummyMelee");
        instance.transform.SetParent(transform, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.LookRotation(previewRoot.position - position);
        CwslMonsterVisualBuilder.Build(instance.transform, CwslMonsterType.Melee);
        CwslMonsterVisualRefresh.Refresh(instance.transform, CwslMonsterType.Melee);
        instance.AddComponent<CwslMonsterStunVisual>();
        return instance.transform;
    }

    private static Color ResolveCharacterColor(CwslCharacterId character)
    {
        return character switch
        {
            CwslCharacterId.MissileTank => new Color(0.35f, 0.55f, 0.28f),
            CwslCharacterId.MomentumRammer => new Color(0.72f, 0.42f, 0.18f),
            CwslCharacterId.CrowdGatherer => new Color(0.28f, 0.42f, 0.82f),
            _ => new Color(0.28f, 0.42f, 0.72f)
        };
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
        if (target == dummyMonsterRoot)
            dummyMonsterRoot = null;
    }
}
