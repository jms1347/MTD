using System;
using UnityEngine;

[Serializable]
public class RoguelikeOfferSettings
{
    [Tooltip("이 스테이지 수마다 카드 선택 UI를 띄웁니다.")]
    [Min(1)]
    public int stagesPerOffer = 5;

    [Tooltip("한 번에 보여줄 카드 선택지 수")]
    [Min(1)]
    public int choiceCount = 3;

    [Tooltip("마법 카드 최대 보유 수")]
    [Min(1)]
    public int maxMagicHandSize = 5;

    public bool ShouldOfferAfterStageClear(int clearedStage)
    {
        if (clearedStage <= 0)
            return false;

        return clearedStage % stagesPerOffer == 0;
    }
}
