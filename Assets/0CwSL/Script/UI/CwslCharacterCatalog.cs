using System.Collections.Generic;

public static class CwslCharacterCatalog
{
    public readonly struct Entry
    {
        public readonly CwslCharacterId Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly string ControlHint;
        public readonly float VisionRadius;

        public Entry(
            CwslCharacterId id,
            string displayName,
            string description,
            string controlHint,
            float visionRadius)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ControlHint = controlHint;
            VisionRadius = visionRadius;
        }
    }

    private static readonly Entry[] Entries =
    {
        new(
            CwslCharacterId.Tank,
            "방패 탱커",
            "높은 체력과 방어력으로 전선을 유지합니다. 몬스터의 압박을 막고, 스턴·넉백으로 아군을 보호하는 역할입니다.",
            "우클릭 이동 · 좌클릭 적 선택 · A+클릭 공격 이동",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.MissileTank,
            "총잡이",
            "원거리 화력 딜러입니다. 안전한 거리에서 몬스터와 넥서스를 압박하는 적을 처리합니다.",
            "우클릭 이동 · 좌클릭 적 선택 · A+클릭 공격 이동",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.RedMage,
            "빨간 마법사",
            "광역 마법으로 몬스터 무리를 정리합니다. 메테오·구체·순간이동으로 순간 폭딜을 넣습니다.",
            "우클릭 이동 · 좌클릭 적 선택 · Q 스킬은 지면 클릭",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.MomentumRammer,
            "질주자",
            "속도를 모아 돌진하며 충돌 피해를 줍니다. 기동성이 높아 견제와 견인에 특화되어 있습니다.",
            "우클릭 홀드 조향 · 좌클릭 적 선택 · Q/Space 홀드 스킬",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.CrowdGatherer,
            "끌모",
            "몬스터를 끌어모아 아군 딜 타이밍을 만듭니다. Q 블랙홀·W 밧줄 수렴·E 영역 교환·R 회오리로 전장을 교란합니다.",
            "우클릭 이동 · 좌클릭 적 선택 · Q/Space 홀드 스킬",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.Barricade,
            "바리케이드",
            "벽과 발판으로 길을 통제하고 넥서스를 수리합니다. 넥서스 러시를 막는 방어형입니다.",
            "우클릭 이동 · Q는 드래그로 벽 설치",
            CwslGameConstants.PlayerVisionRadius),
        new(
            CwslCharacterId.Healer,
            "힐러",
            "아군 회복과 버프, 독 장판으로 전장을 지원합니다. 지속 전투에서 팀 생존력을 높입니다.",
            "우클릭 이동 · 좌클릭 적 선택 · Q/E/R/W 장판·스킬",
            CwslGameConstants.PlayerVisionRadius)
    };

    public static IReadOnlyList<Entry> All => Entries;

    public static Entry Get(CwslCharacterId id)
    {
        foreach (var entry in Entries)
        {
            if (entry.Id == id)
                return entry;
        }

        return Entries[0];
    }

    public static float GetVisionRadius(CwslCharacterId id) => CwslGameConstants.PlayerVisionRadius;
}
