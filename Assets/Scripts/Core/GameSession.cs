using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Lives Settings")]
    [Tooltip("Yeni bir oyun başladığında oyuncunun sahip olacağı can sayısı.")]
    [SerializeField]
    [Min(1)]
    private int startingLives = 3;

    public int CurrentLives { get; private set; }

    public int StartingLives => startingLives;

    public event Action<int> LivesChanged;


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        /*
         * Scene yeniden yüklendiğinde yeni bir GameSession oluşabilir.
         * Eski kalıcı GameSession varsa yeni olanı siliyoruz.
         */
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        CurrentLives = Mathf.Max(1, startingLives);
    }


    public bool LoseLife()
    {
        if (CurrentLives <= 0)
        {
            return false;
        }

        CurrentLives--;

        CurrentLives = Mathf.Max(CurrentLives, 0);

        LivesChanged?.Invoke(CurrentLives);

        return CurrentLives > 0;
    }


    public void ResetRun()
    {
        CurrentLives = Mathf.Max(1, startingLives);

        LivesChanged?.Invoke(CurrentLives);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        startingLives = Mathf.Max(1, startingLives);
    }
#endif
}