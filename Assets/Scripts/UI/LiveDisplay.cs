using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LivesDisplay : MonoBehaviour
{
    private TMP_Text livesText;
    private GameSession gameSession;

    private void Awake()
    {
        livesText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        gameSession = GameSession.Instance;

        if (gameSession == null)
        {
            Debug.LogError(
                "LivesDisplay, GameSession bulamadı.",
                gameObject
            );

            return;
        }

        gameSession.LivesChanged += HandleLivesChanged;

        UpdateText(gameSession.CurrentLives);
    }

    private void HandleLivesChanged(int currentLives)
    {
        UpdateText(currentLives);
    }

    private void UpdateText(int currentLives)
    {
        livesText.text = $"LIVES: {currentLives}";
    }

    private void OnDestroy()
    {
        if (gameSession != null)
        {
            gameSession.LivesChanged -= HandleLivesChanged;
        }
    }
}