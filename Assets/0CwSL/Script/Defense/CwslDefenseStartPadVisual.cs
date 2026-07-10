using UnityEngine;
using Unity.Netcode;

/// <summary>넥서스 남쪽 공용 시작 발판 — 준비 상태 표시.</summary>
public class CwslDefenseStartPadVisual : MonoBehaviour
{
    private static CwslDefenseStartPadVisual instance;

    private Renderer padRenderer;
    private static readonly Color IdleColor = new(0.35f, 0.78f, 1f, 0.42f);
    private static readonly Color ReadyColor = new(0.35f, 1f, 0.55f, 0.72f);
    private static readonly Color LocalColor = new(1f, 0.9f, 0.35f, 0.82f);

    public static void Ensure()
    {
        if (!CwslGameConstants.UseDefenseMode)
            return;

        if (instance != null)
            return;

        var root = new GameObject("CwslDefenseStartPadVisual");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<CwslDefenseStartPadVisual>();
    }

    private void OnEnable()
    {
        CwslDefenseModeController.OnPrepStateChanged += RefreshPad;
        BuildPad();
        RefreshPad();
    }

    private void OnDisable()
    {
        CwslDefenseModeController.OnPrepStateChanged -= RefreshPad;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildPad()
    {
        if (padRenderer != null)
            Destroy(padRenderer.gameObject);

        var position = CwslDefensePrepUtility.GetSharedStartPadWorldPosition();
        var ring = CwslGroundRingVisual.Create(
            position,
            CwslDefensePrepUtility.StartPadRadius * 2.15f,
            IdleColor);
        ring.transform.SetParent(transform, false);
        padRenderer = ring.GetComponent<Renderer>();
    }

    private void RefreshPad()
    {
        var controller = CwslDefenseModeController.Instance;
        if (controller == null || padRenderer == null)
            return;

        if (controller.MatchPhase == CwslDefenseMatchPhase.Active)
        {
            if (padRenderer != null)
                padRenderer.enabled = false;
            return;
        }

        if (padRenderer != null && !padRenderer.enabled)
            padRenderer.enabled = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        var readyCount = controller.GetReadyCount();
        var required = Mathf.Max(1, controller.RequiredPlayerCount);
        var localOnPad = IsLocalPlayerOnPad();

        Color color;
        if (readyCount >= required)
            color = ReadyColor;
        else if (localOnPad)
            color = LocalColor;
        else if (readyCount > 0)
            color = Color.Lerp(IdleColor, ReadyColor, readyCount / (float)required);
        else
            color = IdleColor;

        CwslGroundRingVisual.ApplyTransparent(padRenderer, color);
    }

    private static bool IsLocalPlayerOnPad()
    {
        var network = NetworkManager.Singleton;
        if (network == null || network.LocalClient == null || network.LocalClient.PlayerObject == null)
            return false;

        return CwslDefensePrepUtility.IsOnStartPad(network.LocalClient.PlayerObject.transform.position);
    }
}
