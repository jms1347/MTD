using UnityEngine;

public class HumanHeartbeatRadar : MonoBehaviour
{
    [SerializeField] private HumanController human;
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioSource mosquitoWingSource;

    public void Bind(HumanController target) => human = target;

    private float pulse;

    private void Update()
    {
        if (human == null || !human.IsLocalOwner)
            return;

        var nearestDistance = FindNearestMosquitoDistance();
        var intensity = 0f;
        if (nearestDistance < PanicGameConstants.HeartbeatRadius)
        {
            var t = 1f - nearestDistance / PanicGameConstants.HeartbeatRadius;
            intensity = t * t;
        }

        pulse += Time.deltaTime * Mathf.Lerp(1.5f, 7f, intensity);
        human.SetHeartbeatIntensity(intensity, Mathf.Sin(pulse));

        if (heartbeatSource != null)
            heartbeatSource.volume = Mathf.Lerp(0f, 1f, intensity);

        if (mosquitoWingSource != null)
            mosquitoWingSource.volume = FanGimmick.IsMaskingMosquitoAudio ? 0.08f : Mathf.Lerp(0f, 0.9f, intensity);
    }

    private float FindNearestMosquitoDistance()
    {
        var best = float.MaxValue;
        var mosquitoes = FindObjectsByType<MosquitoController>(FindObjectsSortMode.None);
        foreach (var mosquito in mosquitoes)
        {
            if (mosquito == null || !mosquito.IsAlive)
                continue;

            var distance = Vector3.Distance(human.transform.position, mosquito.transform.position);
            if (distance < best)
                best = distance;
        }

        return best;
    }
}
