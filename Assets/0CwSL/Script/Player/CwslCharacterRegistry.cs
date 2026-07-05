using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>캐릭터 점유 조회 (NetworkVariable 동기화 기준).</summary>
public static class CwslCharacterRegistry
{
    public static bool IsTakenByOther(CwslCharacterId characterId, ulong ownerClientId)
    {
        foreach (var character in UnityEngine.Object.FindObjectsByType<CwslPlayerCharacter>(FindObjectsSortMode.None))
        {
            if (character == null || !character.IsSpawned)
                continue;

            if (character.OwnerClientId == ownerClientId)
                continue;

            if (character.CharacterId == characterId)
                return true;
        }

        return false;
    }

    public static CwslCharacterId? GetNextAvailable(CwslCharacterId current, ulong ownerClientId)
    {
        var values = (CwslCharacterId[])Enum.GetValues(typeof(CwslCharacterId));
        var index = Array.IndexOf(values, current);
        if (index < 0)
            index = 0;

        for (var step = 1; step <= values.Length; step++)
        {
            var candidate = values[(index + step) % values.Length];
            if (!IsTakenByOther(candidate, ownerClientId))
                return candidate;
        }

        return null;
    }
}
