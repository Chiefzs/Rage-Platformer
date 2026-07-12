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
        Time.timeScale = 1f;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetRun();
        }
        else
        {
            Debug.LogError(
                "Restart sırasında GameSession bulunamadı."
            );
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                "Aktif sahne Build Profiles listesinde bulunmuyor."
            );

            return;
        }

        SceneManager.LoadScene(activeScene.buildIndex);
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
}