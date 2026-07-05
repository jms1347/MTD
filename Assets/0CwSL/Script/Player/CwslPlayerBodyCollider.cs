using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 캐릭터 비주얼에 맞춘 몸통 캡슐 + NavMeshAgent 크기. 방패 버블은 별도(SphereCollider).
/// </summary>
public class CwslPlayerBodyCollider : NetworkBehaviour
{
    private CapsuleCollider bodyCollider;
    private NavMeshAgent agent;
    private CwslPlayerCharacter playerCharacter;

    public float Radius { get; private set; }
    public float Height { get; private set; }
    public float CenterY { get; private set; }

    public override void OnNetworkSpawn()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
        agent = GetComponent<NavMeshAgent>();
        playerCharacter = GetComponent<CwslPlayerCharacter>();

        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged += HandleCharacterChanged;

        ApplyForCurrentCharacter();
    }

    public override void OnNetworkDespawn()
    {
        if (playerCharacter != null)
            playerCharacter.OnCharacterChanged -= HandleCharacterChanged;
    }

    private void HandleCharacterChanged(CwslCharacterId characterId)
    {
        ApplyForCharacter(characterId);
    }

    public void ApplyForCurrentCharacter()
    {
        var characterId = playerCharacter != null
            ? playerCharacter.CharacterId
            : CwslCharacterId.Tank;
        ApplyForCharacter(characterId);
    }

    public void ApplyForCharacter(CwslCharacterId characterId)
    {
        ResolveSize(characterId, out var radius, out var height, out var centerY);
        Radius = radius;
        Height = height;
        CenterY = centerY;

        if (bodyCollider != null)
        {
            bodyCollider.radius = radius;
            bodyCollider.height = height;
            bodyCollider.center = new Vector3(0f, centerY, 0f);
        }

        if (agent != null)
        {
            agent.radius = radius;
            agent.height = height;
            agent.baseOffset = centerY;
        }
    }

    public static void ResolveSize(CwslCharacterId characterId, out float radius, out float height, out float centerY)
    {
        switch (characterId)
        {
            case CwslCharacterId.MomentumRammer:
                radius = 0.40f;
                height = 1.38f;
                centerY = 0.69f;
                break;
            case CwslCharacterId.MissileTank:
                radius = 0.30f;
                height = 1.70f;
                centerY = 0.85f;
                break;
            case CwslCharacterId.RedMage:
                radius = 0.28f;
                height = 1.62f;
                centerY = 0.81f;
                break;
            default:
                radius = 0.32f;
                height = 1.74f;
                centerY = 0.87f;
                break;
        }
    }

    public static float ResolveDefaultRadius(CwslCharacterId characterId)
    {
        ResolveSize(characterId, out var radius, out _, out _);
        return radius;
    }
}
