using UnityEngine;
using StarterAssets; // Thêm dòng này để nhận diện class ThirdPersonController mới

public class AiPerson : MonoBehaviour
{
    [Header("References")]
    public Player player; // Đổi từ Player thành ThirdPersonController
    public Transform homeAreaCenter;
    public Vector3 homeAreaSize = new Vector3(10f, 0f, 10f);
    public float wanderSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float detectionRadius = 8f;
    public float attackRadius = 1.8f;
    public float throwRadius = 5f;
    public float attackCooldown = 2f;
    public float throwForce = 8f;
    public GameObject slipperPrefab;
    public int maxThrownSlippers = 2;
    public LayerMask slipperLayer;
    public Transform throwOrigin;
    public float wanderTargetThreshold = 0.5f;
    public float detectionCheckInterval = 0.5f;

    private Rigidbody rb;
    private Vector3 wanderTarget;
    private float attackTimer;
    private float detectionTimer;
    private bool chasing = false;
    private int slippersRemaining;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        if (player == null)
            player = FindObjectOfType<Player>(); // Đổi từ Player thành ThirdPersonController

        if (player == null)
            Debug.LogWarning("AiPerson: không tìm thấy ThirdPersonController trong scene.");

        slippersRemaining = maxThrownSlippers;
        ChooseWanderTarget();
        detectionTimer = 0f;

        if (throwOrigin == null)
            throwOrigin = transform;
    }

    void Update()
    {
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionCheckInterval;
            EvaluateDetection();
        }

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (chasing)
        {
            Vector3 targetPosition = player.transform.position;
            MoveToward(targetPosition, chaseSpeed);

            if (distanceToPlayer <= attackRadius)
            {
                TryAttack();
            }
            else if (slippersRemaining > 0 && distanceToPlayer <= throwRadius)
            {
                TryThrowSlipper();
            }
        }
        else
        {
            MoveToward(wanderTarget, wanderSpeed);
            if (Vector3.Distance(transform.position, wanderTarget) <= wanderTargetThreshold)
            {
                ChooseWanderTarget();
            }
        }

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    void EvaluateDetection()
    {
        if (player == null)
            return;

        bool playerHasSlipper = false;
        DogAction dogAction = player.GetComponent<DogAction>();
        if (dogAction != null)
            playerHasSlipper = dogAction.IsHoldingSlipper;

        bool nearbySlipper = false;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, slipperLayer);
        foreach (Collider hit in hits)
        {
            Slipper slipper = hit.GetComponent<Slipper>();
            if (slipper != null && !slipper.isPickedUp)
            {
                nearbySlipper = true;
                break;
            }
        }

        if (playerHasSlipper || nearbySlipper || Vector3.Distance(transform.position, player.transform.position) <= detectionRadius)
        {
            chasing = true;
        }
        else
        {
            chasing = false;
        }
    }

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Vector3 velocity = direction.normalized * speed;
        if (rb != null)
        {
            Vector3 newVelocity = rb.linearVelocity;
            newVelocity.x = velocity.x;
            newVelocity.z = velocity.z;
            rb.linearVelocity = newVelocity;
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
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
            : transform.position + randomOffset;
    }

    void TryAttack()
    {
        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;
        if (player != null)
        {
            player.TakeDamage(10); // Gọi hàm TakeDamage nhận sát thương từ code của bạn
            Debug.Log("Person đánh trúng chó và trừ máu.");
        }
    }

    void TryThrowSlipper()
    {
        if (slippersRemaining <= 0)
            return;

        if (slipperPrefab == null)
        {
            Debug.LogWarning("AiPerson: slipperPrefab chưa gán.");
            return;
        }

        slippersRemaining--;
        Vector3 spawnPoint = throwOrigin.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        GameObject thrown = Instantiate(slipperPrefab, spawnPoint, Quaternion.identity);
        Slipper slipper = thrown.GetComponent<Slipper>();
        if (slipper == null)
            slipper = thrown.AddComponent<Slipper>();
        slipper.isPickedUp = false;

        Rigidbody thrownRb = thrown.GetComponent<Rigidbody>();
        if (thrownRb == null)
            thrownRb = thrown.AddComponent<Rigidbody>();

        Vector3 throwDirection = (player.transform.position + Vector3.up * 0.5f - spawnPoint).normalized;
        thrownRb.linearVelocity = throwDirection * throwForce + Vector3.up * 2f;

        Collider thrownCollider = thrown.GetComponent<Collider>();
        if (thrownCollider == null)
            thrownCollider = thrown.AddComponent<SphereCollider>();

        Debug.Log("Person đã ném dép.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = homeAreaCenter != null ? homeAreaCenter.position : transform.position;
        Gizmos.DrawWireCube(center, homeAreaSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, throwRadius);
    }
}