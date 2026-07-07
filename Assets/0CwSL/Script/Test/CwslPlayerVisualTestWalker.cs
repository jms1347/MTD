using UnityEngine;

/// <summary>캐릭터 비주얼 테스트 — WASD 이동.</summary>
public class CwslPlayerVisualTestWalker : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float turnSpeed = 12f;

    private bool active = true;

    public bool IsWalking { get; private set; }

    public void SetActive(bool enabled)
    {
        active = enabled;
        if (!active)
            IsWalking = false;
    }

    private void Update()
    {
        if (!active)
            return;

        var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude < 0.0001f)
        {
            IsWalking = false;
            return;
        }

        input.Normalize();
        IsWalking = true;
        transform.position += input * (moveSpeed * Time.deltaTime);

        if (input.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }
    }
}
