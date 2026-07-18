using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Giả sử DogAction và Player, Slipper đã tồn tại như code cũ của bạn.
// Nếu không, bạn cần comment các dòng liên quan đến chúng để test di chuyển trước.

public class AiPerson : MonoBehaviour
{
    // Các trạng thái của AI
    public enum AIState { Wandering, LookingAtTree, Chasing }

    [Header("Current State")]
    [SerializeField] private AIState currentState = AIState.Wandering;

    [Header("References")]
    public Player player; // Đối tượng chó
    public Transform homeAreaCenter;
    public Vector3 homeAreaSize = new Vector3(20f, 0f, 20f); // Bán kính 10m
    public Transform throwOrigin;

    [Header("Speeds")]
    public float walkSpeed = 1.5f; // Tốc độ đi vòng vòng (Walk anim)
    public float runSpeed = 4.0f;  // Tốc độ rượt chó (Run anim)
    public float rotationSpeed = 10f;

    [Header("Detection & Logic")]
    public float detectionRadius = 8f;
    public LayerMask slipperLayer;
    public LayerMask treeLayer; // Layer chứa các object Cây
    public float detectionCheckInterval = 0.5f;
    [Range(0f, 1f)] public float lookAtTreeChance = 0.3f; // Xác suất dừng lại ngắm cây khi thấy (0-1)

    [Header("Wander Settings")]
    public float wanderTargetThreshold = 0.5f;
    public float minIdleTime = 1f; // Thời gian đứng yên tối thiểu tại target
    public float maxIdleTime = 3f; // Thời gian đứng yên tối đa tại target

    [Header("Look at Tree Settings")]
    public float minLookTime = 2f;
    public float maxLookTime = 5f;

    [Header("Combat Settings")]
    public float attackRadius = 1.8f;
    public float throwRadius = 5f;
    public float attackCooldown = 2f;
    public float throwForce = 8f;
    public GameObject slipperPrefab;
    public int maxThrownSlippers = 2;

    // Phụ trợ
    private Rigidbody rb;
    private Animator animator;
    private Vector3 wanderTarget;
    private float attackTimer;
    private float detectionTimer;
    private int slippersRemaining;
    private Vector3 spawnPosition;
    private bool isIdleAtTarget = false;
    private Coroutine currentBehaviorCoroutine;
    private Transform currentTreeTarget;

    // Animation IDs
    private int animIDSpeed;
    private int animIDMotionSpeed;
    private int animIDFightTrigger;
    // GIỮ LẠI TRIGGER CŨ (Dù logic mới dùng Speed Float)
    private int animIDRunTrigger;
    private int animIDIdleTrigger;

    // Các biến phụ trợ cho Animation ThirdPerson gốc
    private bool _hasAnimator;

    void Start()
    {
        _hasAnimator = TryGetComponent(out animator);
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        // Đảm bảo Rigidbody không bị xoay bởi vật lý
        rb.freezeRotation = true;

        AssignAnimationIDs();

        spawnPosition = transform.position;

        if (player == null)
            player = FindObjectOfType<Player>();

        if (player == null)
            Debug.LogWarning("AiPerson: không tìm thấy Player (Chó) trong scene.");

        slippersRemaining = maxThrownSlippers;
        throwOrigin = throwOrigin == null ? transform : throwOrigin;

        // Bắt đầu trạng thái mặc định
        EnterState(AIState.Wandering);
    }

    void Update()
    {
        // 1. Giảm Timer
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // 2. Kiểm tra điều kiện chuyển trạng thái sang CHASSING (luôn ưu tiên)
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionCheckInterval;
            if (ShouldStartChasing())
            {
                if (currentState != AIState.Chasing)
                {
                    EnterState(AIState.Chasing);
                }
            }
            else
            {
                // Nếu không cần đuổi nữa và đang đuổi, quay về đi dạo
                if (currentState == AIState.Chasing)
                {
                    EnterState(AIState.Wandering);
                }
            }
        }

