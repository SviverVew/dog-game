using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using StarterAssets; // Thêm dòng này để nhận diện class ThirdPersonController mới

public class IntroCinematic : MonoBehaviour
{
    [Header("Danh sách Camera ảo quét qua các chiếc dép")]
    public List<CinemachineVirtualCamera> slipperCameras;

    [Header("Camera FreeLook điều khiển chú chó")]
    public CinemachineFreeLook playerCamera;

    [Header("Thời gian đứng lại ở mỗi chiếc dép (giây)")]
    public float viewDuration = 2f;

    private Player playerScript; 

    void Start()
    {
        // Tìm script di chuyển của người chơi để khóa lại lúc đang xem phim
        playerScript = FindObjectOfType<Player>(); // Đổi từ Player thành ThirdPersonController

        if (playerScript != null)
        {
            playerScript.enabled = false; // Khóa di chuyển của con chó lại
        }

        // Bắt đầu chuỗi chạy Cinematic
        StartCoroutine(PlayIntroRoutine());
    }

    IEnumerator PlayIntroRoutine()
    {
        // 1. Tắt tất cả camera ảo đi, chỉ bật camera đầu tiên lên
        DisableAllCameras();

        foreach (var cam in slipperCameras)
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                cam.Priority = 20; // Đẩy độ ưu tiên lên cao để Cinemachine tự động zoom mượt tới đây
                
                yield return new WaitForSeconds(viewDuration); // Đợi người chơi nhìn chiếc dép
                
                cam.Priority = 10; // Hạ ưu tiên xuống lại
                cam.gameObject.SetActive(false);
            }
        }

        // 2. Sau khi xem hết các chiếc dép, bật lại camera của Chú chó
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.Priority = 20;
        }

        // Đợi một chút cho camera zoom mượt từ chiếc dép cuối cùng bay về phía chú chó
        yield return new WaitForSeconds(1.5f); 

        // 3. Trả lại tự do cho người chơi!
        if (playerScript != null)
        {
            playerScript.enabled = true; // Mở khóa di chuyển, người chơi bắt đầu được điều khiển
        }

        Debug.Log("Cinematic hoàn tất! Bắt đầu game!");
    }

    void DisableAllCameras()
    {
        foreach (var cam in slipperCameras)
        {
            if (cam != null) cam.gameObject.SetActive(false);
        }
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
    }
}