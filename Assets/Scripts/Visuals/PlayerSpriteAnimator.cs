using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerSpriteAnimator : MonoBehaviour
{
    [Header("Sprite Frames")]
    [SerializeField]
    private Sprite idleSprite;

    [SerializeField]
    private Sprite[] walkSprites;

    [SerializeField]
    private Sprite crouchSprite;

    [SerializeField]
    private Sprite[] jumpSprites;

    [Header("Generated Sprite Materials")]
    [SerializeField]
    private Material stateMaterial;

    [SerializeField]
    private Material walkMaterial;

    [SerializeField]
    private Material jumpMaterial;

    [Header("Animation")]
    [SerializeField]
    [Min(1f)]
    private float walkFramesPerSecond = 11f;

    [SerializeField]
    [Min(0f)]
    private float movementThreshold = 0.05f;

    [SerializeField]
    [Min(0f)]
    private float takeoffFrameDuration = 0.08f;

    [SerializeField]
    [Min(0f)]
    private float landingFrameDuration = 0.09f;

    [SerializeField]
    private float apexEnterVelocity = 3f;

    [SerializeField]
    private float fallEnterVelocity = -2f;

    private SpriteRenderer spriteRenderer;
    private PlayerController2D playerController;
    private Rigidbody2D playerBody;

    private Vector3 stableLocalScale;
    private Vector3 stableLocalPosition;

    private float walkTimer;
    private float airborneTimer;
    private float landingTimer;
    private int lastFacingDirection = 1;
    private bool previousGrounded;
    private bool groundStateInitialized;

    public int WalkFrameCount =>
        walkSprites == null ? 0 : walkSprites.Length;

    public int JumpFrameCount =>
        jumpSprites == null ? 0 : jumpSprites.Length;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController =
            GetComponentInParent<PlayerController2D>();
        playerBody = GetComponentInParent<Rigidbody2D>();

        stableLocalScale = transform.localScale;
        stableLocalPosition = transform.localPosition;

        SetSprite(idleSprite, crouchSprite, stateMaterial);
    }

    private void LateUpdate()
    {
        if (playerController == null || playerBody == null)
        {
            return;
        }

        Vector2 velocity = GetBodyVelocity();

        if (Mathf.Abs(velocity.x) > movementThreshold)
        {
            lastFacingDirection = velocity.x < 0f ? -1 : 1;
        }

        spriteRenderer.flipX = lastFacingDirection < 0;

        bool isCrouching = playerController.IsCrouching();
        bool isGrounded = playerController.IsGrounded();

        if (!groundStateInitialized)
        {
            previousGrounded = isGrounded;
            groundStateInitialized = true;
        }
        else if (!previousGrounded && isGrounded)
        {
            landingTimer = landingFrameDuration;
        }

        if (isGrounded)
        {
            airborneTimer = 0f;
            landingTimer = Mathf.Max(
                0f,
                landingTimer - Time.deltaTime
            );
        }
        else
        {
            airborneTimer += Time.deltaTime;
            landingTimer = 0f;
        }

        if (isCrouching)
        {
            walkTimer = 0f;
            SetSprite(crouchSprite, idleSprite, stateMaterial);
        }
        else if (
            isGrounded &&
            landingTimer > 0f &&
            HasJumpFrame(4)
        )
        {
            walkTimer = 0f;
            SetSprite(jumpSprites[4], idleSprite, jumpMaterial);
        }
        else if (!isGrounded)
        {
            walkTimer = 0f;

            int jumpFrameIndex;

            if (airborneTimer <= takeoffFrameDuration)
            {
                jumpFrameIndex = 0;
            }
            else if (velocity.y > apexEnterVelocity)
            {
                jumpFrameIndex = 1;
            }
            else if (velocity.y >= fallEnterVelocity)
            {
                jumpFrameIndex = 2;
            }
            else
            {
                jumpFrameIndex = 3;
            }

            Sprite jumpSprite = HasJumpFrame(jumpFrameIndex)
                ? jumpSprites[jumpFrameIndex]
                : idleSprite;

            SetSprite(jumpSprite, idleSprite, jumpMaterial);
        }
        else if (
            Mathf.Abs(velocity.x) > movementThreshold &&
            walkSprites != null &&
            walkSprites.Length > 0
        )
        {
            walkTimer += Time.deltaTime * walkFramesPerSecond;

            int frameIndex =
                Mathf.FloorToInt(walkTimer) % walkSprites.Length;

            SetSprite(
                walkSprites[frameIndex],
                idleSprite,
                walkMaterial
            );
        }
        else
        {
            walkTimer = 0f;
            SetSprite(idleSprite, crouchSprite, stateMaterial);
        }

        /*
         * PlayerController2D crouch sırasında eski kare görseli
         * dikey olarak eziyor. Artık özel crouch karesi kullanıldığı
         * için görsel transformunu sabit tutuyoruz; yalnızca fizik
         * collider'ı küçülmeye devam ediyor.
         */
        transform.localScale = stableLocalScale;
        transform.localPosition = stableLocalPosition;
        previousGrounded = isGrounded;
    }

    public void Configure(
        Sprite idle,
        Sprite[] walking,
        Sprite crouching,
        Sprite[] jumping,
        Material states,
        Material walkingMaterial,
        Material jumpingMaterial,
        float framesPerSecond
    )
    {
        idleSprite = idle;
        walkSprites = walking;
        crouchSprite = crouching;
        jumpSprites = jumping;
        stateMaterial = states;
        walkMaterial = walkingMaterial;
        jumpMaterial = jumpingMaterial;
        walkFramesPerSecond = Mathf.Max(1f, framesPerSecond);

        SpriteRenderer targetRenderer =
            GetComponent<SpriteRenderer>();

        if (targetRenderer != null)
        {
            targetRenderer.sprite = idleSprite;

            if (stateMaterial != null)
            {
                targetRenderer.sharedMaterial = stateMaterial;
            }
        }
    }

    private bool HasJumpFrame(int index)
    {
        return jumpSprites != null &&
            index >= 0 &&
            index < jumpSprites.Length &&
            jumpSprites[index] != null;
    }

    private void SetSprite(
        Sprite preferred,
        Sprite fallback,
        Material material
    )
    {
        Sprite target = preferred != null ? preferred : fallback;

        if (target != null)
        {
            spriteRenderer.sprite = target;
        }

        if (material != null)
        {
            spriteRenderer.sharedMaterial = material;
        }
    }

    private Vector2 GetBodyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return playerBody.linearVelocity;
#else
        return playerBody.velocity;
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        walkFramesPerSecond =
            Mathf.Max(1f, walkFramesPerSecond);
        movementThreshold =
            Mathf.Max(0f, movementThreshold);
        takeoffFrameDuration =
            Mathf.Max(0f, takeoffFrameDuration);
        landingFrameDuration =
            Mathf.Max(0f, landingFrameDuration);

        if (fallEnterVelocity > apexEnterVelocity)
        {
            fallEnterVelocity = apexEnterVelocity;
        }
    }
#endif
}
