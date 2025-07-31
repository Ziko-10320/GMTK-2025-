using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PlayerSnapshot
{
    public Vector2 position;
    public Quaternion rotation;
    public Vector2 velocity;
    public string animationState;
    public bool isFacingRight;
    public bool isDashing;
    public float timestamp;

    public PlayerSnapshot(Vector2 pos, Quaternion rot, Vector2 vel, string animState, bool facingRight, bool dashing, float time)
    {
        position = pos;
        rotation = rot;
        velocity = vel;
        animationState = animState;
        isFacingRight = facingRight;
        isDashing = dashing;
        timestamp = time;
    }
}

public class PlayerCloneController : MonoBehaviour
{
    // Mouvement (hérité de PlayerMovement)
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    private float horizontalMovement;
    public bool isFacingRight = true;

    // Saut
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    // Dash
    [Header("Dashing")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer;
    private float originalGravity;
    private Vector2 dashDirectionVector;

    // Clone System
    [Header("Clone System")]
    [SerializeField] private float cloneRecordDuration = 3f;
    [SerializeField] private float cloneScaleFactor = 1.5f;
    [SerializeField] private float clonePlaybackSpeed = 0.75f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject clonePrefab;
    public int maxActiveClones = 2; // Nouvelle variable publique pour la limite de clones
    private List<GameObject> activeClones = new List<GameObject>(); // Liste pour suivre les clones actifs

    [Header("Clone Stun")]
    [SerializeField] private float cloneStunDuration = 2f; // Durée d'étourdissement des clones

    // UI Elements
    [Header("UI Elements")]
    [SerializeField] private Slider uiTimerSlider;
    [SerializeField] private Slider volumeSlider;

    // Audio
    [Header("Audio")]
    [SerializeField] private AudioSource cloneAudioSource;
    [SerializeField] private AudioClip cloneAudioClip;
    [SerializeField] private float cloneAudioVolume = 1f;

    // Clone Recording
    private bool isRecording = false;
    private float recordingTimer = 0f;
    private List<PlayerSnapshot> recordedSnapshots = new List<PlayerSnapshot>();
    private float snapshotInterval = 0.02f; // 50 FPS recording
    private float lastSnapshotTime = 0f;

    // Références
    private Rigidbody2D rb;
    private Animator animator;
    private GhostEffect ghostEffect;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        ghostEffect = GetComponent<GhostEffect>();
        originalGravity = rb.gravityScale;

        // Initialize UI
        if (uiTimerSlider != null)
        {
            uiTimerSlider.maxValue = cloneRecordDuration;
            uiTimerSlider.value = 0f;
            uiTimerSlider.gameObject.SetActive(false);
        }

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = cloneAudioVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Initialize audio
        if (cloneAudioSource != null)
        {
            cloneAudioSource.volume = cloneAudioVolume;
            cloneAudioSource.clip = cloneAudioClip;
        }
    }

