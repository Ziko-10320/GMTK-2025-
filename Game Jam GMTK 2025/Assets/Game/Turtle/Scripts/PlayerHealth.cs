using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerDeath; // Event to notify other scripts of player death

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 1; // Max health, set to 1 for instant death on hazard
    public int currentHealth; // Rendre public pour GameManager
    public int MaxHealth { get { return maxHealth; } } // Ajouter un getter public pour maxHealth

    [Header("Death Particle Systems")]
    [SerializeField] private ParticleSystem[] deathParticleSystems; // Tableau de systèmes de particules de mort

    [Header("Death Spawn Points")]
    [SerializeField] private Transform[] deathSpawnPoints; // Tableau de points de spawn pour les particules

    [Header("Audio Settings")]
    [SerializeField] private AudioSource deathAudioSource; // AudioSource for death sound
    [SerializeField] private AudioClip deathSoundClip; // Clip for death sound
    [SerializeField][Range(0f, 1f)] private float deathSoundVolume = 1f; // Volume for death sound

    void Awake()
    {

        currentHealth = maxHealth;
        if (deathAudioSource == null)
        {
            deathAudioSource = GetComponent<AudioSource>();
            if (deathAudioSource == null)
            {
                deathAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        deathAudioSource.volume = deathSoundVolume;
        deathAudioSource.playOnAwake = false;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"Player took {damageAmount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        if (deathAudioSource != null && deathSoundClip != null)
        {
            deathAudioSource.PlayOneShot(deathSoundClip, deathSoundVolume);
        }

        // Déclencher les systèmes de particules de mort
        for (int i = 0; i < deathParticleSystems.Length; i++)
        {
            if (deathParticleSystems[i] != null && deathSpawnPoints.Length > i && deathSpawnPoints[i] != null)
            {
                // Instancier le système de particules à la position du point de spawn
                // ou simplement le jouer si c'est un système pré-existant
                ParticleSystem ps = Instantiate(deathParticleSystems[i], deathSpawnPoints[i].position, Quaternion.identity);
                ps.Play();
                // Détruire le système de particules après sa durée pour éviter l'accumulation
                Destroy(ps.gameObject, ps.main.duration);
            }
        }
        OnPlayerDeath?.Invoke();

        // Notifier le GameManager de la mort du joueur
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartPlayerRespawn();
        }

        // Désactiver le joueur ici, car GameManager va gérer la réactivation
        gameObject.SetActive(false);
    }
}