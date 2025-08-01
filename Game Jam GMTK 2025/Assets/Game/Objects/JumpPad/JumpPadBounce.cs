using UnityEngine;

public class JumpPad : MonoBehaviour, IPickable
{
    [Header("Jump Pad Settings")]
    [SerializeField] private float bounceForce = 20f; // Force de rebond appliquée au joueur

    // Optionnel: Effets visuels ou sonores
    [Header("Effects (Optional)")]
    [SerializeField] private GameObject bounceEffectPrefab; // Effet de particule ou autre
    [SerializeField] private AudioClip bounceSound; // Son de rebond
    private AudioSource audioSource;

    void Awake()
    {
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
        // Vérifier si l\"objet en collision a le tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                // Réinitialiser la vélocité Y du joueur pour un rebond constant
                playerRb.velocity = new Vector2(playerRb.velocity.x, 0f);
                // Appliquer la force de rebond instantanée vers le haut
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

                // Jouer l\"effet sonore
                if (audioSource != null && bounceSound != null)
                {
                    audioSource.PlayOneShot(bounceSound);
                }

                // Instancier l\"effet visuel
                if (bounceEffectPrefab != null)
                {
                    Instantiate(bounceEffectPrefab, transform.position, Quaternion.identity);
                }
            }
        }
    }

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