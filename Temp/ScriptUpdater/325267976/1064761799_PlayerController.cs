using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    private Transform mainCameraTransform; // Để di chuyển theo hướng Camera nhìn

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 15f; // Tốc độ xoay mượt mà
    private float currentSpeed;
    private Vector3 moveDirection;

    [Header("Jumping & Gravity")]
    public float jumpForce = 7f;
    public float extraGravity = 15f; // Trọng lực phụ thêm để rớt xuống mượt, không bị bay lơ lửng
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Sprint System")]
    public float sprintDuration = 3f;
    public float sprintCooldown = 5f;
    private bool isSprinting = false;
    private float sprintTimeRemaining;
    private float sprintCooldownRemaining;

    [Header("Actions & Stats")]
    public int maxHealth = 100;
    private int currentHealth;
    public Slider healthBar;
    public AudioClip barkClip;
    public AudioClip yelpClip;
    private AudioSource audioSource;
    private bool canBite = true;
    public float biteCooldown = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Lấy Camera chính để tính góc di chuyển
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Cấu hình Rigidbody chuẩn cho game TPS/FPS
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        sprintTimeRemaining = sprintDuration;
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Khóa chuột
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Kiểm tra mặt đất liên tục
        isGrounded = CheckGrounded();

        // 2. Nhận Input từ bàn phím
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveForward = Input.GetAxisRaw("Vertical");
        
        // Tính toán hướng di chuyển dựa trên hướng góc nhìn của Camera
        if (mainCameraTransform != null)
        {
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;
            camForward.y = 0; // Khóa trục Y để không bị đi cắm đầu xuống đất khi camera nhìn xuống
            camRight.y = 0;
            moveDirection = (camForward.normalized * moveForward + camRight.normalized * moveHorizontal).normalized;
        }
        else
        {
            moveDirection = (Vector3.forward * moveForward + Vector3.right * moveHorizontal).normalized;
        }

        // 3. Xử lý Nhảy
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Các hàm phụ trợ giữ nguyên logic của bạn
        UpdateSprint(moveHorizontal, moveForward);
        UpdateActions();
        UpdateCursorLock();
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyExtraGravity();
    }

    void MovePlayer()
    {
        // Chọn tốc độ dựa trên việc có đang chạy nhanh hay không
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        
        // Nội suy tốc độ để tăng/giảm tốc mượt mà thay vì khựng khựng
        currentSpeed = Mathf.MoveTowards(currentSpeed, moveDirection.magnitude * targetSpeed, Time.fixedDeltaTime * 20f);

        // Đổi vận tốc (Velocity) trên trục X và Z, giữ nguyên vận tốc nhảy trục Y
        Vector3 velocity = moveDirection * currentSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        // Xoay nhân vật mượt mà theo hướng di chuyển giống PUBG
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void Jump()
    {
        // Thay đổi vận tốc trục Y trực tiếp để lực nhảy đồng đều, không bị ảnh hưởng bởi vận tốc cũ
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void ApplyExtraGravity()
    {
        // Khi ở trên không, áp thêm một lực trọng lực phụ để lúc rớt xuống có cảm giác nặng và thật hơn (giống PUBG)
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    bool CheckGrounded()
    {
        // Dùng SphereCast quét một hình cầu nhỏ dưới chân để check đất mượt hơn Raycast đơn
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        Vector3 origin = transform.position + Vector3.up * (capsule.radius);
        float radius = capsule.radius * 0.9f; // Hơi thu nhỏ bán kính để tránh kẹt tường
        float castDistance = capsule.radius + 0.1f;

        return Physics.SphereCast(origin, radius, Vector3.down, out _, castDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    // --- CÁC HÀM LOGIC CŨ ĐƯỢC GIỮ NGUYÊN VÀ TỐI ƯU HÓA ---

    void UpdateSprint(float h, float v)
    {
        bool wantSprint = Input.GetKey(KeyCode.LeftShift);
        bool hasMovementInput = Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f;

        if (wantSprint && sprintTimeRemaining > 0f && sprintCooldownRemaining <= 0f && hasMovementInput)
        {
            isSprinting = true;
            sprintTimeRemaining -= Time.deltaTime;
            if (sprintTimeRemaining <= 0f)
            {
                sprintCooldownRemaining = sprintCooldown;
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;
            if (sprintCooldownRemaining > 0f)
            {
                sprintCooldownRemaining -= Time.deltaTime;
                if (sprintCooldownRemaining <= 0f) sprintTimeRemaining = sprintDuration;
            }
            else if (!wantSprint)
            {
                sprintTimeRemaining = Mathf.Min(sprintTimeRemaining + Time.deltaTime, sprintDuration);
            }
        }
    }

    void UpdateActions()
    {
        if (Input.GetKeyDown(KeyCode.Q) && canBite) StartCoroutine(BiteRoutine());
        if (Input.GetKeyDown(KeyCode.R)) Bark();
    }

    IEnumerator BiteRoutine()
    {
        canBite = false;
        Debug.Log("Cắn! (Q)");
        yield return new WaitForSeconds(biteCooldown);
        canBite = true;
    }

    void Bark()
    {
        if (audioSource != null && barkClip != null)
        {
            audioSource.PlayOneShot(barkClip);
            Debug.Log("Sủa! (R)");
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHealthUI();
        if (audioSource != null && yelpClip != null) audioSource.PlayOneShot(yelpClip);
    }

    void UpdateHealthUI() { if (healthBar != null) healthBar.value = currentHealth; }

    void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }
}