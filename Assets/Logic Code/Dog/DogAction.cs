using System.Collections;
using UnityEngine;
using TMPro;

public class DogAction : MonoBehaviour
{
    public Transform mouthPoint;

    public TMP_Text promptText;
    public float promptRadius = 2f;

    private Slipper currentSlipper;
    public bool IsHoldingSlipper => currentSlipper != null;
    private bool awaitingDropConfirmation = false;
    private float dropConfirmDuration = 2f;
    private Coroutine dropConfirmCoroutine = null;

    void Update()
    {
        UpdatePrompt();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentSlipper == null)
            {
                PickUpSlipper();
            }
            else
            {
                // If already holding, first press confirms drop
                if (!awaitingDropConfirmation)
                {
                    awaitingDropConfirmation = true;
                    Debug.Log("Nhấn E lần nữa để thả dép.");
                    dropConfirmCoroutine = StartCoroutine(DropConfirmCountdown());
                }
                else
                {
                    DropSlipper();
                }
            }
        }
    }

    void UpdatePrompt()
    {
        if (promptText == null)
            return;

        if (currentSlipper == null)
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                promptRadius
            );

            bool found = false;

            foreach (Collider col in colliders)
            {
                Slipper slipper = col.GetComponent<Slipper>();
                if (slipper != null && !slipper.isPickedUp)
                {
                    found = true;
                    break;
                }
            }

            if (found)
                ShowPrompt("Nhấn E để nhặt dép");
            else
                HidePrompt();
        }
        else
        {
            if (awaitingDropConfirmation)
                ShowPrompt("Nhấn E lần nữa để thả dép");
            else
                ShowPrompt("Nhấn E để thả dép");
        }
    }

    void ShowPrompt(string message)
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(true);
        promptText.text = message;
    }

    void HidePrompt()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
    }

    void PickUpSlipper()
{
    if (currentSlipper != null)
        return;

    Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);

    foreach (Collider col in colliders)
    {
        Slipper slipper = col.GetComponent<Slipper>();

        if (slipper != null && !slipper.isPickedUp)
        {
            currentSlipper = slipper;
            slipper.isPickedUp = true;

            // 1. Tắt Collider hoàn toàn thay vì để isTrigger (để chắc chắn không va chạm với chân chó)
            Collider slipperCol = slipper.GetComponent<Collider>();
            if (slipperCol != null)
            {
                slipperCol.enabled = false; 
            }

            // 2. Xử lý Rigidbody cực kỳ nghiêm ngặt
            Rigidbody rb = slipper.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;        // Xóa sạch vận tốc thừa
                rb.angularVelocity = Vector3.zero; // Xóa sạch vận tốc xoay thừa
                rb.detectCollisions = false;       // Ép hệ thống vật lý ngừng quét va chạm trên vật này
            }

            // 3. Đưa vào miệng con chó
            slipper.transform.SetParent(mouthPoint, false);
            slipper.transform.localPosition = Vector3.zero;
            slipper.transform.localRotation = Quaternion.identity;

            break;
        }
    }
}

void DropSlipper()
    {
        if (currentSlipper == null)
            return;

        // Hủy bộ đếm thời gian chờ ấn E lần 2
        if (dropConfirmCoroutine != null)
        {
            StopCoroutine(dropConfirmCoroutine);
            dropConfirmCoroutine = null;
        }
        awaitingDropConfirmation = false;

        // 1. Gỡ chiếc dép ra khỏi miệng con chó (Không còn là cha con nữa)
        currentSlipper.transform.SetParent(null);

        // 2. CẬP NHẬT TRẠNG THÁI TRƯỚC: Báo cho hệ thống biết dép đã được thả tự do
        // Dòng này đặt ở đây giúp DropZone nhận diện chính xác ngay lập tức khi va chạm
        currentSlipper.isPickedUp = false;

        // 3. Khôi phục lại các thuộc tính vật lý của Rigidbody
        Rigidbody rb = currentSlipper.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            
            // Hất nhẹ chiếc dép về phía trước và hướng lên trên một chút cho đẹp mắt
            rb.AddForce(transform.forward * 3f + Vector3.up * 1.5f, ForceMode.Impulse);
        }

        // 4. Bật lại Collider vật lý của chiếc dép để nó có thể rơi chạm đất hoặc chạm DropZone
        Collider slipperCol = currentSlipper.GetComponent<Collider>();
        if (slipperCol != null)
        {
            slipperCol.enabled = true;
            slipperCol.isTrigger = false;
        }

        // 5. Giải phóng biến tạm trên người chó để có thể đi nhặt chiếc dép tiếp theo
        currentSlipper = null;
    }

    IEnumerator DropConfirmCountdown()
    {
        yield return new WaitForSeconds(dropConfirmDuration);
        awaitingDropConfirmation = false;
        dropConfirmCoroutine = null;
    }
}