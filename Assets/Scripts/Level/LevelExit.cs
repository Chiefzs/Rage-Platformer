using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class LevelExit : MonoBehaviour
{
    [Header("Level Transition")]
    [Tooltip(
        "Kapıya girildikten sonra bir sonraki levelin " +
        "yüklenmesi için beklenecek süre."
    )]
    [SerializeField]
    [Min(0f)]
    private float transitionDelay = 0.15f;

    private Collider2D exitCollider;

    private PlayerController2D currentPlayer;
    private Rigidbody2D currentPlayerBody;

    private bool isCompletingLevel;

    private void Awake()
    {
        exitCollider = GetComponent<Collider2D>();

        if (!exitCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name} üzerindeki Collider2D, " +
                "Is Trigger olarak ayarlanmamış.",
                gameObject
            );
        }
    }

    private void Reset()
    {
        Collider2D attachedCollider =
            GetComponent<Collider2D>();

        if (attachedCollider != null)
        {
            attachedCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCompletingLevel)
        {
            return;
        }

        PlayerController2D player =
            other.GetComponentInParent<PlayerController2D>();

        if (player == null)
        {
            return;
        }

        PlayerDeathController playerDeath =
            player.GetComponent<PlayerDeathController>();

        if (playerDeath != null && playerDeath.IsDead)
        {
            return;
        }

        isCompletingLevel = true;

        currentPlayer = player;
        currentPlayerBody =
            player.GetComponent<Rigidbody2D>();


        exitCollider.enabled = false;

        FreezePlayer();

        StartCoroutine(LoadNextLevelAfterDelay());
    }

    private void FreezePlayer()
    {
        if (currentPlayer != null)
        {
            currentPlayer.SetControlsEnabled(false);
        }

        if (currentPlayerBody == null)
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        currentPlayerBody.linearVelocity = Vector2.zero;
#else
        currentPlayerBody.velocity = Vector2.zero;
#endif

        currentPlayerBody.angularVelocity = 0f;
        currentPlayerBody.simulated = false;
    }

    private IEnumerator LoadNextLevelAfterDelay()
    {
        if (transitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                transitionDelay
            );
        }
        else
        {
            /*
             * Scene'i fizik callback'inin doğrudan içinden
             * yüklememek için en az bir frame bekliyoruz.
             */
            yield return null;
        }

        Scene activeScene =
            SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                "Aktif sahne Scene List içerisinde bulunmuyor. " +
                "File > Build Profiles > Scene List bölümünü kontrol et.",
                gameObject
            );

            RestorePlayerAfterFailure();
            yield break;
        }

        int nextSceneBuildIndex =
            activeScene.buildIndex + 1;

        if (nextSceneBuildIndex >=
            SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning(
                $"{activeScene.name} sahnesinden sonra " +
                "yüklenebilecek başka bir level bulunamadı. " +
                "Scene List sırasını kontrol et.",
                gameObject
            );

            RestorePlayerAfterFailure();
            yield break;
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(nextSceneBuildIndex);
    }

    private void RestorePlayerAfterFailure()
    {
        if (currentPlayerBody != null)
        {
            currentPlayerBody.simulated = true;
        }

        if (currentPlayer != null)
        {
            currentPlayer.SetControlsEnabled(true);
        }

    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        transitionDelay =
            Mathf.Max(0f, transitionDelay);
    }
#endif
}