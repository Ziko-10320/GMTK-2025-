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

    [Header("Pickable Object Interaction")]
    [SerializeField] private Transform pickPoint; // Point où l'objet sera tenu
    [SerializeField] private float pickUpRange = 1f; // Portée de ramassage de l'objet
    [SerializeField] private KeyCode pickUpThrowKey = KeyCode.G; // Touche pour ramasser/lancer
    [SerializeField] private float throwForce = 10f; // Force de lancer de l'objet
    private GameObject heldObject = null; // Référence à l'objet actuellement tenu

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

    // Moving Platform System - IMPROVED
    [Header("Moving Platform")]
    [SerializeField] private LayerMask clonePlatformLayer; // Layer for clones acting as platforms
    private Transform currentPlatformClone; // Reference to the clone the player is currently on
    private Vector3 lastPlatformPosition; // To track platform movement
    private Vector3 platformVelocity; // Track platform velocity for smooth movement
    private bool wasOnPlatform = false; // Track if player was on platform last frame

    [Header("Glow Effect During Recording")]
    [SerializeField] private SpriteRenderer glowSprite1; // Premier sprite à faire briller
    [SerializeField] private SpriteRenderer glowSprite2; // Deuxième sprite à faire briller
    [SerializeField] private Material glowMaterial1; // Matériau avec effet de glow pour le premier sprite
    [SerializeField] private Material glowMaterial2; // Matériau avec effet de glow pour le deuxième sprite
    private Material originalMaterial1; // Matériau original du premier sprite
    private Material originalMaterial2; // Matériau original du deuxième sprite

    [Header("Particle System During Recording")]
    [SerializeField] private ParticleSystem cloningParticleSystem; // Système de particules pour le clonage

    [Header("Dust Particle Systems")]
    [SerializeField] private ParticleSystem flipDustParticleSystem; // Système de particules pour le flip
    [SerializeField] private ParticleSystem fallDustParticleSystem; // Système de particules pour la chute au sol

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 2f; // Délai avant le respawn du joueur
    public bool IsRecordingClone { get { return isRecording; } }

    // UI Elements
    [Header("UI Elements")]
    [SerializeField] private Slider uiTimerSlider;
    [SerializeField] private Slider volumeSlider;

    // Audio
    [Header("Audio")]
    [SerializeField] private AudioSource cloneAudioSource;
    [SerializeField] private AudioClip cloneAudioClip;
    [SerializeField][Range(0f, 1f)] private float cloneAudioVolume = 1f;

    [Header("Respawn Audio")]
    [SerializeField] private AudioSource respawnAudioSource;
    [SerializeField] private AudioClip respawnSoundClip;
    [SerializeField][Range(0f, 1f)] private float respawnSoundVolume = 1f;

    [Header("Movement Audio")]
    [SerializeField] private AudioSource movementAudioSource; // AudioSource for movement sounds (run, jump, fall)
    [SerializeField] private AudioClip runSoundClip;
    [SerializeField][Range(0f, 1f)] private float runSoundVolume = 1f;
    [SerializeField] private AudioClip jumpSoundClip;
    [SerializeField][Range(0f, 1f)] private float jumpSoundVolume = 1f;
    [SerializeField] private AudioClip fallSoundClip;
    [SerializeField][Range(0f, 1f)] private float fallSoundVolume = 1f;

    [Header("Dash Audio")]
    [SerializeField] private AudioSource dashAudioSource;
    [SerializeField] private AudioClip dashSoundClip;
    [SerializeField][Range(0f, 1f)] private float dashSoundVolume = 1f;

    [Header("Clone System Audio")]
    [SerializeField] private AudioSource cloneSystemAudioSource; // General AudioSource for clone system sounds
    [SerializeField] private AudioClip cloneStartSoundClip; // Sound for starting clone recording
    [SerializeField][Range(0f, 1f)] private float cloneStartSoundVolume = 1f;
    [SerializeField] private AudioClip cloneRecordingLoopSoundClip; // Loop sound during recording
    [SerializeField][Range(0f, 1f)] private float cloneRecordingLoopSoundVolume = 1f;
    [SerializeField] private AudioClip cloneEndSoundClip; // Sound for ending clone recording/creation
    [SerializeField][Range(0f, 1f)] private float cloneEndSoundVolume = 1f;
    [SerializeField] private AudioClip stunSoundClip; // Sound for stunning clones
    [SerializeField][Range(0f, 1f)] private float stunSoundVolume = 1f;

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
    private PlayerHealth playerHealth; // Reference to PlayerHealth script

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        ghostEffect = GetComponent<GhostEffect>();
        originalGravity = rb.gravityScale;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerReferences(gameObject, playerHealth, respawnPoint);
        }

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

        if (respawnAudioSource == null)
        {
            respawnAudioSource = GetComponent<AudioSource>();
            if (respawnAudioSource == null)
            {
                respawnAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        respawnAudioSource.volume = respawnSoundVolume;
        respawnAudioSource.playOnAwake = false;

        if (movementAudioSource == null)
        {
            movementAudioSource = GetComponent<AudioSource>();
            if (movementAudioSource == null)
            {
                movementAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        movementAudioSource.volume = runSoundVolume; // Default to run volume
        movementAudioSource.playOnAwake = false;

        if (cloneSystemAudioSource == null)
        {
            cloneSystemAudioSource = GetComponent<AudioSource>();
            if (cloneSystemAudioSource == null)
            {
                cloneSystemAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        cloneSystemAudioSource.volume = cloneStartSoundVolume; // Default to clone start volume
        cloneSystemAudioSource.playOnAwake = false;

        // Initialize PlayerHealth and subscribe to death event
        playerHealth = GetComponent<PlayerHealth>();
      
    }

    void Update()
    {
        // Vérification du sol
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

       if (isGrounded && !wasGrounded) // If the player has just landed
       {
          if (fallDustParticleSystem != null)
          {
            fallDustParticleSystem.Play();
          }
       }

        // Handle fall sound
        if (!isGrounded && wasGrounded && rb.velocity.y < 0) // Just started falling
        {
            if (movementAudioSource != null && fallSoundClip != null)
            {
                movementAudioSource.PlayOneShot(fallSoundClip, fallSoundVolume);
            }
        }

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

        // Handle run sound
        if (isGrounded && horizontalMovement != 0 && !movementAudioSource.isPlaying && runSoundClip != null)
        {
            movementAudioSource.clip = runSoundClip;
            movementAudioSource.volume = runSoundVolume;
            movementAudioSource.loop = true;
            movementAudioSource.Play();
        }
        else if ((!isGrounded || horizontalMovement == 0) && movementAudioSource.clip == runSoundClip)
        {
            movementAudioSource.Stop();
        }

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
            if (movementAudioSource != null && jumpSoundClip != null)
            {
                movementAudioSource.PlayOneShot(jumpSoundClip, jumpSoundVolume);
            }
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
            if (cloneSystemAudioSource != null && stunSoundClip != null)
            {
                cloneSystemAudioSource.PlayOneShot(stunSoundClip, stunSoundVolume);
            }
        }

        // Pick up / Throw Object
        if (Input.GetKeyDown(pickUpThrowKey))
        {
            if (heldObject == null) // Try to pick up
            {
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, pickUpRange);
                foreach (Collider2D hitCollider in hitColliders)
                {
                    // Vérifier si l'objet a le tag "JumpPad" ou "Box"
                    if (hitCollider.CompareTag("JumpPad") || hitCollider.CompareTag("Box"))
                    {
                        IPickable pickableObject = hitCollider.gameObject.GetComponent<IPickable>();
                        if (pickableObject != null)
                        {
                            heldObject = hitCollider.gameObject;
                            heldObject.transform.position = pickPoint.position;
                            heldObject.transform.SetParent(pickPoint);

                            // Désactiver la physique et les colliders de l'objet ramassable
                            pickableObject.SetPhysicsAndCollidersEnabled(false);
                            Debug.Log($"Object {heldObject.name} picked up!");
                            break;
                        }
                    }
                }
            }
            else // Throw Object
            {
                // Détacher l'objet du joueur
                heldObject.transform.SetParent(null);

                // Réactiver la physique et les colliders de l'objet ramassable
                IPickable pickableObject = heldObject.GetComponent<IPickable>();
                if (pickableObject != null)
                {
                    pickableObject.SetPhysicsAndCollidersEnabled(true);
                }

                Rigidbody2D objectRb = heldObject.GetComponent<Rigidbody2D>();
                if (objectRb != null)
                {
                    // Réinitialiser la vélocité et appliquer une nouvelle vélocité pour le lancer
                    objectRb.velocity = Vector2.zero; // S'assurer qu'il n'y a pas de vélocité résiduelle
                    Vector2 throwDirection = isFacingRight ? Vector2.right : Vector2.left;
                    objectRb.velocity = throwDirection * throwForce; // Appliquer la vélocité directement
                }

                heldObject = null;
                Debug.Log("Object thrown!");
            }
        }
    }

    void FixedUpdate()
    {
        // La logique de la plateforme mobile est maintenant gérée en premier
        // pour déterminer si le joueur est sur une plateforme et obtenir sa vélocité.
        Vector2 platformVelocity = HandleMovingPlatformImproved();

        // Calculer la vélocité cible du joueur basée sur l'input
        float targetVelocityX = horizontalMovement * moveSpeed;

        // Appliquer la vélocité du joueur ET celle de la plateforme
        if (!isDashing)
        {
            // La nouvelle vélocité est la vélocité cible du joueur + la vélocité de la plateforme
            rb.velocity = new Vector2(targetVelocityX + platformVelocity.x, rb.velocity.y);
        }
        else
        {
            // La logique du dash reste inchangée
            rb.velocity = new Vector2(dashDirectionVector.x * dashSpeed, 0);
        }

        // L'enregistrement du snapshot reste ici
        if (isRecording && Time.time >= lastSnapshotTime + snapshotInterval)
        {
            RecordSnapshot();
            lastSnapshotTime = Time.time;
        }
    }

    // SYSTÈME DE PLATEFORME MOBILE AMÉLIORÉ ET CORRIGÉ
    Vector2 HandleMovingPlatformImproved()
    {
        // Vérifie si le joueur est au sol sur une plateforme clone
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, 0.2f, clonePlatformLayer);
        bool isOnClonePlatform = hit != null && isGrounded;

        if (isOnClonePlatform)
        {
            Transform clonePlatform = hit.transform;

            // Si on vient d'atterrir sur la plateforme
            if (currentPlatformClone != clonePlatform)
            {
                currentPlatformClone = clonePlatform;
                lastPlatformPosition = currentPlatformClone.position;
            }

            // Calculer le mouvement de la plateforme depuis la dernière frame physique
            Vector3 platformMovement = currentPlatformClone.position - lastPlatformPosition;
            lastPlatformPosition = currentPlatformClone.position;

            // Retourner la vélocité de la plateforme pour l'utiliser dans FixedUpdate
            // Cela permet au joueur de "coller" à la plateforme tout en se déplaçant librement
            return platformMovement / Time.fixedDeltaTime;
        }
        else
        {
            // Si le joueur n'est pas sur une plateforme, on réinitialise
            if (currentPlatformClone != null)
            {
                currentPlatformClone = null;
            }
            // Aucune vélocité de plateforme à appliquer
            return Vector2.zero;
        }
    }
    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    if (flipDustParticleSystem != null)
     {
        flipDustParticleSystem.Play();
     }
    }

    private IEnumerator StartDash()
    {
        isDashing = true;
        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }

        canDash = false;
        rb.gravityScale = 0f; // Disable gravity during dash
        dashDirectionVector = isFacingRight ? Vector2.right : Vector2.left;
        rb.velocity = new Vector2(dashDirectionVector.x * dashSpeed, 0);

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }


        if (dashAudioSource != null && dashSoundClip != null)
        {
            dashAudioSource.PlayOneShot(dashSoundClip, dashSoundVolume);
        }

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

       
        if (cloningParticleSystem != null)
        {
            var mainModule = cloningParticleSystem.main;
            mainModule.duration = cloneRecordDuration; // Set duration to match cloning duration
            mainModule.loop = true; // Ensure it loops
            cloningParticleSystem.Play();
        }
       

        if (glowSprite1 != null)
      {
        originalMaterial1 = glowSprite1.material;
        glowSprite1.material = glowMaterial1;
      }
    
      if (glowSprite2 != null)
      {
        originalMaterial2 = glowSprite2.material;
        glowSprite2.material = glowMaterial2;
      }

        isRecording = true;
        recordingTimer = 0f;
        recordedSnapshots.Clear();
        lastSnapshotTime = Time.time;

        // UI feedback
        if (uiTimerSlider != null)
        {
            uiTimerSlider.gameObject.SetActive(true);
        }

        // Audio feedback
        if (cloneSystemAudioSource != null && cloneStartSoundClip != null)
        {
            cloneSystemAudioSource.PlayOneShot(cloneStartSoundClip, cloneStartSoundVolume);
        }

        // Start recording loop sound
        if (cloneSystemAudioSource != null && cloneRecordingLoopSoundClip != null)
        {
            cloneSystemAudioSource.clip = cloneRecordingLoopSoundClip;
            cloneSystemAudioSource.volume = cloneRecordingLoopSoundVolume;
            cloneSystemAudioSource.loop = true;
            cloneSystemAudioSource.Play();
        }

        Debug.Log("Clone recording started!");
    }

    public void ResetPlayerState()
    {
        // Réinitialiser les variables de dash
        isDashing = false;
        canDash = true;
        rb.gravityScale = originalGravity; // Restaurer la gravité si elle a été modifiée par le dash
        rb.velocity = Vector2.zero; // Arrêter tout mouvement résiduel

        // Réinitialiser les paramètres de l'Animator
        if (animator != null)
        {
            animator.SetBool("isDashing", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false); // Assurez-vous que toutes les animations sont à leur état par défaut
                                                  // Ajoutez ici d'autres paramètres d'animation si nécessaire
        }

        // Assurez-vous que le joueur est bien orienté (par exemple, vers la droite par défaut)
        if (!isFacingRight)
        {
            Flip(); // Appelez votre méthode Flip() si elle existe et gère l'orientation
        }
        // Ou simplement :
        // isFacingRight = true;
        // transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        Debug.Log("Player state reset after respawn.");
    }
    private void HandleRecording()
    {
        recordingTimer += Time.deltaTime;

        // Update UI
        if (uiTimerSlider != null)
        {
            uiTimerSlider.value = recordingTimer;
        }

        // Auto-stop recording when duration is reached
        if (recordingTimer >= cloneRecordDuration)
        {
            EndCloneRecording();
        }
    }

    private void EndCloneRecording()
    {
        if (!isRecording) return;

        isRecording = false;
        if (uiTimerSlider != null)
        {
            uiTimerSlider.gameObject.SetActive(false);
        }

        if (cloneSystemAudioSource != null && cloneEndSoundClip != null)
        {
            cloneSystemAudioSource.PlayOneShot(cloneEndSoundClip, cloneEndSoundVolume);
        }

        if (recordedSnapshots.Count == 0)
        {
            Debug.Log("No snapshots recorded or recording cancelled. Player will respawn.");
            RespawnPlayerWithoutDeath(); // <-- AJOUTEZ CETTE LIGNE ICI
            return;
        }
        // *** AJOUTEZ CETTE CONDITION ICI ***
        if (recordedSnapshots.Count == 0)
        {
            Debug.Log("No snapshots recorded or recording cancelled. Clone will not be spawned.");
            return; // Ne pas créer de clone si aucun snapshot n'a été enregistré ou si l'enregistrement a été annulé
        }

        // Si le nombre de clones actifs dépasse la limite, détruire le plus ancien
        if (activeClones.Count >= maxActiveClones)
        {
            Destroy(activeClones[0]);
            activeClones.RemoveAt(0);
        }

        // Créer le clone
        GameObject newClone = Instantiate(clonePrefab, respawnPoint.position, Quaternion.identity);
        newClone.transform.localScale = transform.localScale * cloneScaleFactor; // Appliquer le facteur d'échelle
        ClonePlayback clonePlayback = newClone.GetComponent<ClonePlayback>();
        if (clonePlayback != null)
        {
            clonePlayback.InitializeClone(recordedSnapshots, clonePlaybackSpeed);
        }
        activeClones.Add(newClone);

        RespawnPlayerWithoutDeath();

    if (glowSprite1 != null && originalMaterial1 != null)
    {
        glowSprite1.material = originalMaterial1;
    }
    
    if (glowSprite2 != null && originalMaterial2 != null)
    {
        glowSprite2.material = originalMaterial2;
    }

     
        if (cloningParticleSystem != null)
        {
            cloningParticleSystem.Stop();
        }
       

        // Effacer les snapshots pour le prochain enregistrement
        recordedSnapshots.Clear();
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
        if (isDashing) return "Dash";
        if (!isGrounded) return "Jump";
        if (horizontalMovement != 0) return "Run";
        return "Idle";
    }

    private void CreateClone()
    {
        // Remove oldest clone if we've reached the limit
        if (activeClones.Count >= maxActiveClones)
        {
            GameObject oldestClone = activeClones[0];
            activeClones.RemoveAt(0);
            Destroy(oldestClone);
        }

        // Instantiate new clone
        GameObject newClone = Instantiate(clonePrefab, recordedSnapshots[0].position, recordedSnapshots[0].rotation);
        
        // Scale the clone
        newClone.transform.localScale = transform.localScale * cloneScaleFactor;

        // Initialize clone playback
        ClonePlayback clonePlayback = newClone.GetComponent<ClonePlayback>();
        if (clonePlayback != null)
        {
            clonePlayback.InitializeClone(recordedSnapshots, clonePlaybackSpeed);
        }

        // Add to active clones list
        activeClones.Add(newClone);

        // Audio feedback
        if (cloneAudioSource != null && cloneAudioClip != null)
        {
            cloneAudioSource.PlayOneShot(cloneAudioClip, cloneAudioVolume);
        }

        Debug.Log($"Clone created! Active clones: {activeClones.Count}");
    }

    private void StunAllActiveClones()
    {
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

        Debug.Log($"Stunned {activeClones.Count} clones for {cloneStunDuration} seconds.");
    }

    private void RespawnPlayerWithoutDeath()
    {
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            rb.velocity = Vector2.zero;

            // Reset platform tracking when respawning
            currentPlatformClone = null;
            platformVelocity = Vector3.zero;
            wasOnPlatform = false;

            // Audio feedback
            if (respawnAudioSource != null && respawnSoundClip != null)
            {
                respawnAudioSource.PlayOneShot(respawnSoundClip, respawnSoundVolume);
            }

            Debug.Log("Player respawned without death!");
        }
    }

   

   

    private void DestroyAllActiveClones()
    {
        foreach (GameObject clone in activeClones)
        {
            if (clone != null)
            {
                Destroy(clone);
            }
        }
        activeClones.Clear();
        Debug.Log("All active clones destroyed.");
    }

    private void OnVolumeChanged(float volume)
    {
        cloneAudioVolume = volume;
        if (cloneAudioSource != null)
        {
            cloneAudioSource.volume = cloneAudioVolume;
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }

        if (pickPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pickPoint.position, 0.1f);
        }

        // Draw pickup range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickUpRange);
    }
}

public interface IPickable
{
    void SetPhysicsAndCollidersEnabled(bool enabled);
}
