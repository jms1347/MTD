using Unity.Netcode;
using UnityEngine;

public class StllEaDebugInput : MonoBehaviour
{
    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (Input.GetKeyDown(KeyCode.H))
            StllRunController.Instance?.ServerForceStartHub();

        if (Input.GetKeyDown(KeyCode.K))
            StllRunController.Instance?.ServerNotifyStageSurvived();

        if (Input.GetKeyDown(KeyCode.J))
            StllRunController.Instance?.ServerNotifyBossDefeated();
    }
}
