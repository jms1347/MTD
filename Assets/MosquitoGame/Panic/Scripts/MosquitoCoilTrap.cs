using UnityEngine;

public class MosquitoCoilTrap : MonoBehaviour
{
    private SphereCollider trigger;

    public static GameObject Create(Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("Trap_MosquitoCoil");
        root.transform.SetPositionAndRotation(position, rotation);

        var baseCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseCylinder.name = "CoilBase";
        baseCylinder.transform.SetParent(root.transform, false);
        baseCylinder.transform.localScale = new Vector3(0.35f, 0.05f, 0.35f);
        PanicMaterialFactory.ApplyColor(baseCylinder.GetComponent<Renderer>(), new Color(0.2f, 0.75f, 0.25f));
        Object.Destroy(baseCylinder.GetComponent<Collider>());

        var aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aura.name = "CoilAura";
        aura.transform.SetParent(root.transform, false);
        aura.transform.localScale = Vector3.one * PanicGameConstants.CoilRadius * 2f;
        PanicMaterialFactory.ApplyColor(aura.GetComponent<Renderer>(), new Color(0.2f, 0.9f, 0.35f, 0.18f), transparent: true);
        Object.Destroy(aura.GetComponent<Collider>());

        var trap = root.AddComponent<MosquitoCoilTrap>();
        trap.trigger = root.AddComponent<SphereCollider>();
        trap.trigger.isTrigger = true;
        trap.trigger.radius = PanicGameConstants.CoilRadius;
        trap.trigger.center = Vector3.up * 0.2f;

        return root;
    }

    private void OnTriggerStay(Collider other)
    {
        var mosquito = other.GetComponentInParent<MosquitoController>();
        if (mosquito == null)
            return;

        mosquito.ApplyCoilEffect(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var mosquito = other.GetComponentInParent<MosquitoController>();
        if (mosquito == null)
            return;

        mosquito.ClearCoilEffect(this);
    }
}
