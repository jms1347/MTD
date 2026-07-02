using UnityEngine;

public abstract class MonsterAttack : MonoBehaviour
{
    public virtual bool IsFinished => false;

    public abstract void Reset(Monster monster);
    public abstract void Tick(Monster monster);
    public virtual void Interrupt(Monster monster) { }
}
