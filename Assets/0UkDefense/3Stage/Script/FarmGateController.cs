using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농장 입구 문. 전투 페이즈가 시작되면 닫히고, 준비 중에는 열립니다.
/// </summary>
public class FarmGateController : MonoBehaviour
{
    private static readonly List<FarmGateController> Controllers = new();

    public static FarmGateController Instance => Controllers.Count > 0 ? Controllers[0] : null;

    [SerializeField] private Collider gateCollider;
    [SerializeField] private GameObject gateVisual;
    [SerializeField] private Collider[] extraGateColliders;

    public bool IsOpen { get; private set; } = true;

    public static bool AreAllOpen()
    {
        if (Controllers.Count == 0)
            return true;

        foreach (var controller in Controllers)
        {
            if (!controller.IsOpen)
                return false;
        }

        return true;
    }

    public void Initialize(Collider collider, GameObject visual, Collider[] additionalColliders = null)
    {
        gateCollider = collider;
        gateVisual = visual;
        extraGateColliders = additionalColliders;
        SetGateOpen(true);
    }

    private void Awake()
    {
        Controllers.Add(this);
    }

    private void OnEnable()
    {
        StartCoroutine(BindStageTimerWhenReady());
    }

    private void OnDisable()
    {
        UnbindStageTimer();
    }

    private void OnDestroy()
    {
        UnbindStageTimer();
        Controllers.Remove(this);
    }

    public static void SyncAllFromStageTimer()
    {
        if (Controllers.Count == 0)
            return;

        var phase = DefenseStageTimerManager.Instance != null
            ? DefenseStageTimerManager.Instance.CurrentPhase
            : DefenseStagePhase.PreBattleCountdown;

        foreach (var controller in Controllers)
            controller.ApplyPhase(phase);
    }

    private IEnumerator BindStageTimerWhenReady()
    {
        while (DefenseStageTimerManager.Instance == null)
            yield return null;

        UnbindStageTimer();
        DefenseStageTimerManager.Instance.OnStageStateChanged += HandleStageChanged;
        ApplyPhase(DefenseStageTimerManager.Instance.CurrentPhase);
    }

    private void UnbindStageTimer()
    {
        if (DefenseStageTimerManager.Instance != null)
            DefenseStageTimerManager.Instance.OnStageStateChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int stage, DefenseStagePhase phase, float secondsRemaining, string message)
    {
        ApplyPhase(phase);
    }

    private void ApplyPhase(DefenseStagePhase phase)
    {
        SetGateOpen(phase != DefenseStagePhase.Battle);
    }

    public void SetGateOpen(bool open)
    {
        IsOpen = open;

        if (gateVisual != null)
            gateVisual.SetActive(!open);

        if (gateCollider != null)
            gateCollider.enabled = !open;

        if (extraGateColliders == null)
            return;

        foreach (var collider in extraGateColliders)
        {
            if (collider == null)
                continue;

            if (collider.gameObject != gateVisual)
                collider.gameObject.SetActive(!open);

            collider.enabled = !open;
        }
    }
}
