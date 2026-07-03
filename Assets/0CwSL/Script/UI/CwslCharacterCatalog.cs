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
            "Q/Space 홀드 + 골드 1 이상 — 쉴드 켜지면 무적, 피격 시 골드 1 소모 / 시야 좁음",
            "우클릭 이동 | 좌클릭 선택 | A 공격 | Q/Space 방패(홀드) | G 골드 선물/부활",
            14f),
        new(
            CwslCharacterId.MissileTank,
            "미사일 탱크",
            "Q — 로켓 발사 (미사일 1발당 골드 1) / 시야 넓음",
            "우클릭 이동 | 좌클릭 선택 | A 공격 | Q 미사일(골드1) | G 골드 선물/부활",
            22f)
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

    public static float GetVisionRadius(CwslCharacterId id) => Get(id).VisionRadius;
}
