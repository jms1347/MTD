using UnityEngine;

public class FanGimmick : MonoBehaviour
{
    public static FanGimmick Instance { get; private set; }
    public static bool IsMaskingMosquitoAudio { get; private set; }

    private bool fanEnabled = true;
    private AudioSource loopSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
        loopSource.minDistance = 2f;
        loopSource.maxDistance = 18f;
        loopSource.volume = 0.65f;
        loopSource.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetFanEnabled(bool enabled)
    {
        fanEnabled = enabled;
        IsMaskingMosquitoAudio = enabled;
        if (loopSource == null)
            return;

        loopSource.mute = !enabled;
        if (enabled && !loopSource.isPlaying)
            loopSource.Play();
        else if (!enabled)
            loopSource.Stop();
    }

    public static FanGimmick Create(Vector3 position)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "FanGimmick";
        root.transform.position = position;
        root.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f);
        PanicMaterialFactory.ApplyColor(root.GetComponent<Renderer>(), new Color(0.7f, 0.72f, 0.75f));
        Object.Destroy(root.GetComponent<Collider>());

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "FanBlade";
        blade.transform.SetParent(root.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        blade.transform.localScale = new Vector3(1.2f, 0.05f, 0.18f);
        PanicMaterialFactory.ApplyColor(blade.GetComponent<Renderer>(), new Color(0.35f, 0.38f, 0.42f));
        Object.Destroy(blade.GetComponent<Collider>());

        return root.AddComponent<FanGimmick>();
    }
}