        // 3. Thực thi logic trạng thái hiện tại
        UpdateStateLogics();
    }

    // --- QUẢN LÝ TRẠNG THÁI (FSM) ---

    void EnterState(AIState newState)
    {
        // Thoát khỏi trạng thái cũ (dọn dẹp Coroutine nếu có)
        if (currentBehaviorCoroutine != null)
        {
            StopCoroutine(currentBehaviorCoroutine);
            currentBehaviorCoroutine = null;
        }
        StopMoving(); // Đảm bảo dừng lại khi chuyển bang

        currentState = newState;
        currentTreeTarget = null;
        isIdleAtTarget = false;

        // Vào trạng thái mới
        switch (currentState)
        {
            case AIState.Wandering:
                ChooseWanderTarget();
                UpdateMovementAnimation(0f); // Bắt đầu bằng Idle
                break;
            case AIState.LookingAtTree:
                // Cần currentTreeTarget được gán trước khi vào đây
                UpdateMovementAnimation(0f); // Đứng yên ngắm (Idle)
                currentBehaviorCoroutine = StartCoroutine(LookAtTreeRoutine());
                break;
            case AIState.Chasing:
                UpdateMovementAnimation(runSpeed); // Chạy
                break;
        }
    }

    void UpdateStateLogics()
    {
        if (player == null) return;

        switch (currentState)
        {
            case AIState.Wandering:
                LogicWandering();
                break;
            case AIState.LookingAtTree:
                LogicLookingAtTree();
                break;
            case AIState.Chasing:
                LogicChasing();
                break;
        }
    }

    // --- LOGIC CHI TIẾT CÁC TRẠNG THÁI ---

    // 1. WANDERING (Đi dạo vô vơ)
    void LogicWandering()
    {
        if (isIdleAtTarget) return; // Đang đứng chơi tại target, không làm gì cả

        // Kiểm tra xem có cây nào gần không để "ngẫu nhiên" dừng lại ngắm
        if (Random.value < lookAtTreeChance * Time.deltaTime) // Kiểm tra mỗi frame với xác suất thấp
        {
            Transform nearbyTree = CheckForNearbyTree();
            if (nearbyTree != null)
            {
                currentTreeTarget = nearbyTree;
                EnterState(AIState.LookingAtTree);
                return;
            }
        }

        // Di chuyển tới điểm ngẫu nhiên
        MoveToward(wanderTarget, walkSpeed);

        // Đến nơi
        if (Vector3.Distance(transform.position, wanderTarget) <= wanderTargetThreshold)
        {
            currentBehaviorCoroutine = StartCoroutine(IdleAtWanderTargetRoutine());
        }
    }

    // 2. LOOKING AT TREE (Đứng lại ngắm cây)
    void LogicLookingAtTree()
    {
        // Chỉ cần xoay mặt về phía cây, animation Idle đã chạy lúc EnterState
        if (currentTreeTarget != null)
        {
            Vector3 lookDirection = (currentTreeTarget.position - transform.position);
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    // 3. CHASING (Rượt chó)
    void LogicChasing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        MoveToward(player.transform.position, runSpeed);

        // Tấn công hoặc ném dép
        if (distanceToPlayer <= attackRadius)
        {
            StopMoving(); // Dừng lại để đánh
            TryAttack();
        }
        else if (slippersRemaining > 0 && distanceToPlayer <= throwRadius)
        {
            // Có thể thêm logic dừng lại 0.5s để ném dép ở đây nếu muốn anim đẹp hơn
            TryThrowSlipper();
        }
    }

    // --- CÁC ROUTINE PHỤ TRỢ (Dùng Coroutine để quản lý thời gian đứng yên) ---

    IEnumerator IdleAtWanderTargetRoutine()
    {
        isIdleAtTarget = true;
        StopMoving(); // Chạy animation Idle
        float waitTime = Random.Range(minIdleTime, maxIdleTime);
        yield return new WaitForSeconds(waitTime);
        isIdleAtTarget = false;
        ChooseWanderTarget();
        UpdateMovementAnimation(walkSpeed); // Quay lại đi bộ
    }

    IEnumerator LookAtTreeRoutine()
    {
        // Logic xoay mặt được xử lý trong Update -> LogicLookingAtTree
        float lookTime = Random.Range(minLookTime, maxLookTime);
        yield return new WaitForSeconds(lookTime);
        // Ngắm xong, quay lại đi dạo
        EnterState(AIState.Wandering);
    }

    // --- HỆ THỐNG DI CHUYỂN & ANIMATION ---

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            StopMoving();
            return;
        }

        // Di chuyển bằng Rigidbody (giống code cũ nhưng sửa linearVelocity thành velocity cho bản Unity cũ hơn nếu cần)
        Vector3 velocity = direction.normalized * speed;
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 newVelocity = rb.linearVelocity;
#else
            Vector3 newVelocity = rb.velocity;
#endif
            newVelocity.x = velocity.x;
            newVelocity.z = velocity.z;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = newVelocity;
#else
            rb.velocity = newVelocity;
