using System.Collections;
using UnityEngine;

/// <summary>라이트닝 오브 → 플레이어 LightningMissilePink 연출.</summary>
public class CwslLightningMissileRunner : MonoBehaviour
{
    private const float HitDistance = 0.35f;

    public void Play(Vector3 origin, Vector3 target, float speed)
    {
        StartCoroutine(Run(origin, target, Mathf.Max(8f, speed)));
    }

    private IEnumerator Run(Vector3 origin, Vector3 target, float speed)
    {
        var missile = CwslVfxSpawner.SpawnLightningMissile(origin, target);
        if (missile == null)
        {
            CwslVfxSpawner.SpawnLightningStrike(target);
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
            Destroy(missile);

        CwslVfxSpawner.SpawnLightningStrike(target);
        Destroy(gameObject);
    }
}
