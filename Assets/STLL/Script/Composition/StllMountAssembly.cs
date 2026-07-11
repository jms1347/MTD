using UnityEngine;

public enum StllHorseKind
{
    DefaultRammer = 0
}

public enum StllRiderKind
{
    LegoWarlord = 0
}

public enum StllWeaponKind
{
    Glaive = 0
}

/// <summary>말·기수·무기를 분리 조립/교체합니다.</summary>
public class StllMountAssembly : MonoBehaviour
{
    [SerializeField] private Color accentColor = new(0.95f, 0.55f, 0.15f);
    [SerializeField] private StllHorseKind horseKind = StllHorseKind.DefaultRammer;
    [SerializeField] private StllRiderKind riderKind = StllRiderKind.LegoWarlord;
    [SerializeField] private StllWeaponKind weaponKind = StllWeaponKind.Glaive;

    private Transform visualRoot;
    private StllHorseVisualBuilder.Result horseVisual;
    private StllRiderVisualBuilder.Result riderVisual;
    private StllWeaponVisualBuilder.Result weaponVisual;
    private StllHorseRideVisual rideVisual;

    public Transform BladePivot => weaponVisual.BladePivot;
    public Transform HeadPivot => riderVisual.HeadPivot;

    private void Awake()
    {
        RebuildLoadout(horseKind, riderKind, weaponKind);
    }

    public void RebuildLoadout(StllHorseKind horse, StllRiderKind rider, StllWeaponKind weapon)
    {
        horseKind = horse;
        riderKind = rider;
        weaponKind = weapon;

        if (visualRoot != null)
            Destroy(visualRoot.gameObject);

        visualRoot = new GameObject("MountVisual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;

        horseVisual = StllHorseVisualBuilder.Build(visualRoot, accentColor);
        riderVisual = StllRiderVisualBuilder.Build(horseVisual.RiderMountPoint, accentColor);

        if (weaponKind == StllWeaponKind.Glaive)
            weaponVisual = StllWeaponVisualBuilder.BuildGlaive(riderVisual.RightHandSocket, accentColor);

        rideVisual = visualRoot.gameObject.AddComponent<StllHorseRideVisual>();
        rideVisual.Bind(
            horseVisual.HorseRoot,
            horseVisual.LegFl,
            horseVisual.LegFr,
            horseVisual.LegBl,
            horseVisual.LegBr,
            riderVisual.RiderRoot,
            riderVisual.HeadPivot);

        if (GetComponent<StllGlaiveSwingVisual>() == null)
            gameObject.AddComponent<StllGlaiveSwingVisual>();
    }
}
