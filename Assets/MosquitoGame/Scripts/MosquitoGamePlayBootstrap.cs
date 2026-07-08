using UnityEngine;

/// <summary>인간 1인칭 / 모기 3인칭 테스트 부트스트랩.</summary>
public class MosquitoGamePlayBootstrap : MonoBehaviour
{
    [SerializeField] private MosquitoGameHumanController human;
    [SerializeField] private MosquitoGameMosquitoController mosquito;

    private void Awake()
    {
        if (human == null || mosquito == null)
            return;

        SetActiveRole(MosquitoGameRole.Human);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var next = human.IsActiveRole ? MosquitoGameRole.Mosquito : MosquitoGameRole.Human;
            SetActiveRole(next);
        }
    }

    public void SetActiveRole(MosquitoGameRole role)
    {
        var isHuman = role == MosquitoGameRole.Human;
        human.SetActiveRole(isHuman);
        mosquito.SetActiveRole(!isHuman);
    }
}

public enum MosquitoGameRole
{
    Human,
    Mosquito
}
