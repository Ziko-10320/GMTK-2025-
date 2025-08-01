using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerDeath; // Event to notify other scripts of player death

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 1; // Max health, set to 1 for instant death on hazard
    private int currentHealth;

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
        OnPlayerDeath?.Invoke(); // Trigger the death event
        // The respawn logic will be handled by PlayerCloneController listening to this event

        // Reset health for next life (PlayerCloneController will handle actual respawn position)
        currentHealth = maxHealth;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Debug.Log("Player health reset.");
    }
}
