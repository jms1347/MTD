using UnityEngine;

/// <summary>
/// 마법 카드(지면 조준형) — 마우스 위치에 범위 표시 후 클릭으로 시전.
/// </summary>
public class RoguelikeMagicTargetController : MonoBehaviour
{
    private RoguelikeCardManager manager;
    private int pendingHandIndex = -1;
    private RoguelikeCardData pendingCard;
    private GameObject rangeIndicator;

    public bool IsTargeting => pendingHandIndex >= 0 && pendingCard != null;

    public void Initialize(RoguelikeCardManager cardManager)
    {
        manager = cardManager;
    }

    public bool BeginTargeting(int handIndex, RoguelikeCardData card)
    {
        if (manager == null || card == null)
            return false;

        if (card.magicUseMode != RoguelikeMagicUseMode.GroundTarget)
            return false;

        pendingHandIndex = handIndex;
        pendingCard = card;
        EnsureRangeIndicator();
        UpdateRangeIndicator();
        return true;
    }

    private void Update()
    {
        if (!IsTargeting)
            return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargeting();
            return;
        }

        if (!TryGetGroundPoint(out Vector3 groundPoint))
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false);
            return;
        }

        UpdateRangeIndicator(groundPoint);

        if (Input.GetMouseButtonDown(0))
            ConfirmAt(groundPoint);
    }

    private void ConfirmAt(Vector3 groundPoint)
    {
        if (!IsTargeting)
            return;

        int index = pendingHandIndex;
        var card = pendingCard;
        CancelTargeting();

        if (manager != null)
            manager.ConfirmGroundMagicCast(index, card, groundPoint);
    }

    public void CancelTargeting()
    {
        pendingHandIndex = -1;
        pendingCard = null;
        DestroyRangeIndicator();
    }

    private void EnsureRangeIndicator()
    {
        if (rangeIndicator != null)
            return;

        rangeIndicator = new GameObject("RoguelikeMagicRangePreview");
        rangeIndicator.transform.SetParent(transform, false);
    }

    private void UpdateRangeIndicator()
    {
        if (!TryGetGroundPoint(out Vector3 groundPoint))
            return;

        UpdateRangeIndicator(groundPoint);
    }

    private void UpdateRangeIndicator(Vector3 groundPoint)
    {
        if (rangeIndicator == null || pendingCard == null)
            return;

        float radius = DefenseRoguelikeSkillCaster.ResolvePreviewRadius(pendingCard);
        var color = DefenseRoguelikeSkillCaster.ResolvePreviewColor(pendingCard);

        DefenseStrikeWarningZone.DestroyZone(rangeIndicator);
        rangeIndicator = DefenseStrikeWarningZone.CreateSustained(groundPoint, radius, color);
        rangeIndicator.name = "RoguelikeMagicRangePreview";
    }

    private void DestroyRangeIndicator()
    {
        DefenseStrikeWarningZone.DestroyZone(rangeIndicator);
        rangeIndicator = null;
    }

    private static bool TryGetGroundPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        var cam = Camera.main;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                500f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (!hit.collider.CompareTag("Ground")
            && !hit.collider.CompareTag("FarmSoil")
            && hit.collider.gameObject.name != "DefenseGround")
        {
            return false;
        }

        worldPoint = hit.point;
        return true;
    }

    private void OnDisable()
    {
        CancelTargeting();
    }
}
