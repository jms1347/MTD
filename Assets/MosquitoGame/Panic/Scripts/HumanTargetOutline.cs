using UnityEngine;

/// <summary>
/// 레거시 래퍼. HumanVisualBuilder의 Outline을 사용한다.
/// </summary>
public class HumanTargetOutline : MonoBehaviour
{
    private HumanVisualBuilder visual;

    public Transform OutlineRoot => visual != null ? visual.OutlineRoot : null;

    public void Build()
    {
        if (visual == null)
            visual = GetComponent<HumanVisualBuilder>() ?? gameObject.AddComponent<HumanVisualBuilder>();

        visual.Build();
    }

    public void SetVisible(bool visible)
    {
        Build();
        visual?.SetOutlineVisible(visible);
    }
}
