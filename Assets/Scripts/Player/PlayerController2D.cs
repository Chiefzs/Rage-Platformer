using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 7f;

    [Header("Jump Settings")]
    [SerializeField]
    private float jumpForce = 13f;

    [Tooltip(
        "Oyuncu zeminden ayrıldıktan sonra kaç saniye " +
        "daha zıplayabilir?"
    )]
    [SerializeField]
    private float coyoteTime = 0.12f;

    [Tooltip(
        "Oyuncu yere inmeden önce zıplamaya basarsa " +
        "input kaç saniye saklanır?"
    )]
    [SerializeField]
    private float jumpBufferTime = 0.12f;

    [Tooltip(
        "Jump tuşu erken bırakıldığında yukarı yönlü " +
        "hızın ne kadarı korunur?"
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float jumpCutMultiplier = 0.5f;

    [Header("Gravity Settings")]
    [SerializeField]
    private float baseGravityScale = 3f;

    [SerializeField]
    private float fallingGravityMultiplier = 1.5f;

    [Tooltip("Maksimum düşüş hızı. Negatif olmalıdır.")]
    [SerializeField]
    private float maxFallSpeed = -22f;

    [Header("Ground Check")]
    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private float groundCheckRadius = 0.12f;

    [SerializeField]
    private LayerMask groundLayer;

    [Header("Crouch Settings")]
    [SerializeField]
    private Vector2 standingColliderSize =
        new Vector2(1f, 1f);

    [SerializeField]
    private Vector2 standingColliderOffset =
        Vector2.zero;

    [SerializeField]
    private Vector2 crouchingColliderSize =
        new Vector2(1f, 0.55f);

    [SerializeField]
    private Vector2 crouchingColliderOffset =
        new Vector2(0f, -0.225f);

    [Tooltip(
        "Crouch sırasında normal hareket hızının " +
        "ne kadarı kullanılacak?"
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float crouchMoveSpeedMultiplier = 0.55f;

    [Header("Ceiling Check")]
    [Tooltip(
        "Ayağa kalkarken kontrol edilecek tavan bölgesinin merkezi."
    )]
    [SerializeField]
    private Transform ceilingCheck;

    [SerializeField]
    private Vector2 ceilingCheckSize =
        new Vector2(0.9f, 0.45f);

    [Header("Player Visual")]
    [Tooltip(
        "Crouch sırasında görsel olarak küçültülecek child nesne."
    )]
    [SerializeField]
    private Transform playerVisual;

    [Header("Debug")]
    [SerializeField]
    private bool drawGroundCheckGizmo = true;

    [SerializeField]
    private bool drawCeilingCheckGizmo = true;

    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    private PlayerInput playerInput;
    private PlayerInteraction playerInteraction;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;

    private Vector2 moveInput;

    private bool isGrounded;
    private bool wasGrounded;
    private bool controlsEnabled = true;

    private bool isCrouching;
    private bool crouchHeld;
    private bool interactionConsumedThisPress;

    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool jumpCutRequested;

    private Vector3 standingVisualScale;
    private Vector3 standingVisualLocalPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        playerInput = GetComponent<PlayerInput>();
        playerInteraction =
            GetComponent<PlayerInteraction>();

        rb.freezeRotation = true;
        rb.gravityScale = baseGravityScale;

        moveAction = playerInput.actions.FindAction(
            "Move",
            throwIfNotFound: true
        );

        jumpAction = playerInput.actions.FindAction(
            "Jump",
            throwIfNotFound: true
        );

        crouchAction = playerInput.actions.FindAction(
            "Crouch",
            throwIfNotFound: true
        );

        if (playerVisual != null)
        {
            standingVisualScale =
                playerVisual.localScale;

            standingVisualLocalPosition =
                playerVisual.localPosition;
        }

        SetCrouching(
            crouching: false,
            forceUpdate: true
        );
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            jumpCutRequested = false;
            crouchHeld = false;
            interactionConsumedThisPress = false;
            return;
        }

        ReadMoveInput();
        ReadJumpInput();
        ReadCrouchAndInteractionInput();
    }

    private void FixedUpdate()
    {
        UpdateCrouchState();
        CheckGrounded();
        UpdateJumpTimers();

        HandleHorizontalMovement();
        HandleJump();
        HandleJumpCut();
        ApplyGravityAndLimitFallSpeed();

        wasGrounded = isGrounded;
    }

    private void ReadMoveInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void ReadJumpInput()
    {
        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (jumpAction.WasReleasedThisFrame())
        {
            jumpCutRequested = true;
        }
    }

    private void ReadCrouchAndInteractionInput()
    {
        if (crouchAction.WasPressedThisFrame())
        {
            interactionConsumedThisPress =
                playerInteraction != null &&
                playerInteraction.TryInteract();
        }

        if (crouchAction.WasReleasedThisFrame())
        {
            interactionConsumedThisPress = false;
        }

        /*
         * Yakında etkileşim varsa bu tuş basışı crouch
         * için kullanılmaz. Oyuncu tuşu bırakıp tekrar
         * basana kadar crouch başlamaz.
         */
        crouchHeld =
            crouchAction.IsPressed() &&
            !interactionConsumedThisPress;
    }

    private void UpdateCrouchState()
    {
        if (crouchHeld)
        {
            if (!isCrouching)
            {
                SetCrouching(true);
            }

            return;
        }

        if (!isCrouching)
        {
            return;
        }

        /*
         * Tuş bırakılmış olsa bile tavan varsa
         * oyuncu crouch hâlinde kalır.
         */
        if (CanStandUp())
        {
            SetCrouching(false);
        }
    }

    private bool CanStandUp()
    {
        if (ceilingCheck == null)
        {
            return true;
        }

        Collider2D ceilingCollider =
            Physics2D.OverlapBox(
                ceilingCheck.position,
                ceilingCheckSize,
                ceilingCheck.eulerAngles.z,
                groundLayer
            );

        return ceilingCollider == null;
    }

    private void SetCrouching(
        bool crouching,
        bool forceUpdate = false
    )
    {
        if (!forceUpdate && isCrouching == crouching)
        {
            return;
        }

        isCrouching = crouching;

        if (isCrouching)
        {
            bodyCollider.size =
                crouchingColliderSize;

            bodyCollider.offset =
                crouchingColliderOffset;

            /*
             * Crouch sırasında daha önce alınmış jump
             * isteğinin sonradan çalışmasını engeller.
             */
            jumpBufferCounter = 0f;
        }
        else
        {
            bodyCollider.size =
                standingColliderSize;

            bodyCollider.offset =
                standingColliderOffset;
        }

        UpdatePlayerVisual();
    }

    private void UpdatePlayerVisual()
    {
        if (playerVisual == null)
        {
            return;
        }

        if (!isCrouching)
        {
            playerVisual.localScale =
                standingVisualScale;

            playerVisual.localPosition =
                standingVisualLocalPosition;

            return;
        }

        float heightRatio =
            crouchingColliderSize.y /
            standingColliderSize.y;

        Vector3 crouchingVisualScale =
            standingVisualScale;

        crouchingVisualScale.y =
            standingVisualScale.y * heightRatio;

        Vector3 crouchingVisualPosition =
            standingVisualLocalPosition;

        crouchingVisualPosition.y +=
            crouchingColliderOffset.y -
            standingColliderOffset.y;

        playerVisual.localScale =
            crouchingVisualScale;

        playerVisual.localPosition =
            crouchingVisualPosition;
    }

    private void CheckGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        Collider2D groundCollider =
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

        isGrounded = groundCollider != null;
    }

    private void UpdateJumpTimers()
    {
        Vector2 velocity = GetBodyVelocity();

        if (isGrounded && velocity.y <= 0.05f)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;

            coyoteCounter =
                Mathf.Max(coyoteCounter, 0f);
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -=
                Time.fixedDeltaTime;

            jumpBufferCounter =
                Mathf.Max(jumpBufferCounter, 0f);
        }
    }

    private void HandleHorizontalMovement()
    {
        Vector2 velocity = GetBodyVelocity();

        float currentMoveSpeed = moveSpeed;

        if (isCrouching)
        {
            currentMoveSpeed *=
                crouchMoveSpeedMultiplier;
        }

        velocity.x =
            moveInput.x * currentMoveSpeed;

        SetBodyVelocity(velocity);
    }

    private void HandleJump()
    {
        if (isCrouching)
        {
            jumpBufferCounter = 0f;
            return;
        }

        bool hasBufferedJump =
            jumpBufferCounter > 0f;

        bool canJump =
            coyoteCounter > 0f;

        if (!hasBufferedJump || !canJump)
        {
            return;
        }

        Vector2 velocity = GetBodyVelocity();

        velocity.y = jumpForce;

        SetBodyVelocity(velocity);

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isGrounded = false;
    }

    private void HandleJumpCut()
    {
        if (!jumpCutRequested)
        {
            return;
        }

        jumpCutRequested = false;

        Vector2 velocity = GetBodyVelocity();

        if (velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;

            SetBodyVelocity(velocity);
        }
    }

    private void ApplyGravityAndLimitFallSpeed()
    {
        Vector2 velocity = GetBodyVelocity();

        if (velocity.y < -0.01f)
        {
            rb.gravityScale =
                baseGravityScale *
                fallingGravityMultiplier;
        }
        else
        {
            rb.gravityScale =
                baseGravityScale;
        }

        if (velocity.y < maxFallSpeed)
        {
            velocity.y = maxFallSpeed;

            SetBodyVelocity(velocity);
        }
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpCutRequested = false;
            crouchHeld = false;
            interactionConsumedThisPress = false;

            Vector2 velocity = GetBodyVelocity();

            velocity.x = 0f;

            SetBodyVelocity(velocity);
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool JustLandedThisFrame()
    {
        return isGrounded && !wasGrounded;
    }

    private Vector2 GetBodyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetBodyVelocity(Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);

        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime =
            Mathf.Max(0f, jumpBufferTime);

        jumpCutMultiplier =
            Mathf.Clamp01(jumpCutMultiplier);

        baseGravityScale =
            Mathf.Max(0f, baseGravityScale);

        fallingGravityMultiplier =
            Mathf.Max(1f, fallingGravityMultiplier);

        maxFallSpeed =
            -Mathf.Max(
                0.01f,
                Mathf.Abs(maxFallSpeed)
            );

        groundCheckRadius =
            Mathf.Max(0.01f, groundCheckRadius);

        standingColliderSize.x =
            Mathf.Max(0.05f, standingColliderSize.x);

        standingColliderSize.y =
            Mathf.Max(0.05f, standingColliderSize.y);

        crouchingColliderSize.x =
            Mathf.Max(0.05f, crouchingColliderSize.x);

        crouchingColliderSize.y =
            Mathf.Clamp(
                crouchingColliderSize.y,
                0.05f,
                standingColliderSize.y
            );

        crouchMoveSpeedMultiplier =
            Mathf.Clamp01(
                crouchMoveSpeedMultiplier
            );

        ceilingCheckSize.x =
            Mathf.Max(0.05f, ceilingCheckSize.x);

        ceilingCheckSize.y =
            Mathf.Max(0.05f, ceilingCheckSize.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (drawGroundCheckGizmo &&
            groundCheck != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (drawCeilingCheckGizmo &&
            ceilingCheck != null)
        {
            Matrix4x4 previousMatrix =
                Gizmos.matrix;

            Gizmos.color = Color.cyan;

            Gizmos.matrix = Matrix4x4.TRS(
                ceilingCheck.position,
                ceilingCheck.rotation,
                Vector3.one
            );

            Gizmos.DrawWireCube(
                Vector3.zero,
                ceilingCheckSize
            );

            Gizmos.matrix = previousMatrix;
        }
    }
#endif
}