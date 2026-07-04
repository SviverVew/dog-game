using UnityEngine;

public class DropZone : MonoBehaviour
{
    [Header("Settings")]
    public int targetSlipperCount = 3; // Số dép cần thiết để qua màn
    private int currentSlipperCount = 0; // Số dép hiện tại đã thu thập được

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem vật thể bay/rơi vào vùng này có chứa component Slipper không
        Slipper slipper = other.GetComponent<Slipper>();

        if (slipper != null)
        {
            // CHỈ TÍNH ĐIỂM KHI ĐÔI DÉP ĐÃ ĐƯỢC THẢ RA (Không tính lúc chó đang ngậm đi qua)
            if (!slipper.isPickedUp)
            {
                // Thêm một cái tag ẩn hoặc component phụ để tránh việc 1 chiếc dép bị tính điểm 2 lần
                if (!other.gameObject.CompareTag("Collected"))
                {
                    other.gameObject.tag = "Collected"; // Đánh dấu dép đã tính điểm
                    currentSlipperCount++;
                    
                    Debug.Log($"Đã thu thập: {currentSlipperCount} / {targetSlipperCount} chiếc dép!");

                    // Khóa vật lý chiếc dép lại cho nó nằm im trong vùng nhận điểm
                    Rigidbody rb = other.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    // Kiểm tra điều kiện thắng màn
                    if (currentSlipperCount >= targetSlipperCount)
                    {
                        WinLevel();
                    }
                }
            }
        }
    }

    void WinLevel()
    {
        Debug.Log("CHÚC MỪNG! BẠN ĐÃ THU THẬP ĐỦ DÉP VÀ VƯỢT QUA MÀN!");
        
        // Đoạn này dùng để chuyển Scene sang màn tiếp theo
        // Để dùng được câu lệnh dưới, nhớ thêm "using UnityEngine.SceneManagement;" ở trên đầu nhé.
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}