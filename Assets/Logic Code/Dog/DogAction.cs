using System.Collections;
using UnityEngine;
using TMPro;

public class DogAction : MonoBehaviour
{
    public Transform mouthPoint;

    public TMP_Text promptText;
    public float promptRadius = 2f;

    [Header("Animation Settings")]
    [Tooltip("Kéo thả CHÍNH XÁC Thằng Con (chứa Animator) vào đây.")]
    [SerializeField] private Animator animator;
    private int _animIDIsEatingBool; 

    [Tooltip("Thời gian trễ (giây) tính từ lúc ấn E cúi đầu cho đến khi mồm chạm dép để ngậm lên.")]
    [SerializeField] private float pickUpDelay = 0.45f; 

    private Slipper currentSlipper;
    public bool IsHoldingSlipper => currentSlipper != null;
    
    // Biến trạng thái để chặn người chơi spam E liên tục khi đang diễn hoạt ảnh nhặt
    private bool isPickingUpInProgress = false; 

    private bool awaitingDropConfirmation = false;
    private float dropConfirmDuration = 2f;
    private Coroutine dropConfirmCoroutine = null;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        _animIDIsEatingBool = Animator.StringToHash("Eat"); 
    }

    void Update()
    {
        UpdatePrompt();

        // Chặn không cho bấm nút hành động nếu đang trong quá trình chuyển giao nhặt dép
        if (isPickingUpInProgress) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentSlipper == null)
            {
                // Bắt đầu tiến trình nhặt dép có chờ đợi animation
                StartCoroutine(PickUpSlipperRoutine());
            }
            else
            {
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
        if (promptText == null) return;

        // Nếu đang bận cúi đầu nhặt dép thì ẩn tạm prompt đi cho sạch màn hình
        if (isPickingUpInProgress)
        {
            HidePrompt();
            return;
        }

        if (currentSlipper == null)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, promptRadius);
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

            if (found) ShowPrompt("Nhấn E để nhặt dép");
            else HidePrompt();
        }
        else
        {
            if (awaitingDropConfirmation) ShowPrompt("Nhấn E lần nữa để thả dép");
            else ShowPrompt("Nhấn E để thả dép");
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

    // Coroutine xử lý: Diễn hoạt ảnh trước -> Chờ chạm đất -> Ngậm dép lên mồm
    IEnumerator PickUpSlipperRoutine()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, promptRadius);
        Slipper targetSlipper = null;

        // Tìm chiếc dép hợp lệ gần nhất trước khi bắt đầu cúi đầu
        foreach (Collider col in colliders)
        {
            Slipper slipper = col.GetComponent<Slipper>();
            if (slipper != null && !slipper.isPickedUp)
            {
                targetSlipper = slipper;
                break;
            }
        }

        // Nếu không có dép ở gần thì không làm gì cả
        if (targetSlipper == null) yield break;

        // Bắt đầu quá trình nhặt (Khóa phím E tạm thời)
        isPickingUpInProgress = true;

        // 1. KÍCH HOẠT ANIMATION ĂN (Chú chó bắt đầu cúi đầu xuống)
        if (animator != null)
        {
            animator.SetBool(_animIDIsEatingBool, true);
        }

        // 2. CHỜ hoạt ảnh cúi đầu diễn ra đạt tới điểm mồm chạm đất
        yield return new WaitForSeconds(pickUpDelay);

        // Kiểm tra lại lần nữa phòng trường hợp chiếc dép bị biến mất/ai đó nhặt mất trong lúc chờ
        if (targetSlipper != null && !targetSlipper.isPickedUp)
        {
            currentSlipper = targetSlipper;
            currentSlipper.isPickedUp = true;

            if (currentSlipper.gameObject.CompareTag("Collected"))
            {
                currentSlipper.gameObject.tag = "Untagged";
            }

            // Tắt Collider & Rigidbody vật lý
            Collider slipperCol = currentSlipper.GetComponent<Collider>();
            if (slipperCol != null) slipperCol.enabled = false;

            Rigidbody rb = currentSlipper.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.detectCollisions = false;
            }

            // 3. CHÍNH THỨC HÚT DÉP VÀO MỒM
            currentSlipper.transform.SetParent(mouthPoint, false);
            currentSlipper.transform.localPosition = Vector3.zero;
            currentSlipper.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Nếu có lỗi xảy ra không ngậm được dép, trả trạng thái animation về bình thường
            if (animator != null)
            {
                animator.SetBool(_animIDIsEatingBool, false);
            }
        }

        // Mở khóa phím E
        isPickingUpInProgress = false;
    }

    void DropSlipper()
    {
        if (currentSlipper == null) return;

        if (dropConfirmCoroutine != null)
        {
            StopCoroutine(dropConfirmCoroutine);
            dropConfirmCoroutine = null;
        }
        awaitingDropConfirmation = false;

        if (animator != null)
        {
            animator.SetBool(_animIDIsEatingBool, false);
        }

        currentSlipper.transform.SetParent(null);
        currentSlipper.isPickedUp = false;

        Rigidbody rb = currentSlipper.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
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