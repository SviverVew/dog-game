using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    private Transform mainCameraTransform;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 15f; 
    private float currentSpeed;
    private Vector3 moveDirection;

    [Header("Jumping & Gravity")]
    public float jumpForce = 7f;
    public float extraGravity = 15f; 
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Sprint System")]
    public float sprintDuration = 3f;
    public float sprintCooldown = 5f;
    private bool isSprinting = false;
    private float sprintTimeRemaining;
    private float sprintCooldownRemaining;

    [Header("Audio")]
    public AudioClip barkClip;
    public AudioClip yelpClip;
    private AudioSource audioSource;

    [Header("Combat & Stats")]
    public int maxHealth = 100;
    private int currentHealth;
    public Slider healthBar;
    
    public float biteDuration = 0.25f;
    public float biteCooldown = 0.5f;
    private bool canBite = true;

    [Header("Bark Anti-Spam System")]
    public int barkLimit = 3;
    public float barkWindow = 5f;
    public float barkPenaltyCooldown = 5f;
    private int barkCount = 0;
    private float barkWindowTimer = 0f;
    private float barkPenaltyTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Cấu hình Rigidbody mượt mà chống giật hình
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Tự động kiểm tra và thêm AudioSource nếu thiếu
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            Debug.Log("AudioSource missing on Player: added one automatically.");
        }

        if (groundLayer == 0) groundLayer = ~0;

        sprintTimeRemaining = sprintDuration;
        sprintCooldownRemaining = 0f;
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Khóa và ẩn con trỏ chuột
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        isGrounded = CheckGrounded();

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveForward = Input.GetAxisRaw("Vertical");
        
        // Tính hướng di chuyển mượt dựa trên góc nhìn Camera tựa PUBG
        if (mainCameraTransform != null)
        {
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;
            camForward.y = 0; 
            camRight.y = 0;
            moveDirection = (camForward.normalized * moveForward + camRight.normalized * moveHorizontal).normalized;
        }
        else
        {
            moveDirection = (Vector3.forward * moveForward + Vector3.right * moveHorizontal).normalized;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Cập nhật các bộ đếm thời gian và hành động cũ của bạn
        UpdateSprint(moveHorizontal, moveForward);
        UpdateBarkTimers();
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
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        
        // Tạo gia tốc tăng/giảm tốc êm ái, loại bỏ hoàn toàn cảm giác khựng
        currentSpeed = Mathf.MoveTowards(currentSpeed, moveDirection.magnitude * targetSpeed, Time.fixedDeltaTime * 20f);

        Vector3 velocity = moveDirection * currentSpeed;
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);

        // Xoay nhân vật mượt mà theo hướng di chuyển chân thực
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    void ApplyExtraGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    bool CheckGrounded()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        Vector3 origin = transform.position + Vector3.up * (capsule.radius);
        float radius = capsule.radius * 0.9f; 
        float castDistance = capsule.radius + 0.1f;

        return Physics.SphereCast(origin, radius, Vector3.down, out _, castDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    void UpdateSprint(float h, float v)
    {
        bool wantSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool hasMovementInput = Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f;

        if (wantSprint && sprintTimeRemaining > 0f && sprintCooldownRemaining <= 0f && hasMovementInput)
        {
            isSprinting = true;
            sprintTimeRemaining -= Time.deltaTime;
            if (sprintTimeRemaining <= 0f)
            {
                sprintTimeRemaining = 0f;
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
                if (sprintCooldownRemaining <= 0f)
                {
                    sprintCooldownRemaining = 0f;
                    sprintTimeRemaining = sprintDuration;
                }
            }
            else if (!wantSprint)
            {
                sprintTimeRemaining = Mathf.Min(sprintTimeRemaining + Time.deltaTime, sprintDuration);
            }
        }
    }

    void UpdateActions()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Bite();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Bark();
        }
    }

    void Bite()
    {
        if (!canBite) return;
        canBite = false;
        Debug.Log("Cắn! (Q)");
        StartCoroutine(BiteCooldown());
    }

    IEnumerator BiteCooldown()
    {
        yield return new WaitForSeconds(biteDuration);
        yield return new WaitForSeconds(biteCooldown - biteDuration);
        canBite = true;
    }

    void Bark()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("Bark failed: no AudioSource available.");
            return;
        }

        if (barkClip == null)
        {
            Debug.LogWarning("Bark failed: no barkClip assigned in Inspector.");
            return;
        }

        if (barkPenaltyTimer > 0f)
        {
            Debug.Log("Bark blocked: waiting for cooldown.");
            return;
        }

        if (barkCount == 0)
        {
            barkWindowTimer = barkWindow;
        }

        if (barkCount >= barkLimit)
        {
            barkPenaltyTimer = barkPenaltyCooldown;
            Debug.Log("Bark limit reached: 5s cooldown.");
            return;
        }

        audioSource.PlayOneShot(barkClip);
        barkCount++;
        Debug.Log("Sủa! (R) " + barkCount + "/" + barkLimit);

        if (barkCount >= barkLimit)
        {
            barkPenaltyTimer = barkPenaltyCooldown;
            barkWindowTimer = 0f;
            Debug.Log("Bark limit reached: 5s cooldown.");
        }
    }

    void UpdateBarkTimers()
    {
        if (barkPenaltyTimer > 0f)
        {
            barkPenaltyTimer -= Time.deltaTime;
            if (barkPenaltyTimer <= 0f)
            {
                barkPenaltyTimer = 0f;
                barkCount = 0;
            }
        }
        else if (barkWindowTimer > 0f)
        {
            barkWindowTimer -= Time.deltaTime;
            if (barkWindowTimer <= 0f)
            {
                barkWindowTimer = 0f;
                barkCount = 0;
            }
        }
    }

    // GỌI HÀM NÀY KHI CHÓ BỊ TRÚNG ĐÒN
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        UpdateHealthUI();

        if (audioSource != null && yelpClip != null)
        {
            audioSource.PlayOneShot(yelpClip); // Kêu ăng ẳng khi dính dame
        }

        if (currentHealth <= 0)
        {
            Debug.Log("Chó hết máu!");
            // Bạn có thể thêm xử lý thua game hoặc hồi sinh tại đây
        }
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    void UpdateCursorLock()
    {
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}