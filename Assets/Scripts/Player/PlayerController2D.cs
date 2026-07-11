using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 13f;

    [Tooltip("Oyuncu zeminden ayrıldıktan sonra kaç saniye daha zıplayabilir?")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Oyuncu yere inmeden önce zıplamaya basarsa input kaç saniye saklanır?")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Tooltip(
        "Jump tuşu erken bırakıldığında yukarı yönlü hızın ne kadarı korunur? " +
        "Düşük değer daha kısa zıplama oluşturur."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Gravity Settings")]
    [Tooltip("Oyuncunun normal gravity scale değeri.")]
    [SerializeField] private float baseGravityScale = 3f;

    [Tooltip(
        "Oyuncu düşerken normal gravity kaç katına çıkar? " +
        "Örneğin 1.5 değeri düşüşte 4.5 gravity scale üretir."
    )]
    [SerializeField] private float fallingGravityMultiplier = 1.5f;

    [Tooltip("Maksimum düşüş hızı. Negatif değer olmalıdır.")]
    [SerializeField] private float maxFallSpeed = -22f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Debug")]
    [SerializeField] private bool drawGroundCheckGizmo = true;

    private Rigidbody2D rb;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector2 moveInput;

    private bool isGrounded;
    private bool wasGrounded;
    private bool controlsEnabled = true;

    private float coyoteCounter;
    private float jumpBufferCounter;

    // Update içinde kaydedilir, FixedUpdate içinde uygulanır.
    private bool jumpCutRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

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
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            jumpCutRequested = false;
            return;
        }

        ReadMoveInput();
        ReadJumpInput();
    }

    private void FixedUpdate()
    {
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
            // Rigidbody burada değiştirilmez.
            // Sadece FixedUpdate için istek kaydedilir.
            jumpCutRequested = true;
        }
    }

    private void CheckGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        Collider2D groundCollider = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        isGrounded = groundCollider != null;
    }

    private void UpdateJumpTimers()
    {
        Vector2 velocity = GetBodyVelocity();

        /*
         * Oyuncu jump yaptıktan hemen sonra GroundCheck kısa süreliğine
         * hâlâ zemine değebilir. Dikey hız pozitifken coyote time'ı
         * yenilemeyerek istemsiz çift zıplamayı engelliyoruz.
         */
        if (isGrounded && velocity.y <= 0.05f)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
            coyoteCounter = Mathf.Max(coyoteCounter, 0f);
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
            jumpBufferCounter = Mathf.Max(jumpBufferCounter, 0f);
        }
    }

    private void HandleHorizontalMovement()
    {
        Vector2 velocity = GetBodyVelocity();

        velocity.x = moveInput.x * moveSpeed;

        SetBodyVelocity(velocity);
    }

    private void HandleJump()
    {
        bool hasBufferedJump = jumpBufferCounter > 0f;
        bool canJump = coyoteCounter > 0f;

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

        // Oyuncu hâlâ yükseliyorsa zıplama yüksekliğini azalt.
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
                baseGravityScale * fallingGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
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

            Vector2 velocity = GetBodyVelocity();

            velocity.x = 0f;

            SetBodyVelocity(velocity);
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
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

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);

        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);

        jumpCutMultiplier = Mathf.Clamp01(jumpCutMultiplier);

        baseGravityScale = Mathf.Max(0f, baseGravityScale);
        fallingGravityMultiplier =
            Mathf.Max(1f, fallingGravityMultiplier);

        maxFallSpeed =
            -Mathf.Max(0.01f, Mathf.Abs(maxFallSpeed));

        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGroundCheckGizmo || groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}