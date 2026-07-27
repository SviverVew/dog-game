using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // Hoặc Cinemachine nếu dùng bản cũ

public class PlayerNetworkCameraSetup : NetworkBehaviour
{
    [Header("Target để Camera nhìn vào")]
    public Transform cameraTarget; 

    public override void OnNetworkSpawn()
    {
        // CHỈ thực hiện trên máy của người chơi này
        if (IsOwner)
        {
            // 1. Tự động tìm Cinemachine Camera trong Scene và gắn Target vào nhân vật này
            var cinemachineCam = FindFirstObjectByType<CinemachineCamera>(); // Unity 6 / Cinemachine 3.0+
            // Nếu bạn dùng Cinemachine cũ hơn thì thay bằng line dưới:
            // var cinemachineCam = FindObjectOfType<CinemachineVirtualCamera>();

            if (cinemachineCam != null && cameraTarget != null)
            {
                cinemachineCam.Follow = cameraTarget;
                // cinemachineCam.LookAt = cameraTarget; (Nối dòng này nếu cần)
            }

            // 2. Tự động tìm Canvas UI Joystick trong Scene để nhận nút bấm di chuyển
            var uiInputs = FindFirstObjectByType<StarterAssets.UICanvasControllerInput>();
            var starterInputs = GetComponent<StarterAssets.StarterAssetsInputs>();
            
            if (uiInputs != null && starterInputs != null)
            {
                uiInputs.starterAssetsInputs = starterInputs;
            }
        }
    }
}