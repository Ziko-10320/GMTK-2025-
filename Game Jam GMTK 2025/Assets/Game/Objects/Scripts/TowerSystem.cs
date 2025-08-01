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

        // Add a component to handle projectile behavior (collision, destruction)
        ProjectileBehavior projectileBehavior = projectile.AddComponent<ProjectileBehavior>();
        projectileBehavior.damageAmount = damageAmount;

        // Destroy the projectile after a certain time to prevent scene clutter
        Destroy(projectile, 5f); // Destroy after 5 seconds
    }

    // Inner class to handle projectile behavior
    private class ProjectileBehavior : MonoBehaviour
    {
        public int damageAmount; // Damage to inflict

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if the colliding object is the player
            if (other.CompareTag("Player")) // Assuming your player GameObject has the tag "Player"
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount);
                }
                Destroy(gameObject); // Destroy projectile on hit
            }
            else if (!other.isTrigger) // Destroy projectile if it hits something that is not a trigger (e.g., wall)
            {
                Destroy(gameObject); 
            }
        }
    }
}


