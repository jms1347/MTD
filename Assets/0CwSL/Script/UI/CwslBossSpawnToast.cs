public static class CwslBossSpawnToast
{
    public static void Show()
    {
        CwslSkillGoldFeedback.ShowMessage("홍명보 보스 등장! 맵 중앙의 거대 적을 공격하세요.", 4f);
    }

    public static void ShowSkill(string skillName)
    {
        CwslSkillGoldFeedback.ShowMessage($"홍명보: {skillName}", 2.5f);
    }
}
