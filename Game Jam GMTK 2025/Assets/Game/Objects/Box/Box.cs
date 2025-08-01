using UnityEngine;

public class Box : MonoBehaviour, IPickable
{
    public void SetPhysicsAndCollidersEnabled(bool enabled)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = !enabled; // Si enabled est false, isKinematic est true (pas de physique)
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = enabled;
        }
    }
}