#endif
        }
        else
        {
            // Fallback nếu không có Rigidbody
            transform.position += velocity * Time.deltaTime;
        }

        // Xoay
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);

        // Cập nhật Animation dựa trên tốc độ
        UpdateMovementAnimation(speed);
    }

    void StopMoving()
    {
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 newVelocity = rb.linearVelocity;
#else
            Vector3 newVelocity = rb.velocity;
#endif
            newVelocity.x = 0f;
            newVelocity.z = 0f;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = newVelocity;
#else
            rb.velocity = newVelocity;
#endif
        }

        UpdateMovementAnimation(0f); // Tốc độ 0 -> Idle
    }

    void UpdateMovementAnimation(float speed)
    {
        if (!_hasAnimator) return;

        // Cập nhật biến Speed Float cho Blend Tree (Walk/Run)
        // Dùng `speed` truyền vào để quyết định anim Walk hay Run
        animator.SetFloat(animIDSpeed, speed);

        // MotionSpeed thường dùng để nhân bản tốc độ anim, mặc định là 1
        animator.SetFloat(animIDMotionSpeed, 1f); 

        // --- CẬP NHẬT TRIGGER CŨ (Dể tương thích nếu bạn dùng Layer/Transition trigger) ---
        if (speed > 0.1f) // Đang di chuyển
        {
            animator.ResetTrigger(animIDIdleTrigger);
            if (speed > walkSpeed + 0.5f) // Đang chạy
            {
                // animator.SetTrigger(animIDRunTrigger); // Thường Run dùng blend tree, ít dùng trigger
            }
        }
        else // Đứng yên
        {
            animator.ResetTrigger(animIDRunTrigger);
            animator.SetTrigger(animIDIdleTrigger);
        }
    }

    // --- HỆ THỐNG PHÁT HIỆN ---

    bool ShouldStartChasing()
    {
        if (player == null) return false;

        // 1. Kiểm tra xem chó đã nhặt dép chưa
        bool playerHasSlipper = false;
        DogAction dogAction = player.GetComponent<DogAction>();
        if (dogAction != null)
        {
            playerHasSlipper = dogAction.IsHoldingSlipper;
        }

        // 2. Kiểm tra xem có chiếc dép nào nằm dưới đất không
        bool nearbySlipper = false;
        Collider[] hitsSlipper = Physics.OverlapSphere(transform.position, detectionRadius, slipperLayer);
        foreach (Collider hit in hitsSlipper)
        {
            Slipper slipper = hit.GetComponent<Slipper>();
            if (slipper != null && !slipper.isPickedUp)
            {
                nearbySlipper = true;
                break;
            }
        }

        // Logic cũ: Đuổi khi chó ngậm dép hoặc dép rơi gần
        return playerHasSlipper || nearbySlipper;
    }

    Transform CheckForNearbyTree()
    {
        Collider[] hitsTree = Physics.OverlapSphere(transform.position, detectionRadius, treeLayer);
        if (hitsTree.Length > 0)
        {
            // Trả về cái cây đầu tiên tìm thấy
            return hitsTree[0].transform;
        }
        return null;
    }

    void ChooseWanderTarget()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-homeAreaSize.x * 0.5f, homeAreaSize.x * 0.5f),
            0f,
            Random.Range(-homeAreaSize.z * 0.5f, homeAreaSize.z * 0.5f)
        );
        wanderTarget = homeAreaCenter != null
            ? homeAreaCenter.position + randomOffset
            : spawnPosition + randomOffset;
    }

    // --- CÁC HÀM CŨ KHÔNG ĐỔI ---

    void TryAttack()
    {
        if (attackTimer > 0f) return;
        attackTimer = attackCooldown;
        TriggerFightAnimation();
        if (player != null) { player.TakeDamage(10); Debug.Log("AI đánh trúng chó."); }
    }

    void TryThrowSlipper()
    {
        // Logic ném dép giữ nguyên từ code cũ...
        if (slippersRemaining <= 0 || slipperPrefab == null) return;
        slippersRemaining--;
        Vector3 spawnPoint = throwOrigin.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        GameObject thrown = Instantiate(slipperPrefab, spawnPoint, Quaternion.identity);
        Rigidbody thrownRb = thrown.GetComponent<Rigidbody>();
        if (thrownRb == null) thrownRb = thrown.AddComponent<Rigidbody>();
        Vector3 throwDirection = (player.transform.position + Vector3.up * 0.5f - spawnPoint).normalized;
        thrownRb.AddForce(throwDirection * throwForce + Vector3.up * 2f, ForceMode.VelocityChange);
        Debug.Log("AI đã ném dép.");
    }

    void AssignAnimationIDs()
    {
        // IDs cho Blend Tree ThirdPerson gốc
        animIDSpeed = Animator.StringToHash("Speed");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        // Giữ lại Trigger IDs cũ
        animIDFightTrigger = Animator.StringToHash("Fight");
        animIDRunTrigger = Animator.StringToHash("Run");
        animIDIdleTrigger = Animator.StringToHash("Idle");
    }

    void TriggerFightAnimation()
    {
        if (!_hasAnimator) return;
        animator.SetTrigger(animIDFightTrigger);
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ các vòng tròn detection
        Gizmos.color = Color.yellow;
        Vector3 center = homeAreaCenter != null ? homeAreaCenter.position : (Application.isPlaying ? spawnPosition : transform.position);
        Gizmos.DrawWireCube(center, homeAreaSize);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, throwRadius);
        
        if (currentTreeTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTreeTarget.position);
        }
    }
}