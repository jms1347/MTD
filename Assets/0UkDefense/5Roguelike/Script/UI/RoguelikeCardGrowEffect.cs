using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드 호버·선택 시 부드럽게 커지는 CardGrow 연출.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RoguelikeCardGrowEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.14f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float growSpeed = 10f;

    private RectTransform rectTransform;
    private float targetScale = 1f;
    private bool isSelected;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetScale = normalScale;
        rectTransform.localScale = Vector3.one * normalScale;
    }

    private void Update()
    {
        float current = rectTransform.localScale.x;
        float next = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * growSpeed);
        rectTransform.localScale = Vector3.one * next;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        targetScale = selected ? selectedScale : normalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected)
            return;

        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected)
            return;

        targetScale = normalScale;
    }
}
