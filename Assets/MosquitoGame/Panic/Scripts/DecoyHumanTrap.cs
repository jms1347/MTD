using UnityEngine;

public class DecoyHumanTrap : MonoBehaviour
{
    public static GameObject Create(Vector3 position, Quaternion rotation)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Trap_DecoyHuman";
        root.transform.SetPositionAndRotation(position, rotation);
        root.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
        PanicMaterialFactory.ApplyColor(root.GetComponent<Renderer>(), new Color(0.95f, 0.95f, 0.95f));

        var collider = root.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        root.AddComponent<DecoyHumanTrap>();
        return root;
    }

    public void TriggerAlarm(Vector3 mosquitoPosition)
    {
        PanicAudioCue.PlayDecoyAlarm();
        var humans = FindObjectsByType<HumanController>(FindObjectsSortMode.None);
        foreach (var human in humans)
            human?.ShowMosquitoReveal(mosquitoPosition, 4f);
    }
}
