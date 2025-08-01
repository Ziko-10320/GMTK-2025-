using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClonePlayback : MonoBehaviour
{
    [Header("Clone Settings")]
    [SerializeField] private float playbackSpeed = 0.75f;
    [SerializeField] private bool debugMode = false;

    // Playback data
    private List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
    private float playbackTime = 0f;
    private bool isPlaying = false;
    private bool isFrozen = false; // Nouvelle variable pour indiquer si le clone est gelé
    private float snapshotInterval = 0.02f; // Should match the recording interval

    // Seamless loop variables
    private bool playingForward = true;
    private float totalRecordingDuration;

    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private GhostEffect ghostEffect;

    // Variables pour sauvegarder l'état avant l'étourdissement
    private float originalAnimatorSpeed;
    private bool originalGhostEffectActive;

    // Animation state tracking
    private string lastAnimationState = "";
    private bool lastFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        ghostEffect = GetComponent<GhostEffect>();

        // Disable physics for the clone initially
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Start ghost effect for the clone permanently
        if (ghostEffect != null)
        {
            // Ensure GhostEffect is initialized with all child SpriteRenderers
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
            ghostEffect.Initialize(childRenderers);
            ghostEffect.StartGhostEffect();
        }

        originalAnimatorSpeed = animator.speed; // Sauvegarder la vitesse originale de l'Animator
        originalGhostEffectActive = ghostEffect != null ? ghostEffect.IsEffectActive() : false; // Sauvegarder l'état original du GhostEffect
    }

    void Update()
    {
        if (isPlaying && snapshots.Count > 0 && !isFrozen)
        {
            PlaybackUpdate();
        }
    }

    public void InitializeClone(List<PlayerSnapshot> recordedSnapshots, float speed)
    {
        snapshots = new List<PlayerSnapshot>(recordedSnapshots);
        playbackSpeed = speed;
        playbackTime = 0f;
        isPlaying = true;
        playingForward = true; // Start playing forward

        if (snapshots.Count > 0)
        {
            // Calculate total duration based on the actual recorded timestamps
            totalRecordingDuration = snapshots[snapshots.Count - 1].timestamp - snapshots[0].timestamp;
            // Fallback if duration is zero (e.g., only one snapshot or very short recording)
            if (totalRecordingDuration <= 0) totalRecordingDuration = (snapshots.Count - 1) * snapshotInterval;
            if (totalRecordingDuration <= 0) totalRecordingDuration = snapshotInterval; // Ensure it's never zero

            // Set initial position and state immediately
            ApplySnapshot(snapshots[0]);
        }

        if (debugMode)
        {
            Debug.Log($"Clone initialized with {snapshots.Count} snapshots at speed {playbackSpeed}. Total duration: {totalRecordingDuration}");
        }
    }

    private void PlaybackUpdate()
    {
        // Update playback time based on direction and speed
        if (playingForward)
        {
            playbackTime += Time.deltaTime * playbackSpeed;
        }
        else
        {
            playbackTime -= Time.deltaTime * playbackSpeed;
        }

        // Handle looping and direction change
        if (playingForward)
        {
            if (playbackTime >= totalRecordingDuration)
            {
                playbackTime = totalRecordingDuration; // Clamp to end of forward path
                playingForward = false; // Switch to backward
                if (debugMode) Debug.Log("Clone playback reached end, switching to backward.");
            }
        }
        else // playingBackward
        {
            if (playbackTime <= 0f)
            {
                playbackTime = 0f; // Clamp to start of backward path
                playingForward = true; // Switch to forward
                if (debugMode) Debug.Log("Clone playback reached start, switching to forward.");
            }
        }

        // Calculate current snapshot indices for interpolation
        // Find the two snapshots to interpolate between based on playbackTime
        int index1 = 0;
        for (int i = 0; i < snapshots.Count - 1; i++)
        {
            if (playbackTime >= (snapshots[i].timestamp - snapshots[0].timestamp) && playbackTime < (snapshots[i + 1].timestamp - snapshots[0].timestamp))
            {
                index1 = i;
                break;
            }
            index1 = i; // In case playbackTime is at or beyond the last snapshot
        }
        int index2 = index1 + 1;

        // Clamp indices to valid range
        index1 = Mathf.Clamp(index1, 0, snapshots.Count - 1);
        index2 = Mathf.Clamp(index2, 0, snapshots.Count - 1);

        // Interpolate between snapshots
        if (snapshots.Count > 1)
        {
            PlayerSnapshot snap1 = snapshots[index1];
            PlayerSnapshot snap2 = snapshots[index2];

            float lerpFactor = 0f;
            if (snap2.timestamp - snap1.timestamp > 0.0001f) // Avoid division by zero
            {
                lerpFactor = (playbackTime - (snap1.timestamp - snapshots[0].timestamp)) / (snap2.timestamp - snap1.timestamp);
            }
            lerpFactor = Mathf.Clamp01(lerpFactor);

            // Apply interpolated position and rotation
            transform.position = Vector3.Lerp(snap1.position, snap2.position, lerpFactor);
            transform.rotation = Quaternion.Lerp(snap1.rotation, snap2.rotation, lerpFactor);

            // Apply facing direction and animation state from the closest snapshot (or interpolated if possible)
            // For smoother animation, we can blend between states or use the state of the dominant snapshot
            // For now, we'll use the state of snap1 and let Unity's Animator handle transitions.
            SetFacingDirection(snap1.isFacingRight);
            SetAnimationState(snap1.animationState);

            if (rb != null)
            {
                rb.velocity = Vector2.Lerp(snap1.velocity, snap2.velocity, lerpFactor);
            }
        }
        else if (snapshots.Count == 1)
        {
            ApplySnapshot(snapshots[0]); // Only one snapshot, just apply it
        }
    }

    private void ApplySnapshot(PlayerSnapshot snapshot)
    {
        transform.position = snapshot.position;
        transform.rotation = snapshot.rotation;

        SetFacingDirection(snapshot.isFacingRight);
        SetAnimationState(snapshot.animationState);

        if (rb != null)
        {
            rb.velocity = snapshot.velocity;
        }
    }

    private void SetFacingDirection(bool facingRight)
    {
        Vector3 scale = transform.localScale;

        if (facingRight && scale.x < 0)
        {
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (!facingRight && scale.x > 0)
        {
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void SetAnimationState(string animationState)
    {
        if (animator == null) return;

        // Reset all animation booleans
        animator.SetBool("isRunning", false);

        switch (animationState)
        {
            case "Run":
                animator.SetBool("isRunning", true);
                break;
            case "Jump":
                // Jump animation is usually handled by velocity or ground check
                // You might need to adjust this based on your animator setup
                break;
            case "Dash":
                animator.SetTrigger("Dash");
                break;
            case "Idle":
            default:
                // Idle is the default state when no other animations are active
                break;
        }
    }

    // Public methods for external control
    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = speed;
    }

    public void StunClone(float duration)
    {
        if (isFrozen) return; // Already frozen

        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isFrozen = true;
        // Sauvegarder l'état actuel du clone pour le restaurer après l'étourdissement
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // Geler le clone en désactivant la lecture et en rendant le Rigidbody cinématique
        isPlaying = false; // Arrêter la lecture
        if (rb != null)
        {
            rb.isKinematic = true; // Geler la physique
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Geler l'animation
        if (animator != null)
        {
            animator.speed = 0f; // Mettre en pause l'animation
        }

        // Désactiver le GhostEffect
        if (ghostEffect != null)
        {
            ghostEffect.StopGhostEffect();
        }

        // S'assurer que le clone reste à sa position et rotation actuelles
        transform.position = currentPosition;
        transform.rotation = currentRotation;

        Debug.Log($"Clone stunned for {duration} seconds at {currentPosition}.");

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        isPlaying = true; // Reprendre la lecture

        // Restaurer l'état physique
        if (rb != null)
        {
            rb.isKinematic = false; // Restaurer la physique
        }

        // Restaurer l'animation
        if (animator != null)
        {
            animator.speed = originalAnimatorSpeed; // Restaurer la vitesse originale de l'animation
        }

        // Restaurer le GhostEffect
        if (ghostEffect != null && originalGhostEffectActive)
        {
            ghostEffect.StartGhostEffect();
        }

        Debug.Log("Clone stun ended. Resuming playback.");
    }

    public void PausePlayback()
    {
        isPlaying = false;
    }

    public void ResumePlayback()
    {
        isPlaying = true;
    }

    public void RestartPlayback()
    {
        playbackTime = 0f;
        isPlaying = true;
        playingForward = true;

        if (snapshots.Count > 0)
        {
            ApplySnapshot(snapshots[0]); // Apply initial state immediately
        }
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public int GetSnapshotCount()
    {
        return snapshots.Count;
    }

    public float GetPlaybackProgress()
    {
        if (totalRecordingDuration == 0) return 0f;
        return playbackTime / totalRecordingDuration;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (debugMode && snapshots != null && snapshots.Count > 0)
        {
            // Draw the path the clone will follow
            Gizmos.color = Color.cyan;
            for (int i = 0; i < snapshots.Count - 1; i++)
            {
                Gizmos.DrawLine(snapshots[i].position, snapshots[i + 1].position);
            }

            // Draw current position
            if (snapshots.Count > 0)
            {
                // Find the snapshot closest to current playbackTime for Gizmo drawing
                int closestIndex = Mathf.FloorToInt(playbackTime / snapshotInterval);
                closestIndex = Mathf.Clamp(closestIndex, 0, snapshots.Count - 1);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(snapshots[closestIndex].position, 0.3f);
            }
        }
    }
}