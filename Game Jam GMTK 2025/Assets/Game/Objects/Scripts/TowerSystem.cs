using System.Collections;
using UnityEngine;

public class TowerSystem : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private GameObject projectilePrefab; // Prefab du projectile à tirer
    [SerializeField] private Transform firePoint; // Point d'où le projectile est tiré
    [SerializeField] private float fireRate = 1f; // Cadence de tir (projectiles par seconde)
    [SerializeField] private float projectileSpeed = 10f; // Vitesse du projectile
    [SerializeField] private int damageAmount = 1; // Dégâts infligés au joueur
    [SerializeField] private LayerMask projectileDestructionLayers; // Calques qui détruisent les projectiles
    [SerializeField] private ParticleSystem explosionEffectPrefab;
    
    private float nextFireTime = 0f;

    void Start()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned in TowerSystem!", this);
            enabled = false; // Disable script if no prefab
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError("Fire Point is not assigned in TowerSystem!", this);
            enabled = false; // Disable script if no fire point
            return;
        }

        StartCoroutine(ShootCoroutine());
    }

    private IEnumerator ShootCoroutine()
    {
        while (true)
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / fireRate;
            }
            yield return null; // Wait for next frame
        }
    }

    private void Shoot()
    {
        // Instantiate the projectile
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Ensure the projectile has a Rigidbody2D to apply velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // Projectiles usually don't have gravity
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // For better collision detection
        }

        // Ensure the projectile has a Collider2D and is a trigger
        Collider2D collider = projectile.GetComponent<Collider2D>();
        if (collider == null)
        {
            // Add a default collider if none exists (e.g., CircleCollider2D)
            collider = projectile.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true; // Make it a trigger for damage detection

        // Set the projectile's velocity
        // Assuming the tower shoots forward based on its local right direction
        rb.velocity = firePoint.right * projectileSpeed;

        ProjectileBehavior projectileBehavior = projectile.AddComponent<ProjectileBehavior>();
        projectileBehavior.damageAmount = damageAmount;

        // AJOUTE CES DEUX LIGNES pour passer les nouvelles valeurs
        projectileBehavior.destructionLayers = projectileDestructionLayers;
        projectileBehavior.explosionPrefab = explosionEffectPrefab;

        // Destroy the projectile after a certain time to prevent scene clutter
        Destroy(projectile, 5f); // Destroy after 5 seconds
    }

    // Inner class to handle projectile behavior
    private class ProjectileBehavior : MonoBehaviour
    {
        public ParticleSystem explosionPrefab;
        public LayerMask destructionLayers;
        public int damageAmount; // Damage to inflict
        private TowerSystem towerSystem;

        private bool isDestroyed = false; // Pour éviter de déclencher l'explosion plusieurs fois

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Si la balle est déjà en cours de destruction, ne rien faire
            if (isDestroyed) return;

            // Condition 1: La balle touche le joueur
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount);
                }
                ExplodeAndDestroy(); // Déclencher l'explosion et la destruction
                return; // Arrêter le traitement ici
            }

            // Condition 2: La balle touche un calque de destruction
            // On utilise une opération binaire pour vérifier si le calque de l'objet "other" est dans notre LayerMask
            if ((destructionLayers.value & (1 << other.gameObject.layer)) > 0)
            {
                ExplodeAndDestroy(); // Déclencher l'explosion et la destruction
            }
        }

        // AJOUTE CETTE NOUVELLE MÉTHODE
        private void ExplodeAndDestroy()
        {
            isDestroyed = true; // Marquer comme détruit pour éviter les appels multiples

            // Instancier et jouer le système de particules
            if (explosionPrefab != null)
            {
                // Instancier le prefab à la position de la balle
                ParticleSystem explosionInstance = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

                // S'assurer que le système de particules est joué
                explosionInstance.Play();

                // Détruire l'objet du système de particules après sa durée de vie
                // C'est plus sûr que de se fier à la durée de l'effet principal
                Destroy(explosionInstance.gameObject, explosionInstance.main.startLifetime.constantMax);
            }

            // Finalement, détruire la balle elle-même
            Destroy(gameObject);
        }
    }
}



