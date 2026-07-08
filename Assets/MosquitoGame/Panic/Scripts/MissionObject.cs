using UnityEngine;

public class MissionObject : MonoBehaviour
{
    [SerializeField] private PanicMissionType missionType;
    [SerializeField] private string displayName = "미션";
    [SerializeField] private float holdRadius = 1.6f;

    private float holdProgress;

    public PanicMissionType MissionType => missionType;
    public float HoldProgress => holdProgress;
    public string DisplayName => displayName;

    private bool IsServer => Unity.Netcode.NetworkManager.Singleton == null || Unity.Netcode.NetworkManager.Singleton.IsServer;

    public bool IsHumanInRange(Vector3 position)
    {
        return Vector3.Distance(position, transform.position) <= holdRadius;
    }

    public void ReportHold(float deltaSeconds)
    {
        if (PanicGameManager.Instance == null || !PanicGameManager.Instance.IsPlay)
            return;

        if (holdProgress >= 1f)
            return;

        holdProgress += deltaSeconds / PanicGameConstants.MissionHoldSeconds;
        if (holdProgress < 1f)
            return;

        holdProgress = 1f;
        PanicGameManager.Instance.NotifyMissionCleared(missionType);
        if (missionType == PanicMissionType.TurnOffFan)
            FanGimmick.Instance?.SetFanEnabled(false);
    }

    public void ReportHoldDecay(float deltaSeconds)
    {
        if (holdProgress <= 0f)
            return;

        holdProgress = Mathf.Max(0f, holdProgress - deltaSeconds / PanicGameConstants.MissionHoldSeconds);
    }

    public static MissionObject Create(PanicMissionType type, string label, Vector3 position, Vector3 scale, Color color)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Mission_" + type;
        root.transform.position = position;
        root.transform.localScale = scale;
        PanicMaterialFactory.ApplyColor(root.GetComponent<Renderer>(), color);

        var trigger = root.GetComponent<BoxCollider>();
        trigger.isTrigger = true;

        var mission = root.AddComponent<MissionObject>();
        mission.missionType = type;
        mission.displayName = label;
        return mission;
    }
}
