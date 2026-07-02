/// <summary>
/// 몬스터에게 동시에 붙을 수 있는 상태. VFX·행동 제어·반응(추후)의 기준 ID.
/// </summary>
public enum MonsterStatus
{
  None = 0,
  /// <summary>이동불가 (빙결). 공격은 가능.</summary>
  Frozen,
  /// <summary>행동불가 (감전). 이동·공격 불가 + 떨림.</summary>
  Shocked,
  Wet,
  Burning,
  /// <summary>불붙음 (장판/그라운드 계열).</summary>
  Ablaze,
  Poisoned,
  /// <summary>약전기 (찌지직 VFX·DoT, 행동 제한 없음). 행동불가는 Shocked.</summary>
  Electrified,
  Slowed
}
