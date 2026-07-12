using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Hazard : MonoBehaviour
{
    private Collider2D hazardCollider;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();

        if (!hazardCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{gameObject.name} üzerindeki Hazard collider'ı " +
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
        PlayerDeathController playerDeath =
            other.GetComponentInParent<PlayerDeathController>();

        if (playerDeath == null)
        {
            return;
        }

        playerDeath.Die();
    }
}