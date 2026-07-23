using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LivesDisplay : MonoBehaviour
{
    [Header("Icon Presentation")]
    [SerializeField]
    private Sprite lifeIconSprite;

    [SerializeField]
    private Vector2 iconSize = new Vector2(46f, 64f);

    [SerializeField]
    [Min(0f)]
    private float iconSpacing;

    [SerializeField]
    private Color iconColor = new Color(1f, 1f, 1f, 0.62f);

    private readonly List<Image> lifeIcons = new List<Image>();

    private TMP_Text livesText;
    private RectTransform rootRect;
    private GameSession gameSession;

    private void Awake()
    {
        CacheComponents();
        HideLegacyText();
        CollectExistingIcons();
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
        UpdateIcons(gameSession.CurrentLives);
    }

    public void Configure(
        Sprite iconSprite,
        Vector2 size,
        float spacing,
        Color color,
        int previewLives = 3
    )
    {
        lifeIconSprite = iconSprite;
        iconSize = new Vector2(
            Mathf.Max(1f, size.x),
            Mathf.Max(1f, size.y)
        );
        iconSpacing = Mathf.Max(0f, spacing);
        iconColor = color;

        CacheComponents();
        HideLegacyText();
        ConfigureRootRect();
        CollectExistingIcons();
        UpdateIcons(Mathf.Max(0, previewLives));
    }

    private void HandleLivesChanged(int currentLives)
    {
        UpdateIcons(currentLives);
    }

    private void UpdateIcons(int currentLives)
    {
        currentLives = Mathf.Max(0, currentLives);

        EnsureIconCapacity(currentLives);

        for (int index = 0; index < lifeIcons.Count; index++)
        {
            Image icon = lifeIcons[index];
            icon.sprite = lifeIconSprite;
            icon.color = iconColor;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.gameObject.SetActive(index < currentLives);

            LayoutIcon(icon.rectTransform, index);
        }
    }

    private void EnsureIconCapacity(int requiredCount)
    {
        CollectExistingIcons();

        while (lifeIcons.Count < requiredCount)
        {
            int iconNumber = lifeIcons.Count + 1;
            GameObject iconObject = new GameObject(
                $"LifeIcon_{iconNumber}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            iconObject.layer = gameObject.layer;

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(transform, false);

            Image icon = iconObject.GetComponent<Image>();
            lifeIcons.Add(icon);
        }
    }

    private void CollectExistingIcons()
    {
        lifeIcons.Clear();

        for (int index = 0; index < transform.childCount; index++)
        {
            Transform child = transform.GetChild(index);

            if (!child.name.StartsWith("LifeIcon_"))
            {
                continue;
            }

            Image icon = child.GetComponent<Image>();

            if (icon != null)
            {
                lifeIcons.Add(icon);
            }
        }

        lifeIcons.Sort(
            (left, right) =>
                left.transform.GetSiblingIndex().CompareTo(
                    right.transform.GetSiblingIndex()
                )
        );
    }

    private void LayoutIcon(RectTransform iconRect, int index)
    {
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.sizeDelta = iconSize;
        iconRect.anchoredPosition = new Vector2(
            index * (iconSize.x + iconSpacing),
            0f
        );
        iconRect.localScale = Vector3.one;
        iconRect.localRotation = Quaternion.identity;
    }

    private void ConfigureRootRect()
    {
        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(28f, -24f);
        rootRect.sizeDelta = new Vector2(
            4f * (iconSize.x + iconSpacing),
            iconSize.y
        );
    }

    private void CacheComponents()
    {
        if (livesText == null)
        {
            livesText = GetComponent<TMP_Text>();
        }

        if (rootRect == null)
        {
            rootRect = GetComponent<RectTransform>();
        }
    }

    private void HideLegacyText()
    {
        if (livesText == null)
        {
            return;
        }

        livesText.text = string.Empty;
        livesText.enabled = false;
        livesText.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (gameSession != null)
        {
            gameSession.LivesChanged -= HandleLivesChanged;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        iconSize.x = Mathf.Max(1f, iconSize.x);
        iconSize.y = Mathf.Max(1f, iconSize.y);
        iconSpacing = Mathf.Max(0f, iconSpacing);
    }
#endif
}
