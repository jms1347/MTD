using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰된 인간을 로컬에 등록. NetworkObject.OnNetworkSpawn이 모든 클라이언트에서 호출되므로
/// ClientRpc 없이 모기 트래커가 Transform(NetworkTransform 동기화)을 조회할 수 있다.
/// </summary>
public class HumanTargetRegistry : MonoBehaviour
{
    public static HumanTargetRegistry Instance { get; private set; }

    private readonly List<HumanController> humans = new();

    public event Action<HumanController> OnHumanRegistered;
    public event Action<HumanController> OnHumanUnregistered;

    public IReadOnlyList<HumanController> Humans => humans;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterHuman(HumanController human)
    {
        if (human == null || humans.Contains(human))
            return;

        humans.Add(human);
        human.EnsureMosquitoVisibleOutline();
        OnHumanRegistered?.Invoke(human);
    }

    public void UnregisterHuman(HumanController human)
    {
        if (human == null || !humans.Remove(human))
            return;

        OnHumanUnregistered?.Invoke(human);
    }

    public HumanController GetPrimaryHuman()
    {
        for (var i = 0; i < humans.Count; i++)
        {
            var human = humans[i];
            if (human != null && human.IsAlive)
                return human;
        }

        return null;
    }

    public void GetAliveHumans(List<HumanController> buffer)
    {
        buffer.Clear();
        for (var i = 0; i < humans.Count; i++)
        {
            var human = humans[i];
            if (human != null && human.IsAlive)
                buffer.Add(human);
        }
    }
}
