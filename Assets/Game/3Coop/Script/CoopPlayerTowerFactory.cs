using UnityEngine;

public static class CoopPlayerTowerFactory
{
    public static CoopPlayerTowerUnit CreatePlayerTank(Transform parent, CoopPlayerState state, int colorIndex)
    {
        if (!CoopTankCatalog.TryGet(state.towerCode, out var tank))
            tank = CoopTankCatalog.GetRandom(new System.Random(colorIndex + 7));

        var position = new Vector3(state.towerX, 0.12f, state.towerZ);
        var tankObject = new GameObject($"CoopTank_{state.playerName}_{tank.DisplayName}");
        tankObject.tag = "Tower";
        tankObject.transform.SetParent(parent, false);
        tankObject.transform.position = position;

        var visuals = CoopTankVisualFactory.Build(tankObject.transform, tank);

        var health = tankObject.AddComponent<Health>();
        health.Initialize(state.towerMaxHp, 0f, 0f);

        var unit = tankObject.AddComponent<CoopPlayerTowerUnit>();
        unit.Initialize(state.playerId, state.playerName, tank, health, visuals.Hull, visuals.Turret, visuals.FirePoint);

        var attack = tankObject.AddComponent<CoopTankAttack>();
        attack.Initialize(unit, visuals.FirePoint);

        return unit;
    }
}