    void Update()
    {
        // Vérification du sol
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        // Clone recording trigger
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isRecording)
            {
                StartCloneRecording();
            }
            else
            {
                EndCloneRecording(); // Stop recording if already recording
            }
        }

        // Handle recording
        if (isRecording)
        {
            HandleRecording();
        }

        // Mouvement horizontal (seulement si pas en train d'enregistrer ou si vivant)
        // Le joueur peut toujours bouger pendant l'enregistrement
        horizontalMovement = Input.GetAxisRaw("Horizontal");

        // Animation de course
        animator.SetBool("isRunning", horizontalMovement != 0);

        // Flip du joueur
        if (horizontalMovement > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalMovement < 0 && isFacingRight)
        {
            Flip();
        }

        // Saut
        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(StartDash());
        }

        // Stun Clones
        if (Input.GetKeyDown(KeyCode.F))
        {
            StunAllActiveClones();
        }
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            rb.velocity = new Vector2(horizontalMovement * moveSpeed, rb.velocity.y);
        }
        else
        {
            // Apply dash movement in FixedUpdate for consistent physics
            rb.velocity = new Vector2(dashDirectionVector.x * dashSpeed, 0);
        }

        // Record snapshot during recording
        if (isRecording && Time.time >= lastSnapshotTime + snapshotInterval)
        {
            RecordSnapshot();
            lastSnapshotTime = Time.time;
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private IEnumerator StartDash()
    {
        isDashing = true;
        canDash = false;
        rb.gravityScale = 0f; // Disable gravity during dash
        rb.velocity = Vector2.zero; // Reset velocity

        dashDirectionVector = isFacingRight ? Vector2.right : Vector2.left;

        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }
        animator.SetTrigger("Dash");

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        rb.gravityScale = originalGravity; // Restore gravity
        rb.velocity = Vector2.zero; // Stop dash movement

        if (ghostEffect != null)
        {
            ghostEffect.StopGhostEffect();
        }

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void StartCloneRecording()
    {
        isRecording = true;
        recordingTimer = 0f;
        recordedSnapshots.Clear();
        lastSnapshotTime = 0f;

        // Show UI timer
        if (uiTimerSlider != null)
        {
            uiTimerSlider.maxValue = cloneRecordDuration;
            uiTimerSlider.value = 0f;
            uiTimerSlider.gameObject.SetActive(true);
        }

        // Play audio
        if (cloneAudioSource != null && cloneAudioClip != null)
        {
            cloneAudioSource.Play();
        }

        // Start ghost effect for player during recording
        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }

        Debug.Log("Clone recording started!");
    }

    private void HandleRecording()
    {
        recordingTimer += Time.deltaTime;

        // Update UI timer
        if (uiTimerSlider != null)
        {
            uiTimerSlider.value = recordingTimer;
        }

        // Check if recording is complete
        if (recordingTimer >= cloneRecordDuration)
        {
            EndCloneRecording();
        }
    }

    private void RecordSnapshot()
    {
        string currentAnimationState = GetCurrentAnimationState();

        PlayerSnapshot snapshot = new PlayerSnapshot(
            transform.position,
            transform.rotation,
            rb.velocity,
            currentAnimationState,
            isFacingRight,
            isDashing,
            Time.time
        );

        recordedSnapshots.Add(snapshot);
    }

    private string GetCurrentAnimationState()
    {
        if (animator == null) return "Idle";

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Dash"))
            return "Dash";
        else if (stateInfo.IsName("Jump"))
            return "Jump";
        else if (stateInfo.IsName("Run"))
            return "Run";
        else
            return "Idle";
    }

    private void EndCloneRecording()
    {
        isRecording = false;

        // Hide UI timer
        if (uiTimerSlider != null)
        {
            uiTimerSlider.gameObject.SetActive(false);
        }

        // Stop audio
        if (cloneAudioSource != null)
        {
            cloneAudioSource.Stop();
        }

        // Stop ghost effect for player after recording
        if (ghostEffect != null)
        {
            ghostEffect.StopGhostEffect();
        }

        Debug.Log($"Clone recording ended! Recorded {recordedSnapshots.Count} snapshots.");

        // Create clone and respawn player
        CreateCloneAndRespawnPlayer();
    }

    private void CreateCloneAndRespawnPlayer()
    {
        if (clonePrefab != null && recordedSnapshots.Count > 0)
        {
            // Handle max active clones - Clean up null references first
            CleanupActiveClones();

            // Destroy oldest clones if we're at the limit
            while (activeClones.Count >= maxActiveClones)
            {
                DestroyOldestClone();
            }

            // Create new clone
            CreateNewClone();
        }

        // Respawn player immediately
        RespawnPlayer();
    }

    private void CleanupActiveClones()
    {
        // Remove null entries from the list (clones that might have been destroyed manually)
        for (int i = activeClones.Count - 1; i >= 0; i--)
        {
            if (activeClones[i] == null)
            {
                activeClones.RemoveAt(i);
            }
        }
    }

    private void DestroyOldestClone()
    {
        if (activeClones.Count > 0)
        {
            GameObject oldestClone = activeClones[0];
            activeClones.RemoveAt(0);
            if (oldestClone != null)
            {
                Destroy(oldestClone);
                Debug.Log("Destroyed oldest clone to make space for new one.");
            }
        }
    }

    private void CreateNewClone()
    {
        // Instantiate clone at the first recorded position
        Vector3 cloneStartPosition = recordedSnapshots[0].position;
        GameObject cloneInstance = Instantiate(clonePrefab, cloneStartPosition, Quaternion.identity);

        // Scale the clone
        cloneInstance.transform.localScale = Vector3.one * cloneScaleFactor;

        // Get the ClonePlayback component and pass the recorded data
        ClonePlayback clonePlayback = cloneInstance.GetComponent<ClonePlayback>();
        if (clonePlayback != null)
        {
            clonePlayback.InitializeClone(recordedSnapshots, clonePlaybackSpeed);
        }

        // Add new clone to active clones list
        activeClones.Add(cloneInstance);

        Debug.Log("Clone created successfully! Current active clones: " + activeClones.Count);
    }

    private void RespawnPlayer()
    {
        // Start the respawn process
        StartCoroutine(RespawnPlayerCoroutine());
    }

    private IEnumerator RespawnPlayerCoroutine()
    {
        // Step 1: Désactiver le rendu et les colliders du joueur au lieu de désactiver l'objet entier
        SetPlayerVisualsAndColliders(false);
        Debug.Log("Player 'despawned' for respawn!");

        // Step 2: Attendre un court délai pour simuler le 'despawn'
        yield return new WaitForSeconds(0.1f); // Petit délai pour rendre le 'despawn' visible

        // Step 3: Déplacer le joueur au point de respawn
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("RespawnPoint n'est pas assigné ! Le joueur réapparaîtra à l'origine.");
            transform.position = Vector3.zero;
        }

        // Step 4: Réinitialiser l'état du joueur
        ResetPlayerState();

        // Step 5: S'assurer que le joueur fait face à droite
        EnsureFacingRight();

        // Step 6: Réactiver le rendu et les colliders du joueur
        SetPlayerVisualsAndColliders(true);

        Debug.Log("Player respawned at RespawnPoint!");
    }

    private void SetPlayerVisualsAndColliders(bool active)
    {
        // Gérer les SpriteRenderers
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = active;
        }

        // Gérer les Colliders 2D
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = active;
        }

        // Gérer le Rigidbody2D
        if (rb != null)
        {
            rb.simulated = active; // Désactiver la simulation physique lorsque le joueur est 'désactivé'
        }

        // S'assurer que le script PlayerCloneController reste actif
        this.enabled = true; // Le script doit toujours être actif pour gérer les entrées et le clonage
    }




    private void StunAllActiveClones()
    {
        CleanupActiveClones(); // Ensure the list is clean
        foreach (GameObject clone in activeClones)
        {
            if (clone != null)
            {
                ClonePlayback clonePlayback = clone.GetComponent<ClonePlayback>();
                if (clonePlayback != null)
                {
                    clonePlayback.StunClone(cloneStunDuration);
                }
            }
        }
        Debug.Log($"All active clones stunned for {cloneStunDuration} seconds!");
    }


    private void ResetPlayerState()
    {
        // Reset Rigidbody2D properties
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = originalGravity;
            rb.simulated = true;
        }

        // Reset other player states
        isDashing = false;
        canDash = true;
        horizontalMovement = 0f;
    }

    private void EnsureFacingRight()
    {
        // Force player to face right after respawn
        if (!isFacingRight)
        {
            isFacingRight = true;
            Vector3 scaler = transform.localScale;
            scaler.x = Mathf.Abs(scaler.x); // Ensure positive scale for facing right
            transform.localScale = scaler;
        }
    }

    private void OnVolumeChanged(float value)
    {
        cloneAudioVolume = value;
        if (cloneAudioSource != null)
        {
            cloneAudioSource.volume = cloneAudioVolume;
        }
    }

    // Public method to get clone settings (for external configuration)
    public void SetCloneSettings(float recordDuration, float scaleFactor, float playbackSpeed)
    {
        cloneRecordDuration = recordDuration;
        cloneScaleFactor = scaleFactor;
        clonePlaybackSpeed = playbackSpeed;

        if (uiTimerSlider != null)
        {
            uiTimerSlider.maxValue = cloneRecordDuration;
        }
    }

    // Public method to get current active clones count (for debugging)
    public int GetActiveCloneCount()
    {
        CleanupActiveClones();
        return activeClones.Count;
    }

    // Public method to manually destroy all clones
    public void DestroyAllClones()
    {
        foreach (GameObject clone in activeClones)
        {
            if (clone != null)
            {
                Destroy(clone);
            }
        }
        activeClones.Clear();
        Debug.Log("All clones destroyed!");
    }
}





