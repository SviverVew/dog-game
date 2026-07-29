using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // Hoặc Cinemachine nếu dùng v2 cũ
using UnityEngine.InputSystem;

public class PlayerNetworkCameraSetup : NetworkBehaviour
{
    [Header("1. Vị trí Camera bám theo")]
    public Transform cameraTarget;

    [Header("2. Kéo tất cả Script di chuyển & Input vào đây")]
    public Behaviour[] scriptsToDisableIfNotOwner; // Ví dụ: PersonPlayer, Player, StarterAssetsInputs,...

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // ==========================================
            // LÀ MÁY CỦA MÌNH (IsOwner = true)
            // ==========================================

            // 1. Bật tất cả script di chuyển & input của máy mình
            foreach (var script in scriptsToDisableIfNotOwner)
            {
                if (script != null) script.enabled = true;
            }

            if (TryGetComponent<PlayerInput>(out var pInput))
            {
                pInput.enabled = true;
            }

            // 2. Ép Camera trên Scene CHỈ FOLLOW theo nhân vật của máy mình
            SetupLocalCamera();
        }
        else
        {
            // ==========================================
            // LÀ MÁY ĐỐI PHƯƠNG (IsOwner = false)
            // ==========================================

            // 🚨 TẮT SẠCH Input & Di chuyển của đối phương để không bị ăn chung Input
            foreach (var script in scriptsToDisableIfNotOwner)
            {
                if (script != null) script.enabled = false;
            }

            if (TryGetComponent<PlayerInput>(out var pInput))
            {
                pInput.enabled = false; // Tắt hoàn toàn PlayerInput đối phương
            }

            // Tắt AudioListener của đối phương nếu có
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

        var vcam = FindFirstObjectByType<CinemachineCamera>();

        if (vcam == null)
        {
            Debug.LogError("Không tìm thấy CinemachineCamera!");
            return;
        }

        vcam.Follow = cameraTarget;
        vcam.LookAt = cameraTarget;

        Camera mainCam = Camera.main;

        if (mainCam != null)
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            foreach (var l in listeners)
                l.enabled = false;

            if (mainCam.TryGetComponent<AudioListener>(out var audio))
                audio.enabled = true;
        }

        Debug.Log($"Camera Follow -> {gameObject.name}");
    }
}