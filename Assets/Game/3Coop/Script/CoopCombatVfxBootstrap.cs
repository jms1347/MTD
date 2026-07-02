using UnityEngine;

[DefaultExecutionOrder(-280)]
public class CoopCombatVfxBootstrap : MonoBehaviour
{
    private void Awake()
    {
        CoopCombatVfxCache.EnsureInitialized();
    }
}
