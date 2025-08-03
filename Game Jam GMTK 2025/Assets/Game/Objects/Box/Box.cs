using UnityEngine;

public class Box : MonoBehaviour, IPickable
{
    [Header("Static Return Settings")]
    [SerializeField] private float stopThreshold = 0.1f; // Seuil de vitesse pour considérer l'objet arrêté
    [SerializeField] private float stopCheckDelay = 1f; // Temps d'attente avant de vérifier l'arrêt
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint; // Assigne ce point dans l'inspecteur Unity

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool isThrown = false; // Pour savoir si l'objet a été lancé
    private Rigidbody2D rb;
    void Start()
    {
        if (respawnPoint != null)
        {
            initialPosition = respawnPoint.position;
            initialRotation = respawnPoint.rotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // S'assurer qu'il commence en Static
    }

    public void SetPhysicsAndCollidersEnabled(bool enabled)
    {
        if (rb != null)
        {
            if (enabled)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // Quand lancé, devient Dynamic
                isThrown = true;
                StartCoroutine(CheckForStop()); // Commencer à vérifier l'arrêt
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Kinematic; // Quand ramassé, devient Kinematic
                isThrown = false;
            }
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = enabled;
        }
    }
    private System.Collections.IEnumerator CheckForStop()
    {
        yield return new WaitForSeconds(stopCheckDelay); // Attendre un peu avant de commencer à vérifier

        while (isThrown && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            if (rb.velocity.magnitude <= stopThreshold)
            {
                // L'objet s'est arrêté, le remettre en Static
                rb.bodyType = RigidbodyType2D.Static;
                isThrown = false;
                Debug.Log("Box stopped, switched to Static");
                yield break;
            }
            yield return new WaitForSeconds(0.1f); // Vérifier toutes les 0.1 secondes
        }
    }

    public void Respawn()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Réinitialiser la physique
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static; // Remettre en Static
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = true; // Réactiver les colliders
        }

        Debug.Log(gameObject.name + " respawned.");
    }

}


