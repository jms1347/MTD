using UnityEngine;

/// <summary>플레이 모드 안내 HUD.</summary>
public class MosquitoGameHudLabel : MonoBehaviour
{
    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(12f, 12f, 320f, 92f),
            "Mosquito vs Human\n" +
            "Tab: 인간 1인칭 / 모기 3인칭 전환\n" +
            "모기: Space 상승, Ctrl 하강\n" +
            "인간: WASD 이동, 마우스 시야",
            style);
    }
}
