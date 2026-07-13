using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class TestInteractable :
    MonoBehaviour,
    IInteractable
{
    [Header("Test Message")]
    [SerializeField]
    [TextArea(2, 4)]
    private string message =
        "Merhaba! Etkileşim sistemi çalışıyor.";

    [SerializeField]
    [Min(0.1f)]
    private float messageDuration = 2.5f;

    private Collider2D interactionCollider;

    private void Awake()
    {
        interactionCollider =
            GetComponent<Collider2D>();

        if (!interactionCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name} üzerindeki collider " +
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

    public bool CanInteract(GameObject interactor)
    {
        if (interactor == null)
        {
            return false;
        }

        return interactor.GetComponent<PlayerController2D>()
            != null;
    }

    public void Interact(GameObject interactor)
    {
        if (InteractionMessageUI.Instance != null)
        {
            InteractionMessageUI.Instance.ShowMessage(
                message,
                messageDuration
            );

            return;
        }

        Debug.Log(
            $"Interaction message: {message}",
            gameObject
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        messageDuration =
            Mathf.Max(0.1f, messageDuration);
    }
#endif
}