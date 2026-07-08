using UnityEngine;
using TMPro; // BẮT BUỘC phải có dòng này để điều khiển TextMeshPro

public class DropZone : MonoBehaviour
{
    [Header("Mission UI Setup")]
    public TMP_Text missionText; // Kéo file MissionText vào đây trong Inspector
    public string missionPrefix = "Số dép đã thu thập: ";

    [Header("Settings")]
    public int targetSlipperCount = 5; // Đổi thành 5 theo yêu cầu của bạn
    private int currentSlipperCount = 0; 

    void Start()
    {
        // Cập nhật text hiển thị ngay khi bắt đầu game (Ví dụ: 0/5)
        UpdateMissionUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        Slipper slipper = other.GetComponent<Slipper>();

        // Chỉ tính điểm khi vật thể là Dép và ĐÃ ĐƯỢC THẢ RA
        if (slipper != null && !slipper.isPickedUp)
        {
            if (!other.gameObject.CompareTag("Collected"))
            {
                other.gameObject.tag = "Collected"; 
                currentSlipperCount++;
                
                // CẬP NHẬT CHỮ TRÊN MÀN HÌNH
                UpdateMissionUI();

                if (currentSlipperCount >= targetSlipperCount)
                {
                    WinLevel();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Slipper slipper = other.GetComponent<Slipper>();

        // Nếu dép đã tính điểm bị mang đi nơi khác -> Trừ điểm
        if (slipper != null && other.gameObject.CompareTag("Collected"))
        {
            other.gameObject.tag = "Untagged"; 
            currentSlipperCount--; 
            
            // CẬP NHẬT LẠI CHỮ TRÊN MÀN HÌNH (Ví dụ đang 2/5 tụt xuống 1/5)
            UpdateMissionUI();
        }
    }

    // Hàm phụ trách thay đổi chữ trên thanh nhiệm vụ
    void UpdateMissionUI()
    {
        if (missionText != null)
        {
            missionText.text = missionPrefix + currentSlipperCount + "/" + targetSlipperCount;
        }
    }

    void WinLevel()
    {
        if (missionText != null)
        {
            missionText.text = "<color=green>NHIỆM VỤ HOÀN THÀNH!</color>";
        }
        Debug.Log("CHÚC MỪNG! BẠN ĐÃ VƯỢT QUA MÀN!");
        // Thêm logic chuyển cảnh tại đây nếu muốn
    }
}