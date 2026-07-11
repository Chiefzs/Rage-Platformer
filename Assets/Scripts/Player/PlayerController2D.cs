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
    [SerializeField] private float jumpForce = 13.5f;

    [Tooltip("Oyuncu zeminden ayrıldıktan sonra kaç saniye daha zıplayabilir?")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Oyuncu yere düşmeden önce zıplamaya basarsa input kaç saniye saklanır?")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Tooltip("Zıplama tuşu erken bırakılınca yukarı hız ne kadar kesilecek? 0.35 - 0.55 iyi aralık.")]
    [SerializeField] private float jumpCutMultiplier = 0.45f;

    [Header("Gravity Feel")]
    [Tooltip("Düşerken ekstra gravity çarpanı. 1 normal gravity, 2-3 daha tok düşüş.")]
    [SerializeField] private float fallGravityMultiplier = 2.2f;

    [Tooltip("Zıplama tuşu bırakıldıktan sonra yükselirken ekstra gravity. Kısa zıplamayı daha belirgin yapar.")]
    [SerializeField] private float jumpReleaseGravityMultiplier = 2.4f;

    [Tooltip("Maksimum düşüş hızı. Negatif değer olmalı.")]
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

    private bool jumpHeld;
    private bool jumpReleasedThisFrame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        rb.freezeRotation = true;

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            jumpHeld = false;
            jumpReleasedThisFrame = false;
            return;
        }

        ReadMoveInput();
        ReadJumpInput();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        UpdateTimers();

        HandleHorizontalMovement();
        HandleJump();
        ApplyBetterGravity();

        jumpReleasedThisFrame = false;
        wasGrounded = isGrounded;
    }

    private void ReadMoveInput()
    {
        if (moveAction == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void ReadJumpInput()
    {
        if (jumpAction == null)
            return;

        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }

        jumpHeld = jumpAction.IsPressed();

        if (jumpAction.WasReleasedThisFrame())
        {
            jumpReleasedThisFrame = true;
            CutJumpIfMovingUp();
        }
    }

    private void CheckGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void UpdateTimers()
    {
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
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
        bool canUseCoyoteJump = coyoteCounter > 0f;

        if (hasBufferedJump && canUseCoyoteJump)
        {
            Vector2 velocity = GetBodyVelocity();

            velocity.y = 0f;
            SetBodyVelocity(velocity);

            velocity = GetBodyVelocity();
            velocity.y = jumpForce;
            SetBodyVelocity(velocity);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void ApplyBetterGravity()
    {
        Vector2 velocity = GetBodyVelocity();

        // Oyuncu düşüyorsa daha güçlü gravity uygula.
        if (velocity.y < 0f)
        {
            velocity.y += Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        // Oyuncu yükseliyor ama jump tuşunu bırakmışsa yükselişi daha hızlı kes.
        else if (velocity.y > 0f && !jumpHeld)
        {
            velocity.y += Physics2D.gravity.y * (jumpReleaseGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        if (velocity.y < maxFallSpeed)
        {
            velocity.y = maxFallSpeed;
        }

        SetBodyVelocity(velocity);
    }

    private void CutJumpIfMovingUp()
    {
        Vector2 velocity = GetBodyVelocity();

        if (velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
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
            jumpHeld = false;
            jumpReleasedThisFrame = false;

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

    private void OnDrawGizmosSelected()
    {
        if (!drawGroundCheckGizmo || groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}