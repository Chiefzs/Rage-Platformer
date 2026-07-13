using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverMenu : MonoBehaviour
{
    public static GameOverMenu Instance { get; private set; }

    [Header("UI References")]
    [SerializeField]
    private GameObject gameOverPanel;

    [SerializeField]
    private Button restartRunButton;

    [Header("Run Settings")]
    [Tooltip(
        "Game Over ekranındaki Restart Run düğmesine " +
        "basıldığında açılacak ilk levelin sahne adı."
    )]
    [SerializeField]
    private string firstLevelSceneName = "Level_01";

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "GameOverMenu üzerinde Game Over Panel atanmamış.",
                gameObject
            );
        }

        if (restartRunButton != null)
        {
            restartRunButton.onClick.AddListener(
                RestartRun
            );
        }
        else
        {
            Debug.LogError(
                "GameOverMenu üzerinde Restart Run Button atanmamış.",
                gameObject
            );
        }
    }

    public void Show()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartRun()
    {
        if (string.IsNullOrWhiteSpace(firstLevelSceneName))
        {
            Debug.LogError(
                "GameOverMenu üzerindeki First Level Scene Name boş.",
                gameObject
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                firstLevelSceneName
            ))
        {
            Debug.LogError(
                $"'{firstLevelSceneName}' sahnesi yüklenemiyor. " +
                "Sahne adını ve Build Profiles > Scene List " +
                "ayarını kontrol et.",
                gameObject
            );

            return;
        }

        Time.timeScale = 1f;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetRun();
        }
        else
        {
            /*
             * GameSession bulunmasa bile Level_01 yüklenir.
             * Level_01 içindeki GameSession yeni koşuyu oluşturur.
             */
            Debug.LogWarning(
                "Restart sırasında GameSession bulunamadı. " +
                "İlk level yine de yüklenecek.",
                gameObject
            );
        }

        SceneManager.LoadScene(firstLevelSceneName);
    }

    private void OnDestroy()
    {
        if (restartRunButton != null)
        {
            restartRunButton.onClick.RemoveListener(
                RestartRun
            );
        }

        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (firstLevelSceneName != null)
        {
            firstLevelSceneName =
                firstLevelSceneName.Trim();
        }
    }
#endif
}