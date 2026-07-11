public static class StllGlaiveConstants
{
    public const float SteerLeadDistance = 20f;

    // --- 말 물리: CwslGameConstants Rammer 값 사용 (StllHorseMotor) ---
    public const float HorseMaxSpeed = CwslGameConstants.RammerMaxSpeed;
    public const float HorseBrakeDecelPerSecond = 18f;
    public const float HorseBrakeMinSpeed = 4.8f;
    public const float HorseOpposingDotThreshold = 0f;
    public const float HorseSteerAccelDotThreshold = 0.45f;
    public const float HorseTurnDecelFactor = 0.65f;
    public const float HorseSteerTurnRateMultiplier = 0.58f;
    public const float HorseVelocityBlendLow = 4.2f;
    public const float HorseVelocityBlendHigh = 13f;
    public const float HorseDriftAtLowSpeed = 0.18f;
    public const float HorseDriftAtHighSpeed = 0.82f;
    public const float HorseBodyTurnRateMultiplier = 0.72f;
    public const float HorsePivotTurnRate = 480f;
    public const float HorsePivotAlignDot = 0.86f;
    public const float HorsePivotVelocityBlend = 16f;

    // --- 마상 질주 (Space) ---
    public const float ChargeDuration = 0.5f;
    public const float ChargeSpeed = 42f;
    public const float ChargeKnockbackDistance = 4.5f;
    public const float ChargeKnockbackRadius = 1.35f;
    public const int ChargeStaminaCost = 1;

    // --- 평타 Fan_90 ---
    public const float BasicAttackCooldown = 0.38f;
    public const float BasicAttackRange = 3.2f;
    public const float BasicAttackHalfAngleDeg = 45f;
    public const float BasicAttackDamage = 28f;
    public const float BasicAttackKnockback = 1.8f;

    // --- 질주대회전 (RMB) ---
    public const float ChargeSpinCooldown = 9f;
    public const float ChargeSpinDuration = 0.85f;
    public const float ChargeSpinRadius = 4.8f;
    public const float ChargeSpinDamagePerHit = 52f;
    public const float ChargeSpinKnockback = 5.5f;
    public const int ChargeSpinSwings = 2;

    // --- 청룡검기 ---
    public const float QinglongProcChance = 0.15f;
    public const float QinglongProjectileSpeed = 34f;
    public const float QinglongProjectileRange = 22f;
    public const float QinglongProjectileDamage = 36f;
    public const float QinglongProjectileWidth = 1.6f;

    // --- 장수의 위엄 ---
    public const float CommanderAuraRadius = 8f;
    public const float CommanderAuraAttackBonus = 0.15f;

    // --- 부하 ---
    public const int DefaultMinionCount = 4;
    public const float MinionFollowDistance = 2.2f;
    public const float MinionAttackRange = 2.4f;
    public const float MinionBaseDamage = 12f;
    public const float MinionMoveSpeed = 5.5f;

    // --- 스태미나 ---
    public const float MaxStamina = 5f;
    public const float StaminaRegenPerSecond = 0.35f;
}
