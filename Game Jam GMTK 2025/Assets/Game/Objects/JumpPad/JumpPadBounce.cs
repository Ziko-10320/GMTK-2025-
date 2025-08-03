using UnityEngine;

public class JumpPad : MonoBehaviour, IPickable
{
    [Header("Jump Pad Settings")]
    [SerializeField] private float bounceForce = 20f; // Force de rebond appliquée au joueur
    [Header("Static Return Settings")]
    [SerializeField] private float stopThreshold = 0.1f; // Seuil de vitesse pour considérer l'objet arrêté
    [SerializeField] private float stopCheckDelay = 1f; // Temps d'attente avant de vérifier l'arrêt
    private Rigidbody2D rb;
    private bool isThrown = false;
    private PlayerCloneController playerCloneController; // Référence au script du joueur
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint; // Assigne ce point dans l'inspecteur Unity

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    // Optionnel: Effets visuels ou sonores
    [Header("Effects (Optional)")]
    [SerializeField] private GameObject bounceEffectPrefab; // Effet de particule ou autre
    [SerializeField] private AudioClip bounceSound; // Son de rebond
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // S'assurer qu'il commence en Static

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

        // Obtenir ou ajouter un AudioSource si un son est assigné
        if (bounceSound != null && GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = bounceSound;
        }
        else if (GetComponent<AudioSource>() != null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerCloneController playerController = collision.gameObject.GetComponent<PlayerCloneController>();
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

            // La condition principale est que le joueur NE DOIT PAS être au sol
            if (playerRb != null && playerController != null && !playerController.isGrounded)
            {
                // Réinitialiser la vélocité Y du joueur pour un rebond constant
                playerRb.velocity = new Vector2(playerRb.velocity.x, 0f);
                // Appliquer la force de rebond instantanée vers le haut
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                // Jouer l'effet sonore
                if (audioSource != null && bounceSound != null)
                {
                    audioSource.PlayOneShot(bounceSound);
                }

                // Instancier l'effet visuel
                if (bounceEffectPrefab != null)
                {
                    Instantiate(bounceEffectPrefab, transform.position, Quaternion.identity);
                }

                Debug.Log("Player bounced on JumpPad!");
            }
            else
            {
                Debug.Log("Player is grounded, no bounce.");
            }
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
}