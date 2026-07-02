using System.Collections;
using UnityEngine;

public enum CoopSkillZoneMode
{
    Lightning,
    Blizzard,
    Meteor
}

public class CoopSkillZone : MonoBehaviour
{
    private CoopGameSession session;
    private string attackerPlayerId;
    private Vector3 anchor;
    private float radius;
    private float tickDamage;
    private int penetration;
    private float endTime;
    private float tickInterval;
    private CoopSkillZoneMode mode;
    private GameObject rangeIndicator;

    public static CoopSkillZone Spawn(
        CoopGameSession gameSession,
        string playerId,
        Vector3 groundPoint,
        CoopSkillZoneMode zoneMode,
        float damage,
        int pen,
        float duration,
        float radiusValue,
        float tickSeconds)
    {
        var zoneObject = new GameObject($"CoopSkillZone_{zoneMode}");
        var zone = zoneObject.AddComponent<CoopSkillZone>();
        zone.Initialize(gameSession, playerId, groundPoint, zoneMode, damage, pen, duration, radiusValue, tickSeconds);
        return zone;
    }

    private void Initialize(
        CoopGameSession gameSession,
        string playerId,
        Vector3 groundPoint,
        CoopSkillZoneMode zoneMode,
        float damage,
        int pen,
        float duration,
        float radiusValue,
        float tickSeconds)
    {
        session = gameSession;
        attackerPlayerId = playerId;
        mode = zoneMode;
        tickDamage = damage;
        penetration = pen;
        radius = radiusValue;
        tickInterval = tickSeconds;
        anchor = new Vector3(groundPoint.x, 0f, groundPoint.z);
        endTime = Time.time + duration;
        transform.position = anchor;

        rangeIndicator = DefenseStrikeWarningZone.CreateSustained(
            anchor,
            radius,
            ResolveZoneColor(zoneMode),
            transform);

        StartCoroutine(RunTicks());
    }

    private IEnumerator RunTicks()
    {
        while (Time.time < endTime)
        {
            if (mode == CoopSkillZoneMode.Meteor)
                SpawnMeteorDrop();

            CoopSkillCombat.DamageEnemiesInRadius(session, anchor, radius, tickDamage, penetration, attackerPlayerId);

            if (mode == CoopSkillZoneMode.Lightning)
                PlayLightningFx();

            yield return new WaitForSeconds(tickInterval);
        }

        Destroy(gameObject);
    }

    private void SpawnMeteorDrop()
    {
        var offset = Random.insideUnitCircle * radius * 0.75f;
        var strikePoint = anchor + new Vector3(offset.x, 0f, offset.y);
        CoopSkillCombat.SpawnFallingMeteor(strikePoint, tickDamage * 1.4f, penetration, attackerPlayerId, session);
    }

    private void PlayLightningFx()
    {
        if (!CoopSkillCombat.TryFindNearestEnemy(anchor, radius, out var enemy))
            return;

        var hit = enemy.transform.position;
        var origin = hit + Vector3.up * 3.2f;
        DefenseStormStrikeLogic.PlayLightningStrike(origin, hit);
    }

    private static Color ResolveZoneColor(CoopSkillZoneMode zoneMode)
    {
        return zoneMode switch
        {
            CoopSkillZoneMode.Blizzard => DefenseStrikeWarningZone.BlizzardZoneColor,
            CoopSkillZoneMode.Meteor => new Color(1f, 0.2f, 0.08f, 0.42f),
            _ => DefenseStrikeWarningZone.StormStrikeZoneColor
        };
    }

    private void OnDestroy()
    {
        DefenseStrikeWarningZone.DestroyZone(rangeIndicator);
        rangeIndicator = null;
    }
}
