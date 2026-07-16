using System.Collections;
using UnityEngine;
using TMPro;

public class DogAction : MonoBehaviour
{
    public Transform mouthPoint;

    public TMP_Text promptText;
    public float promptRadius = 2f;

    [Header("Animation Settings")]
    [Tooltip("Kéo thả Animator của mô hình con vào đây. Nếu để trống, code sẽ tự tìm ở các Object con.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string eatStateName = "EatingStart";
    // Hash ID để tối ưu hóa hiệu năng thay vì gọi chuỗi String liên tục
    private int _animIDEatTrigger; 

    private Slipper currentSlipper;
    public bool IsHoldingSlipper => currentSlipper != null;
    private bool awaitingDropConfirmation = false;
    private float dropConfirmDuration = 2f;
    private Coroutine dropConfirmCoroutine = null;

    private void Start()
    {
        // Nếu chưa kéo Animator ngoài Inspector, tự động tìm ở các con
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Đăng ký ID tham số Animator Trigger tên là "Eat"
        _animIDEatTrigger = Animator.StringToHash("Eat"); 
    }

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
                // Nếu đang ngậm dép, nhấn E lần đầu để xác nhận chuẩn bị thả
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

        Collider[] colliders = Physics.OverlapSphere(transform.position, promptRadius);

        foreach (Collider col in colliders)
        {
            Slipper slipper = col.GetComponent<Slipper>();

            if (slipper != null && !slipper.isPickedUp)
            {
                currentSlipper = slipper;
                slipper.isPickedUp = true;

                // XÓA TAG "Collected" ngay khi nhặt lên
                if (col.gameObject.CompareTag("Collected"))
                {
                    col.gameObject.tag = "Untagged";
                }

                // Tắt Collider để tránh lỗi vật lý xung đột với chó
                Collider slipperCol = slipper.GetComponent<Collider>();
                if (slipperCol != null)
                {
                    slipperCol.enabled = false;
                }

                // Cấu hình Rigidbody dép về Kinematic
                Rigidbody rb = slipper.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.detectCollisions = false;
                }

                // Gắn vào mồm chó
                slipper.transform.SetParent(mouthPoint, false);
                slipper.transform.localPosition = Vector3.zero;
                slipper.transform.localRotation = Quaternion.identity;

                // Chỉ chạy animation Eat khi nhặt dép thành công bằng phím E
                PlayEatAnimation();

                break;
            }
        }
    }

    void PlayEatAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(_animIDEatTrigger);
        animator.SetTrigger(_animIDEatTrigger);

        if (!string.IsNullOrEmpty(eatStateName))
        {
            animator.CrossFade(eatStateName, 0.05f, 0);
        }
    }

    void DropSlipper()
    {
        if (currentSlipper == null)
            return;

        if (dropConfirmCoroutine != null)
        {
            StopCoroutine(dropConfirmCoroutine);
            dropConfirmCoroutine = null;
        }
        awaitingDropConfirmation = false;

        // Gỡ dép ra khỏi mồm chó
        currentSlipper.transform.SetParent(null);
        currentSlipper.isPickedUp = false;

        // Khôi phục vật lý
        Rigidbody rb = currentSlipper.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            
            // Hất nhẹ chiếc dép về phía trước
            rb.AddForce(transform.forward * 3f + Vector3.up * 1.5f, ForceMode.Impulse);
        }

        Collider slipperCol = currentSlipper.GetComponent<Collider>();
        if (slipperCol != null)
        {
            slipperCol.enabled = true;
            slipperCol.isTrigger = false;
        }

        currentSlipper = null;
    }

    IEnumerator DropConfirmCountdown()
    {
        yield return new WaitForSeconds(dropConfirmDuration);
        awaitingDropConfirmation = false;
        dropConfirmCoroutine = null;
    }
}
