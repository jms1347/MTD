using System.Collections;
using UnityEngine;

/// <summary>라이트닝 오브 → 대상 번개 투사체 연출.</summary>
public class CwslLightningMissileRunner : MonoBehaviour
{
    private const float HitDistance = 0.35f;

    public void Play(Vector3 origin, Vector3 target, float speed)
    {
        Play(origin, target, speed, null, null);
    }

    public void Play(
        Vector3 origin,
        Vector3 target,
        float speed,
        GameObject missilePrefab,
        GameObject strikePrefab,
        bool randomRedMageStrike = false)
    {
        StartCoroutine(Run(origin, target, Mathf.Max(8f, speed), missilePrefab, strikePrefab, randomRedMageStrike));
    }

    private IEnumerator Run(
        Vector3 origin,
        Vector3 target,
        float speed,
        GameObject missilePrefab,
        GameObject strikePrefab,
        bool randomRedMageStrike)
    {
        var missile = SpawnMissile(origin, target, missilePrefab);
        if (missile == null)
        {
            SpawnStrike(target, strikePrefab, randomRedMageStrike);
            Destroy(gameObject);
            yield break;
        }

        var flatTarget = target;
        flatTarget.y = origin.y;
        var direction = flatTarget - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;
        direction.Normalize();

        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        missile.transform.rotation = rotation;

        while (missile != null && Vector3.Distance(missile.transform.position, flatTarget) > HitDistance)
        {
            missile.transform.position += direction * (speed * Time.deltaTime);
            missile.transform.rotation = rotation;
            yield return null;
        }

        if (missile != null)
            CwslVfxPool.Release(missile);

        SpawnStrike(target, strikePrefab, randomRedMageStrike);
        Destroy(gameObject);
    }

    private static GameObject SpawnMissile(Vector3 origin, Vector3 target, GameObject missilePrefab)
    {
        if (missilePrefab != null)
            return CwslVfxSpawner.SpawnMissile(missilePrefab, origin, target, 0f, 0.95f);

        return CwslVfxSpawner.SpawnLightningMissile(origin, target);
    }

    private static void SpawnStrike(Vector3 target, GameObject strikePrefab, bool randomRedMageStrike)
    {
        if (randomRedMageStrike)
        {
            CwslVfxSpawner.SpawnRedMageLightningStrike(target);
            return;
        }

        if (strikePrefab != null)
        {
            CwslVfxSpawner.Spawn(
                strikePrefab,
                target + Vector3.up * 0.08f,
                Quaternion.identity,
                1.1f,
                1f);
            return;
        }

        CwslVfxSpawner.SpawnLightningStrike(target);
    }
}
