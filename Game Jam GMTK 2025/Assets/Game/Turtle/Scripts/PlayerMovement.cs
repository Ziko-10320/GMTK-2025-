using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Mouvement
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    private float horizontalMovement;
    public bool isFacingRight = true; // Changed to public for dash script access

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
    }

    void Update()
    {
        // Vérification du sol
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        // Mouvement horizontal
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
}
