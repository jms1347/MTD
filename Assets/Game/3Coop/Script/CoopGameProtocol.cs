using System;
using UnityEngine;

[Serializable]
public class CoopPlayerState
{
    public string playerId;
    public string playerName;
    public long gold;
    public float towerX;
    public float towerZ;
    public float towerHp;
    public float towerMaxHp;
    public float attack = 10f;
    public float fireInterval = 5f;
    public int penetration = 1;
    public int atkLevel;
    public int hpLevel;
    public int spdLevel;
    public int penLevel;
    public float fireCooldown;
    public int killStreak;
    public float lastKillTime;
    public string towerCode = "TANK-SCOUT";
    public int orderType;
    public float orderX;
    public float orderZ;
    public int attackTargetId = -1;
    public bool hasMoveTarget;
    public float moveTargetX;
    public float moveTargetZ;
    public string skillId = "";
    public float skillCooldown;
}

[Serializable]
public class CoopEnemyState
{
    public int id;
    public float x;
    public float z;
    public float hp;
    public float maxHp;
    public float speed;
    public int defense;
    public bool isBoss;
    public int goldReward;
    public string monsterCode;
}

[Serializable]
public class CoopSyncPayload
{
    public string type = CoopGameProtocol.StateSync;
    public float nexusHp;
    public float nexusMaxHp;
    public int wave;
    public bool waveActive;
    public bool goldRush;
    public bool farmGateOpen = true;
    public int aliveEnemies;
    public string announcement;
    public CoopPlayerState[] players;
    public CoopEnemyState[] enemies;
}

[Serializable]
public class CoopUpgradeRequest
{
    public string type = CoopGameProtocol.UpgradeRequest;
    public string playerId;
    public string upgradeKey;
}

[Serializable]
public class CoopMoveRequest
{
    public string type = CoopGameProtocol.MoveRequest;
    public string playerId;
    public float x;
    public float z;
}

[Serializable]
public class CoopOrderRequest
{
    public string type = CoopGameProtocol.OrderRequest;
    public string playerId;
    public int orderType;
    public float x;
    public float z;
    public int attackTargetId = -1;
}

[Serializable]
public class CoopSkillRequest
{
    public string type = CoopGameProtocol.SkillRequest;
    public string playerId;
    public float x;
    public float z;
}

[Serializable]
public class CoopEventPayload
{
    public string type;
    public string playerId;
    public string message;
    public long goldDelta;
    public int wave;
}

public static class CoopGameProtocol
{
    public const string StateSync = "coop_state_sync";
    public const string UpgradeRequest = "coop_upgrade_request";
    public const string MoveRequest = "coop_move_request";
    public const string OrderRequest = "coop_order_request";
    public const string SkillRequest = "coop_skill_request";
    public const string Event = "coop_event";
    public const string GameOver = "coop_game_over";

    public const string UpgradeAttack = "attack";
    public const string UpgradeHealth = "health";
    public const string UpgradeSpeed = "speed";
    public const string UpgradePenetration = "penetration";

    public const float BaseAttack = 10f;
    public const float BaseHealth = 100f;
    public const float BaseFireInterval = 5f;
    public const int BasePenetration = 1;
    public const float NexusMaxHealth = 500f;
    public const float PlayerMoveSpeed = 5f;
    public const float TowerBodyRadius = 0.55f;

    public const int OrderNone = 0;
    public const int OrderMove = 1;
    public const int OrderAttackMove = 2;
    public const int OrderAttackTarget = 3;

    public static readonly string[] EnemyVisualTypes =
    {
        "grunt", "runner", "brute", "stalker", "swarm"
    };
}

public static class CoopUpgradeRules
{
    public static long GetCost(string upgradeKey, int currentLevel)
    {
        return upgradeKey switch
        {
            CoopGameProtocol.UpgradeAttack => 30 + currentLevel * 20,
            CoopGameProtocol.UpgradeHealth => 25 + currentLevel * 15,
            CoopGameProtocol.UpgradeSpeed => 35 + currentLevel * 25,
            CoopGameProtocol.UpgradePenetration => 50 + currentLevel * 30,
            _ => 9999
        };
    }

    public static void Apply(CoopPlayerState player, string upgradeKey)
    {
        switch (upgradeKey)
        {
            case CoopGameProtocol.UpgradeAttack:
                player.atkLevel++;
                player.attack = CoopGameProtocol.BaseAttack + player.atkLevel * 5f;
                break;
            case CoopGameProtocol.UpgradeHealth:
                player.hpLevel++;
                var hpGain = 25f;
                player.towerMaxHp = CoopGameProtocol.BaseHealth + player.hpLevel * 25f;
                player.towerHp = Mathf.Min(player.towerHp + hpGain, player.towerMaxHp);
                break;
            case CoopGameProtocol.UpgradeSpeed:
                player.spdLevel++;
                player.fireInterval = Mathf.Max(0.6f, CoopGameProtocol.BaseFireInterval - player.spdLevel * 0.35f);
                break;
            case CoopGameProtocol.UpgradePenetration:
                player.penLevel++;
                player.penetration = CoopGameProtocol.BasePenetration + player.penLevel;
                break;
        }
    }
}
