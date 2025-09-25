using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform; // arraste a câmera principal
    public CharacterController controller;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 6.0f;
    public float acceleration = 10f;
    public float rotateSmoothTime = 0.12f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Crouch")]
    public float crouchHeight = 1.0f;
    public float standHeight = 2.0f;
    public float crouchSpeedMultiplier = 0.5f;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Aiming")]
    public KeyCode aimKey = KeyCode.Mouse1;
    public float aimMoveMultiplier = 0.5f;

    // Internals
    private float currentSpeed;
    private float speedVelocity;
    private float turnSmoothVelocity;
    private Vector3 velocity; // for gravity & jump
    private bool isGrounded;
    private bool isCrouched = false;
    private float verticalVelocityRef;

    // Animator parameter IDs (optional optimization)
    private int hashSpeed = Animator.StringToHash("Speed");
    private int hashMoveX = Animator.StringToHash("MoveX");
    private int hashMoveY = Animator.StringToHash("MoveY");
    private int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private int hashIsCrouched = Animator.StringToHash("IsCrouched");
    private int hashIsAiming = Animator.StringToHash("IsAiming");
    private int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main ? Camera.main.transform : null;
    }

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();

        currentSpeed = walkSpeed;
        if (groundCheck == null)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = Vector3.zero + Vector3.up * 0.1f;
        }
    }

    void Update()
    {
        HandleGround();
        HandleCrouch();
        HandleMovement();
        HandleGravityAndJump();
        UpdateAnimator();
    }

    void HandleGround()
    {
        // ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // small downward force to keep grounded
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouched = !isCrouched;
            controller.height = isCrouched ? crouchHeight : standHeight;
            // adjust center to remain on ground
            controller.center = new Vector3(0, controller.height / 2f, 0);
        }
    }

    void HandleMovement()
    {
        // Input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or left/right
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or up/down
        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;

        bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool aiming = Input.GetKey(aimKey);

        float targetSpeed = running ? runSpeed : walkSpeed;
        if (isCrouched) targetSpeed *= crouchSpeedMultiplier;
        if (aiming) targetSpeed *= aimMoveMultiplier;

        // Smooth speed change
        float flatInputMag = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);
        float desiredSpeed = targetSpeed * flatInputMag;
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        if (inputDir.magnitude >= 0.01f)
        {
            // relative to camera
            Vector3 camForward = cameraTransform ? Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;

            Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            // rotate towards movement direction (only when not aiming heavily)
            if (!aiming)
            {
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotateSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            else
            {
                // when aiming, character should rotate to face camera forward (optional)
                Vector3 aimDir = cameraTransform ? new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized : transform.forward;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir), 10f * Time.deltaTime);
            }

            Vector3 displacement = moveDir * currentSpeed * Time.deltaTime;
            controller.Move(displacement);
        }
    }

    void HandleGravityAndJump()
    {
        bool jumpPressed = Input.GetButtonDown("Jump"); // space by default

        if (isGrounded && jumpPressed)
        {
            // v = sqrt(2 * g * h) but gravity is negative
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        // drive animator parameters
        Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);
        float forward = Mathf.Clamp(localVelocity.z / runSpeed, -1f, 1f);
        float right = Mathf.Clamp(localVelocity.x / runSpeed, -1f, 1f);

        animator.SetFloat(hashSpeed, controller.velocity.magnitude);
        animator.SetFloat(hashMoveX, right, 0.1f, Time.deltaTime);
        animator.SetFloat(hashMoveY, forward, 0.1f, Time.deltaTime);
        animator.SetBool(hashIsGrounded, isGrounded);
        animator.SetBool(hashIsCrouched, isCrouched);
        animator.SetBool(hashIsAiming, Input.GetKey(aimKey));
        animator.SetFloat(hashVerticalVelocity, velocity.y);
    }

    // debug visuals
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}

