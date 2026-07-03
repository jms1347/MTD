using UnityEngine;

public interface ICwslPooledNetworkObject
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}
