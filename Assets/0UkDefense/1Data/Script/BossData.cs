using System;

[Serializable]
public class BossData
{
    public string bossCode;
    public string monsterCode;
    public string immunityGroupCode;
    public string weaknessGroupCode;
    public int hp;
    public int attack;
    public int defense;
    /// <summary>초당 공격 횟수. 시트의 n초당 1공격 값은 import 시 1/n 으로 변환.</summary>
    public float attackSpeed;
}
