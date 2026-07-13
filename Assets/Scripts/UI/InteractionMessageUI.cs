using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionMessageUI : MonoBehaviour
{
    public static InteractionMessageUI Instance
    {
        get;
        private set;
    }

    [Header("UI References")]
    [SerializeField]
    private GameObject messagePanel;

    [SerializeField]
    private TMP_Text messageText;

    private Coroutine hideMessageCoroutine;

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
            Debug.LogError(
                "Sahnede birden fazla InteractionMessageUI var.",
                gameObject
            );

            enabled = false;
            return;
        }

        Instance = this;

        if (messagePanel == null)
        {
            Debug.LogError(
                "InteractionMessageUI üzerinde Message Panel atanmamış.",
                gameObject
            );

            return;
        }

        if (messageText == null)
        {
            Debug.LogError(
                "InteractionMessageUI üzerinde Message Text atanmamış.",
                gameObject
            );
        }

        messagePanel.SetActive(false);
    }

    public void ShowMessage(
        string message,
        float duration
    )
    {
        if (messagePanel == null || messageText == null)
        {
            return;
        }

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        messageText.text =
            string.IsNullOrWhiteSpace(message)
                ? "..."
                : message;

        messagePanel.SetActive(true);

        hideMessageCoroutine = StartCoroutine(
            HideMessageAfterDelay(
                Mathf.Max(0.1f, duration)
            )
        );
    }

    private IEnumerator HideMessageAfterDelay(
        float duration
    )
    {
        yield return new WaitForSecondsRealtime(duration);

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        hideMessageCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}