using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerDeathController : MonoBehaviour
{
    [Header("Explosion")]
    [Tooltip("Oyuncu öldüğünde oluşturulacak patlama prefabı.")]
    [SerializeField]
    private GameObject explosionPrefab;

    [Tooltip("Patlama efektinin Player merkezine göre konumu.")]
    [SerializeField]
    private Vector3 explosionOffset = Vector3.zero;

    [Header("Death Timing")]
    [Tooltip(
        "Patlama başladıktan sonra reset veya Game Over için " +
        "kaç saniye beklenecek?"
    )]
    [SerializeField]
    [Min(0f)]
    private float deathSequenceDuration = 0.65f;

    private PlayerController2D playerController;
    private Rigidbody2D rb;

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] playerColliders;

    private bool isDead;

    public bool IsDead => isDead;

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
    }

    /// <summary>
    /// Hazard veya KillZone tarafından çağrılır.
    /// </summary>
    public void Die()
    {
        /*
         * Oyuncu aynı anda birden fazla collider'a değse bile
         * yalnızca bir can kaybetmesini sağlar.
         */
        if (isDead)
        {
            return;
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isDead = true;

        DisablePlayer();

        GameObject explosionInstance = CreateExplosion();

        if (explosionInstance != null)
        {
            StartCoroutine(
                DestroyExplosionAfterRealtime(
                    explosionInstance,
                    2f
                )
            );
        }

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
            hasLivesRemaining =
                GameSession.Instance.LoseLife();
        }

        /*
         * Time.timeScale daha sonra 0 olsa bile
         * patlama bekleme süresi çalışmaya devam eder.
         */
        yield return new WaitForSecondsRealtime(
            deathSequenceDuration
        );

        if (hasLivesRemaining)
        {
            ReloadCurrentLevel();
        }
        else
        {
            ShowGameOver();
        }
    }

    private void DisablePlayer()
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
            playerCollider.enabled = false;
        }

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.enabled = false;
        }
    }

    private GameObject CreateExplosion()
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning(
                "PlayerDeathController üzerinde " +
                "Explosion Prefab atanmamış."
            );

            return null;
        }

        Vector3 explosionPosition =
            transform.position + explosionOffset;

        return Instantiate(
            explosionPrefab,
            explosionPosition,
            Quaternion.identity
        );
    }

    private IEnumerator DestroyExplosionAfterRealtime(
        GameObject explosionObject,
        float waitTime
    )
    {
        yield return new WaitForSecondsRealtime(waitTime);

        if (explosionObject != null)
        {
            Destroy(explosionObject);
        }
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
        deathSequenceDuration =
            Mathf.Max(0f, deathSequenceDuration);
    }
#endif
}