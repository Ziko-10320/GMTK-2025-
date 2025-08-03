using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float respawnDelay = 2f; // D�lai avant le respawn du joueur

    private GameObject playerGameObject; // R�f�rence au GameObject du joueur
    private PlayerHealth playerHealth; // R�f�rence au script PlayerHealth du joueur
    private Transform respawnPoint; // R�f�rence au point de respawn du joueur

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Garde le GameManager actif entre les sc�nes
        }
    }

    public void SetPlayerReferences(GameObject player, PlayerHealth health, Transform spawnPoint)
    {
        playerGameObject = player;
        playerHealth = health;
        respawnPoint = spawnPoint;
    }

    public void StartPlayerRespawn()
    {
        StartCoroutine(RespawnPlayerWithDelay());
    }

    private IEnumerator RespawnPlayerWithDelay()
    {
        // Assurez-vous que le joueur est d�sactiv� avant d'attendre
        if (playerGameObject != null)
        {
            playerGameObject.SetActive(false);
        }

        yield return new WaitForSeconds(respawnDelay);

        if (playerGameObject != null)
        {
            playerGameObject.SetActive(true);
            if (respawnPoint != null)
            {
                playerGameObject.transform.position = respawnPoint.position;
                // R�initialiser la v�locit� du Rigidbody2D si le joueur en a un
                Rigidbody2D rb = playerGameObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
            }
            if (playerHealth != null)
            {
                // R�initialiser la sant� du joueur via le script PlayerHealth
                playerHealth.currentHealth = playerHealth.MaxHealth;
            }
        }

       PlayerCloneController playerCloneController = playerGameObject.GetComponent<PlayerCloneController>();
        if (playerCloneController != null)
        {
            playerCloneController.ResetPlayerState();
        }
        Debug.Log("Player respawned!");
    }
}