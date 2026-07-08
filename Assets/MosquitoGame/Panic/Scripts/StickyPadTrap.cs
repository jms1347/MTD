using UnityEngine;

public class StickyPadTrap : MonoBehaviour
{
    public static GameObject Create(Vector3 position, Quaternion rotation)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Trap_StickyPad";
        root.transform.SetPositionAndRotation(position, rotation);
        root.transform.localScale = new Vector3(0.55f, 0.03f, 0.55f);
        PanicMaterialFactory.ApplyColor(root.GetComponent<Renderer>(), new Color(0.95f, 0.85f, 0.1f));

        var collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        root.AddComponent<StickyPadTrap>();
        return root;
    }

    private void OnTriggerEnter(Collider other)
    {
        var mosquito = other.GetComponentInParent<MosquitoController>();
        if (mosquito == null)
            return;

        mosquito.ApplyStickyStun(PanicGameConstants.StickyStunSeconds);
    }
}
