using Unity.Netcode;
using UnityEngine;

public class StllEaHud : MonoBehaviour
{
    private StllBrotherhoodRoleState localRole;
    private StllPlayerGold localGold;
    private StllPlayerLoadout localLoadout;
    private StllPlayerHealth localHealth;
    private StllPlayerCardInventory localCards;
    private StllHubShopController shop;
    private GUIStyle titleStyle;
    private GUIStyle boxStyle;

    private void Update()
    {
        var player = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (player == null)
            return;

        localRole ??= player.GetComponent<StllBrotherhoodRoleState>();
        localGold ??= player.GetComponent<StllPlayerGold>();
        localLoadout ??= player.GetComponent<StllPlayerLoadout>();
        localHealth ??= player.GetComponent<StllPlayerHealth>();
        localCards ??= player.GetComponent<StllPlayerCardInventory>();
        shop ??= FindFirstObjectByType<StllHubShopController>();
    }

    private void OnGUI()
    {
        EnsureStyles();
        var run = StllRunController.Instance;
        var y = 10f;

        GUI.Box(new Rect(10f, y, 360f, 26f), "STLL EA · 삼국지 협동 로그라이크 (도형 프로토)", boxStyle);
        y += 32f;

        if (localRole != null && localRole.Role != StllBrotherhoodRole.None)
            GUI.Label(new Rect(10f, y, 500f, 22f), StllBrotherhoodRoleUtil.GetDisplayName(localRole.Role), titleStyle);
        y += 24f;

        if (run != null)
        {
            GUI.Label(new Rect(10f, y, 500f, 22f), $"페이즈: {GetPhaseLabel(run.Phase)}  {run.PhaseSecondsRemaining:0}s", titleStyle);
            y += 22f;

            if (run.Phase == StllRunPhase.StageSashuguan)
                GUI.Label(new Rect(10f, y, 500f, 22f), $"군량고 파괴: {run.DestroyedDepotCount}/2", titleStyle);
            y += 22f;
        }

        if (localHealth != null)
        {
            GUI.Label(new Rect(10f, y, 500f, 22f), $"HP {localHealth.CurrentHealth:0}/{localHealth.MaxHealth:0}", titleStyle);
            y += 22f;
        }

        if (localGold != null)
        {
            var team = StllTeamGold.Instance;
            GUI.Label(new Rect(10f, y, 500f, 22f),
                $"골드 {localGold.PersonalGold}  |  팀 {team?.Gold ?? 0}  |  무기 {localLoadout?.WeaponTier}  |  말 {localLoadout?.Horse}", titleStyle);
            y += 24f;
        }

        DrawHubShop();
        DrawCardPicker();
        DrawBossBar();
        DrawControls();
    }

    private void DrawHubShop()
    {
        if (StllRunController.Instance == null || StllRunController.Instance.Phase != StllRunPhase.Hub || shop == null)
            return;

        var x = Screen.width - 220f;
        var y = 80f;
        GUI.Box(new Rect(x - 10f, y - 10f, 210f, 200f), "군영 상점");

        if (GUI.Button(new Rect(x, y, 190f, 28f), $"무기 강화 ({StllEaConstants.WeaponUpgradeCostTier1}G)"))
            shop.BuyWeaponUpgradeServerRpc();
        y += 32f;
        if (GUI.Button(new Rect(x, y, 190f, 28f), $"빠른 말 ({StllEaConstants.HorseFastCost}G)"))
            shop.BuyFastHorseServerRpc();
        y += 32f;
        if (GUI.Button(new Rect(x, y, 190f, 28f), $"중장 말 ({StllEaConstants.HorseHeavyCost}G)"))
            shop.BuyHeavyHorseServerRpc();
        y += 32f;
        if (GUI.Button(new Rect(x, y, 190f, 28f), $"회복약 ({StllEaConstants.HealPotionCost}G)"))
            shop.BuyHealPotionServerRpc();
        y += 32f;
        if (GUI.Button(new Rect(x, y, 190f, 28f), $"연합 깃발 ({StllEaConstants.TeamBannerCost}G)"))
            shop.BuyTeamBannerServerRpc();
    }

    private void DrawCardPicker()
    {
        if (localCards == null || !localCards.IsPickActive)
            return;

        var w = 200f;
        var h = 100f;
        var startX = Screen.width * 0.5f - (w * 1.5f + 10f);
        var y = Screen.height * 0.5f;

        DrawCardButton(startX, y, w, h, localCards.PendingA, 1);
        DrawCardButton(startX + w + 10f, y, w, h, localCards.PendingB, 2);
        DrawCardButton(startX + (w + 10f) * 2f, y, w, h, localCards.PendingC, 3);
    }

    private void DrawCardButton(float x, float y, float w, float h, StllCardId id, int label)
    {
        var def = StllCardCatalog.Get(id);
        if (GUI.Button(new Rect(x, y, w, h), $"{label}. {def.Name}\n{def.Description}"))
            StllCardPickerController.Instance?.SelectCardServerRpc((byte)id);
    }

    private void DrawBossBar()
    {
        var boss = FindFirstObjectByType<StllBossLuBu>();
        if (boss == null || !boss.IsAlive || StllRunController.Instance?.Phase != StllRunPhase.StageHulao)
            return;

        var ratio = boss.MaxHealth > 0f ? boss.CurrentHealth / boss.MaxHealth : 0f;
        var barW = 400f;
        var x = Screen.width * 0.5f - barW * 0.5f;
        GUI.Box(new Rect(x, 40f, barW, 22f), $"여포 P{boss.Phase}  {boss.CurrentHealth:0}/{boss.MaxHealth:0}");
        GUI.Box(new Rect(x + 2f, 42f, (barW - 4f) * ratio, 18f), "");
    }

    private void DrawControls()
    {
        GUI.Label(new Rect(10f, Screen.height - 90f, 700f, 80f),
            "WASD 이동 | LMB 공격 | RMB 역할스킬 | Q 회전격 | Space 질주 | F 부하 | 1·2 카드\n" +
            "Host: H=허브 | K=사수관승리 | J=여포승리");
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 12, fontStyle = FontStyle.Bold };
    }

    private static string GetPhaseLabel(StllRunPhase phase)
    {
        return phase switch
        {
            StllRunPhase.BrotherhoodAssign => "도원결의",
            StllRunPhase.Hub => "군영 허브",
            StllRunPhase.StageSashuguan => "사수관",
            StllRunPhase.CardPick => "카드 선택",
            StllRunPhase.StageHulao => "호로관",
            StllRunPhase.RunComplete => "ACT1 클리어",
            StllRunPhase.RunFailed => "실패",
            _ => "?"
        };
    }
}
