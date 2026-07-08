public static class PanicGameConstants
{
    public const string SceneName = "MosquitoPanicPrototype";
    public const string HumanTag = "PanicHuman";
    public const string MosquitoTag = "PanicMosquito";
    public const string HumanTargetLayerName = PanicVisionLayers.HumanTargetLayerName;

    public const float PrepDurationSeconds = 70f;
    public const float MatchDurationSeconds = 300f;

    public const float HumanMaxHealth = 100f;
    public const float MosquitoGunDamage = 34f;
    public const float BloodSuckIntervalSeconds = 1f;
    public const float BloodSuckRange = 0.55f;

    public const int RpPerBloodTick = 10;
    public const int MosquitoTeamWinBonus = 500;
    public const int HumanHpWinMultiplier = 20;

    public const float HeartbeatRadius = 5f;
    public const float CoilRadius = 2.2f;
    public const float CoilSlowMultiplier = 0.3f;
    public const float CoilJitterStrength = 18f;
    public const float StickyStunSeconds = 3f;

    public const float MissionHoldSeconds = 2.5f;
    public const int RequiredMissionCount = 3;

    public const float MosquitoDashImpulse = 2.8f;
    public const float MosquitoHoverForce = 6.5f;
    public const float MosquitoMaxSpeed = 7.5f;
    public const float MosquitoVerticalForce = 4f;

    public const float HumanMoveSpeed = 4.2f;
    public const float HumanLookSensitivity = 0.18f;
    public const float MosquitoLookSensitivity = 0.22f;

    public const int MaxTrapsPerType = 4;
    public const ushort NetcodePort = 7780;
}

public enum PanicGamePhase : byte
{
    Lobby = 0,
    Prep = 1,
    Play = 2,
    Ended = 3
}

public enum PanicWinReason : byte
{
    None = 0,
    HumanMissionsComplete = 1,
    MosquitoesEliminated = 2,
    HumanEliminated = 3,
    TimeExpiredHumanAhead = 4
}

public enum PanicTrapType : byte
{
    MosquitoCoil = 0,
    StickyPad = 1,
    DecoyHuman = 2
}

public enum PanicMissionType : byte
{
    LaptopHomework = 0,
    TurnOffTabs = 1,
    TurnOffFan = 2
}
