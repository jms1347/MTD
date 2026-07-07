using UnityEngine;

/// <summary>먹물 문어 — 촉수 살짝 흔들림.</summary>
public class CwslInkOctopusTentacleVisual : MonoBehaviour
{
    private Transform[] tentacles;
    private Quaternion[] baseRotations;
    private float phase;

    private void Awake()
    {
        var visual = transform;
        var list = new System.Collections.Generic.List<Transform>();
        for (var i = 0; i < visual.childCount; i++)
        {
            var child = visual.GetChild(i);
            if (child.name.StartsWith("Tentacle_"))
                list.Add(child);
        }

        tentacles = list.ToArray();
        baseRotations = new Quaternion[tentacles.Length];
        for (var i = 0; i < tentacles.Length; i++)
            baseRotations[i] = tentacles[i].localRotation;
    }

    private void Update()
    {
        if (tentacles == null || tentacles.Length == 0)
            return;

        phase += Time.deltaTime * 2.4f;
        for (var i = 0; i < tentacles.Length; i++)
        {
            var tentacle = tentacles[i];
            if (tentacle == null)
                continue;

            var wobble = Mathf.Sin(phase + i * 0.7f) * 8f;
            tentacle.localRotation = baseRotations[i] * Quaternion.Euler(wobble, 0f, 0f);
        }
    }
}
