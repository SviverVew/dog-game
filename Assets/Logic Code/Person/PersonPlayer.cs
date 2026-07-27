using System.Collections;
using Unity.Netcode;
using UnityEngine;
using StarterAssets;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PersonPlayer : NetworkBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    [SerializeField] private Animator animator;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;
    private Transform mainCameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 12.0f;
    private float currentSpeed;
    private Vector3 moveDirection;

    [Header("Combat & Slipper Action")]
    public Transform throwOrigin;      // Vị trí ném / Cầm dép trên tay
    public GameObject slipperPrefab;   // Prefab dép ném
    public float attackRadius = 1.8f;  // Tầm đánh cận chiến
    public float throwForce = 12.0f;
    public int punchDamage = 15;
    public float attackCooldown = 1.0f;
    private float attackTimer;

    private Slipper heldSlipper;       // Chiếc dép đang cầm trên tay

    // Animation IDs (Đồng bộ với Animator StarterAssets)
    private int animIDSpeed;
    private int animIDMotionSpeed;
    private int animIDFightTrigger;

    private bool _hasAnimator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _input = GetComponent<StarterAssetsInputs>();
        
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        if (_mainCamera != null)
        {
            mainCameraTransform = _mainCamera.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Nếu KHÔNG PHẢI là máy local sở hữu người chơi này -> Tắt AudioListener thừa
        if (!IsOwner)
        {
            AudioListener audioListener = GetComponent<AudioListener>();
            if (audioListener != null) audioListener.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _hasAnimator = animator != null;

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (throwOrigin == null) throwOrigin = transform;

        AssignAnimationIDs();
    }

    private void Update()
    {
        // RẤT QUAN TRỌNG: Chỉ xử lý bấm phím ở máy của người chơi này
        if (!IsOwner) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        HandleInputAndMovementDirection();
        HandleActions();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDFightTrigger = Animator.StringToHash("Fight");
    }

    // --- DI CHUYỂN TƯƠNG THÍCH CAMERA CINEMACHINE ---
    private void HandleInputAndMovementDirection()
    {
        if (_input == null) return;

        float moveHorizontal = _input.move.x;
        float moveForward = _input.move.y;

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
    }

    private void MovePlayer()
    {
        bool hasMovementInput = moveDirection.sqrMagnitude > 0.001f;
        // Giữ Phím Sprint (Shift) để Chạy Nhanh, thả ra thì Đi Bộ
        float targetSpeed = _input.sprint ? runSpeed : walkSpeed;

        if (!hasMovementInput)
        {
            targetSpeed = 0f;
            currentSpeed = 0f;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.fixedDeltaTime * 15f);
        }

        // Cập nhật Vận tốc Rigidbody
        Vector3 velocity = moveDirection * currentSpeed;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
#else
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
#endif

        // Xoay nhân vật theo hướng di chuyển
        if (hasMovementInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Cập nhật Animation Speed (Blend Tree)
        if (_hasAnimator)
        {
            animator.SetFloat(animIDSpeed, currentSpeed);
            animator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    // --- XỬ LÝ HÀNH ĐỘNG (ĐÁNH, NHẶT, NÉM DÉP) ---
    private void HandleActions()
    {
        // 1. Chuột trái / Phím F: Đánh cận chiến (Punch / Strike)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
        {
            MeleeAttack();
        }

        // 2. Phím E: Nhặt dép rơi dưới đất
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldSlipper == null) TryPickUpSlipper();
        }

        // 3. Phím G: Ném dép (Nếu đang cầm dép)
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (heldSlipper != null) ThrowSlipper();
        }
    }

    private void MeleeAttack()
    {
        if (attackTimer > 0f) return;
        attackTimer = attackCooldown;

        if (_hasAnimator) animator.SetTrigger(animIDFightTrigger);

        // Quét tìm xem có chú Chó (`Player`) nào trong tầm đánh không
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 0.8f, attackRadius);
        foreach (var col in hitColliders)
        {
            Player dogPlayer = col.GetComponent<Player>();
            if (dogPlayer != null)
            {
                Debug.Log("Người đã đánh trúng Chó!");
                // Gửi lệnh lên Server trừ máu chú chó
                NetworkObject dogNetObj = dogPlayer.GetComponent<NetworkObject>();
                if (dogNetObj != null)
                {
                    DealDamageServerRpc(dogNetObj.NetworkObjectId, punchDamage);
                }
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong dogNetworkId, int damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(dogNetworkId, out NetworkObject netObj))
        {
            Player dog = netObj.GetComponent<Player>();
            if (dog != null)
            {
                dog.TakeDamage(damage);
            }
        }
    }

    private void TryPickUpSlipper()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2.0f);
        foreach (Collider col in colliders)
        {
            Slipper slipper = col.GetComponent<Slipper>();
            if (slipper != null && !slipper.isPickedUp)
            {
                NetworkObject netObj = slipper.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    PickUpSlipperServerRpc(netObj.NetworkObjectId);
                    break;
                }
            }
        }
    }

    [ServerRpc]
    private void PickUpSlipperServerRpc(ulong slipperId)
    {
        PickUpSlipperClientRpc(slipperId);
    }

    [ClientRpc]
    private void PickUpSlipperClientRpc(ulong slipperId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(slipperId, out NetworkObject netObj))
        {
            heldSlipper = netObj.GetComponent<Slipper>();
            if (heldSlipper != null)
            {
                heldSlipper.isPickedUp = true;
                heldSlipper.GetComponent<Collider>().enabled = false;
                
                Rigidbody slipperRb = heldSlipper.GetComponent<Rigidbody>();
                if (slipperRb) slipperRb.isKinematic = true;

                heldSlipper.transform.SetParent(throwOrigin);
                heldSlipper.transform.localPosition = Vector3.zero;
                heldSlipper.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void ThrowSlipper()
    {
        if (heldSlipper == null) return;

        NetworkObject netObj = heldSlipper.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            ThrowSlipperServerRpc(netObj.NetworkObjectId, transform.forward);
        }
    }

    [ServerRpc]
    private void ThrowSlipperServerRpc(ulong slipperId, Vector3 throwDirection)
    {
        ThrowSlipperClientRpc(slipperId, throwDirection);
    }

    [ClientRpc]
    private void ThrowSlipperClientRpc(ulong slipperId, Vector3 throwDirection)
    {
        if (heldSlipper != null)
        {
            heldSlipper.transform.SetParent(null);
            heldSlipper.isPickedUp = false;

            Rigidbody slipperRb = heldSlipper.GetComponent<Rigidbody>();
            if (slipperRb)
            {
                slipperRb.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
                slipperRb.linearVelocity = Vector3.zero;
#else
                slipperRb.velocity = Vector3.zero;
#endif
                slipperRb.AddForce(throwDirection * throwForce + Vector3.up * 2f, ForceMode.Impulse);
            }

            heldSlipper.GetComponent<Collider>().enabled = true;
            heldSlipper = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.8f, attackRadius);
    }
}