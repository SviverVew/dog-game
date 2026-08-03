using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // Standard namespace cho Cinemachine v3
using UnityEngine.InputSystem;

public class PlayerNetworkCameraSetup : NetworkBehaviour
{
    [Header("1. Vị trí Camera bám theo")]
    public Transform cameraTarget;

    [Header("2. Kéo tất cả Script di chuyển & Input vào đây")]
    public Behaviour[] scriptsToDisableIfNotOwner; 

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Bật tất cả script di chuyển & input cho máy LOCAL
            foreach (var script in scriptsToDisableIfNotOwner)
            {
                if (script != null) script.enabled = true;
            }

            if (TryGetComponent<PlayerInput>(out var pInput))
            {
                pInput.enabled = true;
            }

            SetupLocalCamera();
        }
        else
        {
            // Tắt input của đối phương để không ăn chung phím
            foreach (var script in scriptsToDisableIfNotOwner)
            {
                if (script != null) script.enabled = false;
            }

            if (TryGetComponent<PlayerInput>(out var pInput))
            {
                pInput.enabled = false;
            }

            if (TryGetComponent<AudioListener>(out var audioList))
            {
                audioList.enabled = false;
            }
        }
    }

    private void SetupLocalCamera()
    {
        if (cameraTarget == null)
            cameraTarget = transform;

        // Cinemachine v3: Tìm component CinemachineCamera
        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();

        if (vcam == null)
        {
            Debug.LogError("Không tìm thấy CinemachineCamera trong Scene!");
            return;
        }

        // Cinemachine v3 gán Target thông qua Target.TrackingTarget
        vcam.Target.TrackingTarget = cameraTarget;
        vcam.Target.LookAtTarget = cameraTarget;

        // Ưu tiên cao nhất cho máy local
        vcam.Priority.Value = 100;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var l in listeners)
            {
                if (l == null) continue;
                if (l.gameObject == mainCam.gameObject) continue;
                l.enabled = false;
            }

            if (mainCam.TryGetComponent<AudioListener>(out var audio))
                audio.enabled = true;
        }

        Debug.Log($"[v3 Camera Setup] Camera Follow -> {gameObject.name}");
    }
}