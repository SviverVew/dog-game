using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // Nhớ đổi thành Cinemachine nếu dùng bản cũ (CinemachineVirtualCamera)
using UnityEngine.InputSystem; // Cần dùng nếu dự án dùng New Input System

public class PlayerNetworkCameraSetup : NetworkBehaviour
{
    [Header("Target để Camera nhìn vào")]
    public Transform cameraTarget;

    [Header("Danh sách Script điều khiển (Cần tắt trên máy đối phương)")]
    [SerializeField] private Behaviour[] scriptsToDisableOnOthers;

    public override void OnNetworkSpawn()
    {
        // ==========================================
        // 1. NẾU LÀ NHÂN VẬT CỦA MÁY NÀY (IsOwner)
        // ==========================================
        if (IsOwner)
        {
            // Bật lại các script di chuyển (phòng trường hợp prefab bị ẩn sẵn)
            foreach (var script in scriptsToDisableOnOthers)
            {
                if (script != null) script.enabled = true;
            }

            // Gán Cinemachine Camera follow theo nhân vật này
            var cinemachineCam = FindFirstObjectByType<CinemachineCamera>(); // Unity 6 / Cinemachine 3.0+
            // Nếu dùng Cinemachine cũ (v2): 
            // var cinemachineCam = FindFirstObjectByType<CinemachineVirtualCamera>();

            if (cinemachineCam != null && cameraTarget != null)
            {
                cinemachineCam.Follow = cameraTarget;
                cinemachineCam.LookAt = cameraTarget; // Thêm LookAt nếu game của bạn dùng tới
            }

            // Gán UI Joystick di chuyển
            var uiInputs = FindFirstObjectByType<StarterAssets.UICanvasControllerInput>();
            var starterInputs = GetComponent<StarterAssets.StarterAssetsInputs>();

            if (uiInputs != null && starterInputs != null)
            {
                uiInputs.starterAssetsInputs = starterInputs;
            }
        }
        // ==========================================
        // 2. NẾU LÀ NHÂN VẬT CỦA MÁY KHÁC (!IsOwner)
        // ==========================================
        else
        {
            // 🚨 ĐIỂM QUAN TRỌNG NHẤT: Tắt toàn bộ script di chuyển của nhân vật máy khác!
            foreach (var script in scriptsToDisableOnOthers)
            {
                if (script != null) script.enabled = false;
            }

            // Tắt PlayerInput (nếu bạn dùng Starter Assets New Input System)
            if (TryGetComponent<PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
        }
    }
}