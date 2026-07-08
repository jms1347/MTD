using UnityEngine;

/// <summary>모기가 숨을 수 있는 틈 — 에디터에서 반투명 박스로 표시.</summary>
public class MosquitoGameHideZone : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new(0.2f, 0.85f, 0.45f, 0.25f);

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
