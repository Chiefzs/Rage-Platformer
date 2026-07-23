using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameSession : MonoBehaviour
{
    private const string SfxSourceObjectName = "Persistent SFX";

    public static GameSession Instance { get; private set; }

    [Header("Lives Settings")]
    [Tooltip("Yeni bir oyun başladığında oyuncunun sahip olacağı can sayısı.")]
    [SerializeField]
    [Min(1)]
    private int startingLives = 3;

    public int CurrentLives { get; private set; }

    public int StartingLives => startingLives;

    public event Action<int> LivesChanged;

    private AudioSource sfxSource;


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

        ConfigureSfxSource();

        CurrentLives = Mathf.Max(1, startingLives);
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        if (sfxSource == null)
        {
            ConfigureSfxSource();
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void ConfigureSfxSource()
    {
        Transform sourceTransform = transform.Find(SfxSourceObjectName);

        if (sourceTransform == null)
        {
            GameObject sourceObject = new GameObject(
                SfxSourceObjectName
            );
            sourceTransform = sourceObject.transform;
            sourceTransform.SetParent(transform, false);
        }

        sfxSource = sourceTransform.GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            sfxSource = sourceTransform.gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.dopplerLevel = 0f;
        sfxSource.ignoreListenerPause = true;
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
