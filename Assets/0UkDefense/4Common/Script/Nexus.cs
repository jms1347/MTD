using UnityEngine;

public class Nexus : MonoBehaviour
{
    public static Transform Target { get; private set; }

    private void OnEnable()
    {
        Target = transform;
    }

    private void OnDisable()
    {
        if (Target == transform)
            Target = null;
    }
}
