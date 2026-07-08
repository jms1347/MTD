using UnityEngine;

public class PanicHudController : MonoBehaviour
{
    private void OnGUI()
    {
        var manager = PanicGameManager.Instance;
        var score = ScoreManager.Instance;
        if (manager == null)
            return;

        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };
        boxStyle.normal.textColor = Color.white;

        var phaseText = manager.Phase switch
        {
            PanicGamePhase.Prep => $"준비 {manager.PhaseTimer:0}s — 함정 설치 (1/2/3, E)",
            PanicGamePhase.Play => $"플레이 {manager.PhaseTimer:0}s — 미션 {manager.ClearedMissionCount}/{PanicGameConstants.RequiredMissionCount}",
            PanicGamePhase.Ended => manager.HumanWon ? "인간 승리" : "모기 승리",
            _ => "로비"
        };

        var rpText = score != null
            ? $"인간 RP {score.HumanRp} | 모기팀 RP {score.MosquitoTeamRp}"
            : "RP -";

        GUI.Box(new Rect(12f, 12f, 420f, 92f), phaseText + "\n" + rpText, boxStyle);

        if (manager.IsEnded)
        {
            var endStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            endStyle.normal.textColor = manager.HumanWon ? Color.green : Color.red;
            GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, 48f),
                manager.HumanWon ? "HUMAN WIN" : "MOSQUITO WIN",
                endStyle);
        }
    }
}
