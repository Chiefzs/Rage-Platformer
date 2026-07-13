using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Detection")]
    [Tooltip(
        "Etkileşim alanının merkezi. Atanmazsa Player'ın " +
        "kendi pozisyonu kullanılır."
    )]
    [SerializeField]
    private Transform interactionPoint;

    [Tooltip(
        "Oyuncunun ne kadar uzaktaki nesnelerle " +
        "etkileşime girebileceği."
    )]
    [SerializeField]
    [Min(0.05f)]
    private float interactionRadius = 1.25f;

    [Tooltip(
        "Hangi layer üzerindeki nesneler etkileşim için aranacak?"
    )]
    [SerializeField]
    private LayerMask interactableLayer;

    public bool TryInteract()
    {
        Vector2 interactionPosition =
            interactionPoint != null
                ? interactionPoint.position
                : transform.position;

        Collider2D[] nearbyColliders =
            Physics2D.OverlapCircleAll(
                interactionPosition,
                interactionRadius,
                interactableLayer
            );

        IInteractable closestInteractable = null;
        float closestDistanceSqr =
            float.PositiveInfinity;

        foreach (Collider2D nearbyCollider in nearbyColliders)
        {
            IInteractable interactable =
                FindInteractable(nearbyCollider);

            if (interactable == null)
            {
                continue;
            }

            if (!interactable.CanInteract(gameObject))
            {
                continue;
            }

            Vector2 closestPoint =
                nearbyCollider.ClosestPoint(
                    interactionPosition
                );

            float distanceSqr =
                (closestPoint - interactionPosition)
                .sqrMagnitude;

            if (distanceSqr >= closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            closestInteractable = interactable;
        }

        if (closestInteractable == null)
        {
            return false;
        }

        closestInteractable.Interact(gameObject);

        return true;
    }

    private static IInteractable FindInteractable(
        Collider2D sourceCollider
    )
    {
        MonoBehaviour[] behaviours =
            sourceCollider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        interactionRadius =
            Mathf.Max(0.05f, interactionRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 interactionPosition =
            interactionPoint != null
                ? interactionPoint.position
                : transform.position;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            interactionPosition,
            interactionRadius
        );
    }
#endif
}