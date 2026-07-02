/// <summary>
/// linked 스킬 행동 프리팹 루트에 붙는 컴포넌트.
/// LinkedSkillSpawner가 Instantiate 후 Initialize를 호출합니다.
/// 구현 시 <see cref="LinkedSkillSpawnContext"/>의 Resolve* 메서드로
/// 미사일 스킬(속성·배율·이펙트)과 타워 수치(공격력·공속·사거리)를 참조하세요.
/// </summary>
public interface ILinkedSkillSpawn
{
    void Initialize(LinkedSkillSpawnContext context);
}
