using TMPro;
using UnityEngine;

/// <summary>지뢰형 폭탄 부착 시 심지 연소, 다리 숨김, 적 머리 위 3·2·1 카운트다운.</summary>
public class CwslStickyMineFuseBurnVisual : MonoBehaviour
{
    private static readonly string[] LegNames = { "LegL", "LegR", "LegBL", "LegBR" };

    private Transform fuseTop;
    private Transform fuseBottom;
    private Transform fuseTip;
    private Transform timerPanel;
    private CwslSuicideFuseVisual fuseVisual;
    private CwslMonsterLegWalkVisual legWalkVisual;
    private Transform visualRoot;
    private Transform countdownHost;
    private TextMeshPro countdownLabel;

    private bool burning;
    private float burnDuration = 3f;
    private float burnElapsed;

    public void Configure(Transform top, Transform bottom, Transform tip, Transform timer)
    {
        fuseTop = top;
        fuseBottom = bottom;
        fuseTip = tip;
        timerPanel = timer;
        fuseVisual = tip != null ? tip.GetComponent<CwslSuicideFuseVisual>() : null;
        visualRoot = transform.parent;
        legWalkVisual = visualRoot != null ? visualRoot.GetComponent<CwslMonsterLegWalkVisual>() : null;
        ResetVisual();
    }

    public void BeginAttach(Transform hostAnchor, float durationSeconds)
    {
        countdownHost = hostAnchor;
        HideLegs(true);
        EnsureCountdownLabel();
        BeginBurn(durationSeconds);
    }

    public void BeginBurn(float durationSeconds)
    {
        burnDuration = Mathf.Max(0.1f, durationSeconds);
        burnElapsed = 0f;
        burning = true;

        if (timerPanel != null)
            timerPanel.gameObject.SetActive(false);

        fuseVisual?.SetBurningActive(true);
        UpdateBurnPoint(0f);
        UpdateCountdownLabel();
    }

    public void StopBurn()
    {
        burning = false;
        ResetVisual();
    }

    private void ResetVisual()
    {
        burning = false;
        burnElapsed = 0f;
        countdownHost = null;

        HideLegs(false);
        DestroyCountdownLabel();

        if (timerPanel != null)
            timerPanel.gameObject.SetActive(false);

        fuseVisual?.SetBurningActive(false);

        if (fuseTip != null && fuseTop != null)
            fuseTip.position = fuseTop.position;
    }

    private void Update()
    {
        if (!burning)
            return;

        burnElapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(burnElapsed / burnDuration);
        UpdateBurnPoint(progress);
        UpdateCountdownLabel();

        if (burnElapsed >= burnDuration)
            burning = false;
    }

    private void OnDestroy()
    {
        DestroyCountdownLabel();
    }

    private void HideLegs(bool hidden)
    {
        if (visualRoot == null)
            visualRoot = transform.parent;

        if (visualRoot == null)
            return;

        foreach (var legName in LegNames)
        {
            var leg = visualRoot.Find(legName);
            if (leg != null)
                leg.gameObject.SetActive(!hidden);
        }

        if (legWalkVisual != null)
            legWalkVisual.enabled = !hidden;
    }

    private void EnsureCountdownLabel()
    {
        if (countdownLabel != null || countdownHost == null)
            return;

        var labelRoot = new GameObject("MineCountdown");
        labelRoot.transform.SetParent(countdownHost, false);
        labelRoot.transform.localPosition = Vector3.up * 2.25f;
        labelRoot.AddComponent<CwslBillboardToCamera>();

        countdownLabel = labelRoot.AddComponent<TextMeshPro>();
        countdownLabel.fontSize = 8f;
        countdownLabel.fontStyle = FontStyles.Bold;
        countdownLabel.alignment = TextAlignmentOptions.Center;
        countdownLabel.color = new Color(1f, 0.28f, 0.12f);
        countdownLabel.rectTransform.sizeDelta = new Vector2(3f, 3f);
        CwslTmpFontUtil.ApplyFont(countdownLabel);
    }

    private void UpdateCountdownLabel()
    {
        if (countdownLabel == null)
            return;

        var remaining = Mathf.Max(0f, burnDuration - burnElapsed);
        var seconds = Mathf.CeilToInt(remaining);
        countdownLabel.text = seconds > 0 ? seconds.ToString() : string.Empty;
        countdownLabel.gameObject.SetActive(seconds > 0);
    }

    private void DestroyCountdownLabel()
    {
        if (countdownLabel == null)
            return;

        if (Application.isPlaying)
            Destroy(countdownLabel.gameObject);
        else
            DestroyImmediate(countdownLabel.gameObject);

        countdownLabel = null;
    }

    private void UpdateBurnPoint(float progress)
    {
        if (fuseTip == null || fuseTop == null || fuseBottom == null)
            return;

        fuseTip.position = Vector3.Lerp(fuseTop.position, fuseBottom.position, progress);
    }
}
