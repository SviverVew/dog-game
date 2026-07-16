using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class Player : MonoBehaviour
    {
        [Header("References")]
        private Rigidbody rb;
        private Animator _animator;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Transform mainCameraTransform;
        private AudioSource audioSource;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif

        [Header("Movement")]
        [Tooltip("Tốc độ đi bộ của chú chó")]
        public float moveSpeed = 3.0f;
        [Tooltip("Tốc độ chạy nhanh của chú chó")]
        public float sprintSpeed = 6.0f;
        [Tooltip("Tốc độ xoay đầu")]
        public float rotationSpeed = 15.0f; 
        private float currentSpeed;
        private Vector3 moveDirection;

        [Header("Jumping & Gravity")]
        [Tooltip("Lực nhảy nhảy")]
        public float jumpForce = 6.0f;
        [Tooltip("Trọng lực cộng thêm khi rơi tự do")]
        public float extraGravity = 15.0f; 
        [Tooltip("Lớp mặt đất")]
        public LayerMask groundLayer;
        private bool isGrounded;

        [Header("Sprint System with Cooldown")]
        public float sprintDuration = 3f;
        public float sprintCooldown = 5f;
        private bool isSprinting = false;
        private float sprintTimeRemaining;
        private float sprintCooldownRemaining;

        [Header("Cinemachine & Camera")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private const float _threshold = 0.01f;

        [Header("Audio Clips")]
        public AudioClip barkClip;
        public AudioClip yelpClip;
        public AudioClip landingAudioClip;
        public AudioClip[] footstepAudioClips;
        [Range(0, 1)] public float footstepAudioVolume = 0.5f;

        [Header("Combat & Stats")]
        public int maxHealth = 100;
        private int currentHealth;
        public Slider healthBar;
        
        public float biteDuration = 0.5f;
        public float biteCooldown = 1.0f;
        private bool canBite = true;

        [Header("Bark Anti-Spam System")]
        public int barkLimit = 3;
        public float barkWindow = 5f;
        public float barkPenaltyCooldown = 5f;
        private int barkCount = 0;
        private float barkWindowTimer = 0f;
        private float barkPenaltyTimer = 0f;

        // Animation Parameter IDs (Khớp tên tham số được đặt trong Animator của bạn)
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDEatingTrigger; // Ví dụ kích hoạt chuỗi Ăn
        private int _animIDAngryTrigger;  // Kích hoạt chuỗi Tức giận/Sủa

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            if (_mainCamera != null)
            {
                mainCameraTransform = _mainCamera.transform;
            }
        }

        private void Start()
        {
            // Lấy Animator từ con hoặc chính nó
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            _hasAnimator = _animator != null;

            rb = GetComponent<Rigidbody>();
            _input = GetComponent<StarterAssetsInputs>();
            audioSource = GetComponent<AudioSource>();

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }

            AssignAnimationIDs();

            if (groundLayer == 0) groundLayer = ~0;
            sprintTimeRemaining = sprintDuration;
            sprintCooldownRemaining = 0f;
            currentHealth = maxHealth;
            UpdateHealthUI();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            _hasAnimator = _animator != null;
            isGrounded = CheckGrounded();

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

            if (_input.jump && isGrounded)
            {
                Jump();
            }

            // Đồng bộ trạng thái mặt đất vào Animator
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, isGrounded);
                _animator.SetBool(_animIDFreeFall, rb.linearVelocity.y < -0.1f && !isGrounded);
            }

            UpdateSprint(moveHorizontal, moveForward);
            UpdateBarkTimers();
            UpdateActions();
            UpdateCursorLock();
        }

        private void FixedUpdate()
        {
            MovePlayer();
            ApplyExtraGravity(); 
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            // Thay đổi các chuỗi ký tự này trùng khớp với tên Parameter bạn tạo bên cửa sổ Parameters của Animator
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDEatingTrigger = Animator.StringToHash("Eat"); // Trigger chuyển đến EatingStart
            _animIDAngryTrigger = Animator.StringToHash("Bark"); // Trigger chuyển đến AngryStart
        }

        private void MovePlayer()
        {
            float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
            if (moveDirection == Vector3.zero) targetSpeed = 0f;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.fixedDeltaTime * 15f);

            Vector3 velocity = moveDirection * currentSpeed;
            rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }

            // Đồng bộ tốc độ với blend tree / trạng thái di chuyển của bạn
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, currentSpeed);
            }
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            _input.jump = false; 

            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDJump);
            }
        }

        private void ApplyExtraGravity()
        {
            if (!isGrounded)
            {
                rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            }
        }

        private bool CheckGrounded()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            Vector3 origin = transform.position + Vector3.up * (capsule.radius);
            float radius = capsule.radius * 0.9f; 
            float castDistance = capsule.radius + 0.1f;

            return Physics.SphereCast(origin, radius, Vector3.down, out _, castDistance, groundLayer, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (CinemachineCameraTarget == null) return;

            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void UpdateSprint(float h, float v)
        {
            bool wantSprint = _input.sprint;
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

        private void UpdateActions()
        {
            // Nhấn phím Q để Cắn (Đồng bộ với chuỗi hoạt ảnh Eating)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Bite();
            }

            // Nhấn phím R để Sủa (Đồng bộ với chuỗi hoạt ảnh Angry)
            if (Input.GetKeyDown(KeyCode.R))
            {
                Bark();
            }
        }

        private void Bite()
        {
            if (!canBite) return;
            canBite = false;
            
            if (_hasAnimator)
            {
                // Kích hoạt Trigger "Eat" để bắt đầu chuyển trạng thái None -> EatingStart
                _animator.SetTrigger(_animIDEatingTrigger);
            }

            Debug.Log("Cắn! (Q)");
            StartCoroutine(BiteCooldown());
        }

        private IEnumerator BiteCooldown()
        {
            yield return new WaitForSeconds(biteDuration);
            yield return new WaitForSeconds(biteCooldown - biteDuration);
            canBite = true;
        }

        private void Bark()
        {
            if (barkPenaltyTimer > 0f)
            {
                Debug.Log("Bark bị chặn: Đang trong thời gian phạt.");
                return;
            }

            if (barkCount == 0)
            {
                barkWindowTimer = barkWindow;
            }

            if (barkCount >= barkLimit)
            {
                barkPenaltyTimer = barkPenaltyCooldown;
                return;
            }

            // Kích hoạt Trigger "Bark" để bắt đầu chuyển trạng thái None -> AngryStart
            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDAngryTrigger);
            }

            if (audioSource != null && barkClip != null)
            {
                audioSource.PlayOneShot(barkClip);
            }

            barkCount++;
            Debug.Log("Sủa! (R) " + barkCount + "/" + barkLimit);

            if (barkCount >= barkLimit)
            {
                barkPenaltyTimer = barkPenaltyCooldown;
                barkWindowTimer = 0f;
            }
        }

        private void UpdateBarkTimers()
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

        public void TakeDamage(int amount)
        {
            if (currentHealth <= 0) return;

            currentHealth = Mathf.Max(currentHealth - amount, 0);
            UpdateHealthUI();

            if (audioSource != null && yelpClip != null)
            {
                audioSource.PlayOneShot(yelpClip);
            }

            if (currentHealth <= 0)
            {
                Debug.Log("Chó hết máu!");
            }
        }

        private void UpdateHealthUI()
        {
            if (healthBar != null)
            {
                healthBar.maxValue = maxHealth;
                healthBar.value = currentHealth;
            }
        }

        private void UpdateCursorLock()
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

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = isGrounded ? transparentGreen : transparentRed;

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Vector3 origin = transform.position + Vector3.up * (capsule.radius);
                Gizmos.DrawWireSphere(origin, capsule.radius * 0.9f);
                Gizmos.DrawLine(origin, origin + Vector3.down * (capsule.radius + 0.1f));
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && footstepAudioClips != null && footstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, footstepAudioClips.Length);
                audioSource.PlayOneShot(footstepAudioClips[index], footstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && landingAudioClip != null)
            {
                audioSource.PlayOneShot(landingAudioClip, footstepAudioVolume);
            }
        }
    }
}