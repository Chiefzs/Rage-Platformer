using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerDeathController : MonoBehaviour
{
    [Header("Death Timing")]
    [Tooltip(
        "Ölüm başladığında reset veya Game Over için " +
        "beklenecek toplam süre."
    )]
    [SerializeField]
    [Min(0f)]
    private float deathSequenceDuration = 0.82f;

    [Header("Death Audio")]
    [Tooltip("Can kaybedildiğinde çalacak, insan sesi içermeyen efekt.")]
    [SerializeField]
    private AudioClip lifeLostClip;

    [SerializeField]
    [Range(0f, 1f)]
    private float lifeLostVolume = 0.88f;

    [Header("Sprite Fragmentation")]
    [Tooltip("Mevcut karakter karesini parçalara ayıran materyal.")]
    [SerializeField]
    private Material fragmentMaterial;

    [Tooltip(
        "Parçalanmadan önce mevcut karakter karesinin " +
        "okunacağı çok kısa bekleme."
    )]
    [SerializeField]
    [Min(0f)]
    private float fragmentHoldDuration = 0.035f;

    [SerializeField]
    [Min(0f)]
    private float fragmentMinimumSpeed = 1.25f;

    [SerializeField]
    [Min(0f)]
    private float fragmentMaximumSpeed = 2.85f;

    [SerializeField]
    [Min(0f)]
    private float fragmentUpwardBoost = 0.9f;

    [SerializeField]
    [Min(0f)]
    private float fragmentGravity = 5.8f;

    [SerializeField]
    [Min(0f)]
    private float fragmentDrag = 0.28f;

    [SerializeField]
    [Min(0f)]
    private float fragmentAngularSpeed = 390f;

    [SerializeField]
    [Min(0.05f)]
    private float fragmentLifetime = 0.72f;

    [SerializeField]
    [Min(0f)]
    private float fragmentFadeStart = 0.48f;

    private PlayerController2D playerController;
    private Rigidbody2D rb;
    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] playerColliders;
    private PlayerSpriteAnimator[] spriteAnimators;

    private Vector2 deathEntryVelocity;
    private bool isDead;

    public bool IsDead => isDead;

    public AudioClip LifeLostClip => lifeLostClip;

    public Material FragmentMaterial => fragmentMaterial;

    public float DeathSequenceDuration => deathSequenceDuration;

    public float LifeLostVolume => lifeLostVolume;

    public float FragmentHoldDuration => fragmentHoldDuration;

    public float FragmentLifetime => fragmentLifetime;

    public float FragmentFadeStart => fragmentFadeStart;

    private void Awake()
    {
        playerController = GetComponent<PlayerController2D>();
        rb = GetComponent<Rigidbody2D>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(
            includeInactive: true
        );
        playerColliders = GetComponentsInChildren<Collider2D>(
            includeInactive: true
        );
        spriteAnimators = GetComponentsInChildren<PlayerSpriteAnimator>(
            includeInactive: true
        );
    }

    /// <summary>
    /// Hazard veya KillZone tarafından çağrılır.
    /// </summary>
    public void Die()
    {
        if (isDead)
        {
            return;
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isDead = true;

        float sequenceStartTime = Time.unscaledTime;
        deathEntryVelocity = GetBodyVelocity();

        DisablePlayerMotionAndCollisions();
        StopSpriteAnimation();
        PlayLifeLostSound();

        bool hasLivesRemaining = false;

        if (GameSession.Instance == null)
        {
            Debug.LogError(
                "GameSession bulunamadı. " +
                "Sahnede GameSession objesi olduğundan emin ol."
            );
        }
        else
        {
            hasLivesRemaining = GameSession.Instance.LoseLife();
        }

        yield return PlayFragmentationAnimation();

        float elapsedTime = Time.unscaledTime - sequenceStartTime;
        float remainingTime = deathSequenceDuration - elapsedTime;

        if (remainingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingTime);
        }

        if (hasLivesRemaining)
        {
            ReloadCurrentLevel();
        }
        else
        {
            ShowGameOver();
        }
    }

    private void DisablePlayerMotionAndCollisions()
    {
        playerController.SetControlsEnabled(false);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif

        rb.angularVelocity = 0f;
        rb.simulated = false;

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }
        }
    }

    private void StopSpriteAnimation()
    {
        foreach (PlayerSpriteAnimator spriteAnimator in spriteAnimators)
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.enabled = false;
            }
        }
    }

    private void PlayLifeLostSound()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.PlaySfx(
                lifeLostClip,
                lifeLostVolume
            );
        }
    }

    private IEnumerator PlayFragmentationAnimation()
    {
        SpriteRenderer primaryRenderer = FindPrimaryRenderer();

        if (primaryRenderer == null)
        {
            HidePlayerVisuals();
            yield break;
        }

        if (fragmentHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                fragmentHoldDuration
            );
        }

        Vector2 inheritedVelocity = Vector2.ClampMagnitude(
            deathEntryVelocity,
            5f
        ) * 0.18f;

        SpriteFragmentBurst fragmentBurst =
            SpriteFragmentBurst.Create(
                primaryRenderer,
                fragmentMaterial,
                inheritedVelocity,
                fragmentMinimumSpeed,
                fragmentMaximumSpeed,
                fragmentUpwardBoost,
                fragmentGravity,
                fragmentDrag,
                fragmentAngularSpeed,
                fragmentLifetime,
                fragmentFadeStart
            );

        if (fragmentBurst == null)
        {
            Debug.LogWarning(
                "Karakter sprite parçaları oluşturulamadı.",
                gameObject
            );
            yield break;
        }

        HidePlayerVisuals();

        // Bir kare boyunca parçalar karakteri tam olarak yeniden kurar.
        yield return null;
        fragmentBurst.Begin();
    }

    private SpriteRenderer FindPrimaryRenderer()
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (
                spriteRenderer != null &&
                spriteRenderer.enabled &&
                spriteRenderer.gameObject.activeInHierarchy &&
                spriteRenderer.sprite != null
            )
            {
                return spriteRenderer;
            }
        }

        return null;
    }

    private void HidePlayerVisuals()
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }
    }

    private Vector2 GetBodyVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public void ConfigurePresentation(
        AudioClip clip,
        Material spriteFragmentMaterial,
        float sequenceDuration
    )
    {
        lifeLostClip = clip;
        fragmentMaterial = spriteFragmentMaterial;
        fragmentHoldDuration = 0.035f;
        fragmentMinimumSpeed = 1.25f;
        fragmentMaximumSpeed = 2.85f;
        fragmentUpwardBoost = 0.9f;
        fragmentGravity = 5.8f;
        fragmentDrag = 0.28f;
        fragmentAngularSpeed = 390f;
        fragmentLifetime = 0.72f;
        fragmentFadeStart = 0.48f;
        deathSequenceDuration = Mathf.Max(
            fragmentHoldDuration + fragmentLifetime + 0.05f,
            sequenceDuration
        );
    }

    private void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                "Aktif sahne Build Profiles listesinde bulunmuyor. " +
                "File > Build Profiles içinden Add Open Scenes yap."
            );
            return;
        }

        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void ShowGameOver()
    {
        if (GameOverMenu.Instance == null)
        {
            Debug.LogError(
                "GameOverMenu bulunamadı. " +
                "Canvas üzerinde GameOverMenu component'i " +
                "olduğundan emin ol."
            );
            return;
        }

        GameOverMenu.Instance.Show();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lifeLostVolume = Mathf.Clamp01(lifeLostVolume);
        fragmentHoldDuration = Mathf.Max(0f, fragmentHoldDuration);
        fragmentMinimumSpeed = Mathf.Max(0f, fragmentMinimumSpeed);
        fragmentMaximumSpeed = Mathf.Max(
            fragmentMinimumSpeed,
            fragmentMaximumSpeed
        );
        fragmentUpwardBoost = Mathf.Max(0f, fragmentUpwardBoost);
        fragmentGravity = Mathf.Max(0f, fragmentGravity);
        fragmentDrag = Mathf.Max(0f, fragmentDrag);
        fragmentAngularSpeed = Mathf.Max(0f, fragmentAngularSpeed);
        fragmentLifetime = Mathf.Max(0.05f, fragmentLifetime);
        fragmentFadeStart = Mathf.Clamp(
            fragmentFadeStart,
            0f,
            fragmentLifetime
        );
        deathSequenceDuration = Mathf.Max(
            deathSequenceDuration,
            fragmentHoldDuration + fragmentLifetime + 0.05f
        );
    }
#endif
}
