using UnityEngine;

/// <summary>몬스터 비주얼 테스트 씬 — 원형 패트롤로 다리 걷기 연출.</summary>
public class CwslMonsterVisualTestWalker : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float patrolRadius = 2.8f;

    private Vector3 origin;
    private float angle;
    private bool active;

    public bool IsWalking => active;

    public void Begin(Vector3 center)
    {
        origin = center;
        angle = 0f;
        active = true;
        enabled = true;
    }

    public void Stop()
    {
        active = false;
        enabled = false;
    }

    private void Update()
    {
        if (!active)
            return;

        angle += moveSpeed * Time.deltaTime;
        var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * patrolRadius;
        var next = origin + offset;
        var flat = next - transform.position;
        flat.y = 0f;
        transform.position = next;

        if (flat.sqrMagnitude > 0.0004f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 12f);
    }
}
