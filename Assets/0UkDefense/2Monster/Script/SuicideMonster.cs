using UnityEngine;

public class SuicideMonster : MonsterAttack
{
    private bool hasExploded;
    private Transform attackTarget;

    public override bool IsFinished => hasExploded;

    public override void Reset(Monster monster)
    {
        hasExploded = false;
        attackTarget = null;
    }

    public override void Interrupt(Monster monster)
    {
    }

    public override void Tick(Monster monster)
    {
        if (hasExploded)
            return;

        attackTarget = Monster.FindAttackTarget(monster);
        if (attackTarget == null)
            return;

        if (TryExplodeOnNexusContact(monster, attackTarget))
            return;

        Vector3 toTarget = attackTarget.position - monster.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance < 0.05f)
            return;

        if (!monster.CanMove)
            return;

        Vector3 moveTarget = MonsterMovement.ResolveMoveTarget(monster, attackTarget);
        Vector3 direction = (moveTarget - monster.transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            direction.Normalize();
        else
            direction = toTarget / distance;

        monster.transform.position = monster.MoveAlongPath(moveTarget);
        monster.transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool TryExplodeOnNexusContact(Monster monster, Transform nexus)
    {
        if (nexus == null)
            return false;

        Vector3 toNexus = nexus.position - monster.transform.position;
        toNexus.y = 0f;
        if (toNexus.magnitude > monster.SuicideTouchRadius)
            return false;

        nexus.GetComponent<Health>()?.TakeDamage(monster.AttackDamage);
        monster.GetComponent<Health>()?.TakeDamage(9999f);
        hasExploded = true;
        return true;
    }
}
