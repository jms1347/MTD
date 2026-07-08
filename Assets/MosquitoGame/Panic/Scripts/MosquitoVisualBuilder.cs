using UnityEngine;

public class MosquitoWingFlap : MonoBehaviour
{
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private float flapSpeed = 48f;
    [SerializeField] private float flapAngle = 28f;

    private void Update()
    {
        if (leftWing == null || rightWing == null)
            return;

        var angle = Mathf.Sin(Time.time * flapSpeed) * flapAngle;
        leftWing.localRotation = Quaternion.Euler(0f, 0f, angle);
        rightWing.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    public void Configure(Transform left, Transform right)
    {
        leftWing = left;
        rightWing = right;
    }
}

public static class MosquitoVisualBuilder
{
    public static GameObject Build(Transform parent)
    {
        var root = new GameObject("MosquitoVisual");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        PanicMaterialFactory.ApplyColor(body.GetComponent<Renderer>(), new Color(0.45f, 0.28f, 0.12f));
        Object.Destroy(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.24f);
        head.transform.localScale = Vector3.one * 0.11f;
        PanicMaterialFactory.ApplyColor(head.GetComponent<Renderer>(), new Color(0.35f, 0.22f, 0.1f));
        Object.Destroy(head.GetComponent<Collider>());

        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(head.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.15f, 0.35f);
        eye.transform.localScale = Vector3.one * 0.35f;
        PanicMaterialFactory.ApplyColor(eye.GetComponent<Renderer>(), new Color(0.9f, 0.1f, 0.1f));
        Object.Destroy(eye.GetComponent<Collider>());

        var proboscis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        proboscis.name = "Proboscis";
        proboscis.transform.SetParent(head.transform, false);
        proboscis.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        proboscis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        proboscis.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
        PanicMaterialFactory.ApplyColor(proboscis.GetComponent<Renderer>(), new Color(0.55f, 0.55f, 0.58f));
        Object.Destroy(proboscis.GetComponent<Collider>());

        var leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.name = "LeftWing";
        leftWing.transform.SetParent(root.transform, false);
        leftWing.transform.localPosition = new Vector3(-0.16f, 0.02f, 0.02f);
        leftWing.transform.localScale = new Vector3(0.22f, 0.01f, 0.12f);
        PanicMaterialFactory.ApplyColor(leftWing.GetComponent<Renderer>(), new Color(0.75f, 0.8f, 0.85f, 0.45f), transparent: true);
        Object.Destroy(leftWing.GetComponent<Collider>());

        var rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.name = "RightWing";
        rightWing.transform.SetParent(root.transform, false);
        rightWing.transform.localPosition = new Vector3(0.16f, 0.02f, 0.02f);
        rightWing.transform.localScale = new Vector3(0.22f, 0.01f, 0.12f);
        PanicMaterialFactory.ApplyColor(rightWing.GetComponent<Renderer>(), new Color(0.75f, 0.8f, 0.85f, 0.45f), transparent: true);
        Object.Destroy(rightWing.GetComponent<Collider>());

        var flap = root.AddComponent<MosquitoWingFlap>();
        flap.Configure(leftWing.transform, rightWing.transform);

        return root;
    }
}
