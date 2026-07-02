using UnityEngine;

public struct DamageInfo
{
    public float amount;
    public DamageElement element;
    public Vector3 hitPoint;

    public static DamageInfo Physical(float amount, Vector3 hitPoint)
    {
        return new DamageInfo
        {
            amount = amount,
            element = DamageElement.Physical,
            hitPoint = hitPoint
        };
    }

    public static DamageInfo Projectile(float amount, DamageElement element, Vector3 hitPoint)
    {
        return new DamageInfo
        {
            amount = amount,
            element = element,
            hitPoint = hitPoint
        };
    }

    public static DamageInfo AoE(float amount, DamageElement element, Vector3 hitPoint)
    {
        return new DamageInfo
        {
            amount = amount,
            element = element,
            hitPoint = hitPoint
        };
    }
}
