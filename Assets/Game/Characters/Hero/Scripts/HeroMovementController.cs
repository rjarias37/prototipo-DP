using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class HeroMovementController : MonoBehaviour
{
    private enum FacingMode { MoveDirection, CameraYaw }

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference move;   // Vector2
    [SerializeField] private InputActionReference sprint; // Button
    [SerializeField] private InputActionReference jump;   // Button

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float airControlMultiplier = 0.85f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Orientación del Mesh")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float turnLerp = 18f;
    [SerializeField] private FacingMode facing = FacingMode.CameraYaw; // mirar a la cámara por defecto
    [SerializeField] private bool alignWithCameraWhenIdle = true;      // solo para MoveDirection
    [SerializeField] private bool snapMeshToCameraWhenIdle = true;
    [SerializeField, Range(1f, 15f)] private float idleSnapAngle = 5f;

    [Header("Animación (opcional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isJumpingParam = "isJumping";

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isRunning;
    private bool wantJump;
    private bool isDead;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsJumpingHash = Animator.StringToHash("isJumping");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        var cc = GetComponent<CharacterController>();
        if (cc && cc.enabled) Debug.LogWarning("Desactiva o elimina el CharacterController: este script usa Rigidbody.");

        var health = GetComponent<Health>();
        if (health != null) health.onDeath.AddListener(OnDied);
        if (animator) animator.applyRootMotion = false;
    }

    void OnEnable()
    {
        move?.action.Enable();
        sprint?.action.Enable();
        jump?.action.Enable();

        if (sprint != null)
        {
            sprint.action.performed += OnSprintPerformed;
            sprint.action.canceled += OnSprintCanceled;
        }
        if (jump != null) jump.action.performed += OnJumpPerformed;
    }

    void OnDisable()
    {
        if (sprint != null)
        {
            sprint.action.performed -= OnSprintPerformed;
            sprint.action.canceled -= OnSprintCanceled;
        }
        if (jump != null) jump.action.performed -= OnJumpPerformed;

        move?.action.Disable();
        sprint?.action.Disable();
        jump?.action.Disable();
    }

    void Update()
    {
        if (isDead) return;
        moveInput = move?.action?.ReadValue<Vector2>() ?? Vector2.zero;

        if (animator && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(SpeedHash, moveInput.magnitude * (isRunning ? 2f : 1f));
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Transform cam = Camera.main ? Camera.main.transform : transform;
        Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();

        Vector3 moveDir = (right * moveInput.x + fwd * moveInput.y);
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        bool grounded = IsGrounded();
        float control = grounded ? 1f : airControlMultiplier;

        Vector3 v = rb.linearVelocity;
        v.x = moveDir.x * targetSpeed * control;
        v.z = moveDir.z * targetSpeed * control;

        if (wantJump && grounded)
        {
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (animator && !string.IsNullOrEmpty(isJumpingParam)) animator.SetBool(IsJumpingHash, true);
            wantJump = false;
        }
        else rb.linearVelocity = v;

        if (animator && animator.GetBool(IsJumpingHash) && grounded && rb.linearVelocity.y <= 0.01f)
            animator.SetBool(IsJumpingHash, false);

        if (modelTransform)
        {
            Vector3 faceDir;
            if (facing == FacingMode.CameraYaw)
            {
                faceDir = fwd;
                if (snapMeshToCameraWhenIdle && moveInput.sqrMagnitude < 0.0004f)
                {
                    float delta = Mathf.Abs(Mathf.DeltaAngle(modelTransform.eulerAngles.y, cam.eulerAngles.y));
                    if (delta <= idleSnapAngle)
                    {
                        modelTransform.rotation = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
                        return;
                    }
                }
            }
            else
            {
                faceDir = moveDir.sqrMagnitude > 0.0001f
                    ? moveDir
                    : (alignWithCameraWhenIdle ? fwd : modelTransform.forward);
            }

            if (faceDir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(faceDir, Vector3.up);
                modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, look, Time.fixedDeltaTime * turnLerp);
            }
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = groundCheck ? groundCheck.position : transform.position + Vector3.up * 0.1f;
        return Physics.CheckSphere(origin, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    void OnSprintPerformed(InputAction.CallbackContext _) => isRunning = true;
    void OnSprintCanceled(InputAction.CallbackContext _) => isRunning = false;
    void OnJumpPerformed(InputAction.CallbackContext _) => wantJump = true;

    void OnDied()
    {
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        enabled = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
#endif
}
