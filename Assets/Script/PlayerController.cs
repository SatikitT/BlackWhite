using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.2f;
    public float jumpDelay = 0.1f; // Delay before the jump
    public float groundCheckRadius = 0.1f;
    public Camera mainCamera;

    public RuntimeAnimatorController sheepAnimatorController;
    public RuntimeAnimatorController wolfAnimatorController;
    public RuntimeAnimatorController transformAnimatorController;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool canDoubleJump;
    private bool isDashing;
    private float dashTime;
    private bool isJumping = false;

    private KeyCode lastKey;
    private float lastKeyTime;

    private string groundTag;
    private bool isSheep = true; // True if the player is a sheep, false if the player is a wolf

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = sheepAnimatorController;
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleDash();
        HandleCharacterSwitch();
        FollowPlayerWithCamera();
        UpdateAnimations();
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Horizontal");
        if (!isDashing)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }

        if (moveInput > 0 && transform.localScale.x < 0)
        {
            Flip();
        }
        else if (moveInput < 0 && transform.localScale.x > 0)
        {
            Flip();
        }
    }

    private void HandleJump()
    {
        bool wasGrounded = isGrounded;
        Collider2D groundCollider = Physics2D.OverlapCircle(new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), groundCheckRadius, LayerMask.GetMask("Ground"));

        if (groundCollider != null)
        {
            isGrounded = true;
            groundTag = groundCollider.tag;
            animator.ResetTrigger("LandTrigger");
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !wasGrounded && isJumping)
        {
            // Player just landed
            isJumping = false;
            animator.SetTrigger("LandTrigger"); // Set Land trigger for reversed jump animation
            canDoubleJump = true; // Reset double jump when player lands
        }

        if (Input.GetButtonDown("Jump") && isSheep && (isGrounded || canDoubleJump))
        {
            if (isGrounded || canDoubleJump)
            {
                if (!isGrounded)
                {
                    animator.SetBool("Walk", false);
                    animator.SetTrigger("IdleTrigger");
                    canDoubleJump = false; // Use double jump if not grounded
                }

                isJumping = true;
                animator.SetTrigger("JumpTrigger"); // Set jump trigger for jump animation
                StartCoroutine(JumpWithDelay());
            }
        }
    }

    private IEnumerator JumpWithDelay()
    {
        yield return new WaitForSeconds(jumpDelay);
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void HandleDash()
    {
        if (isDashing)
        {
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            rb.velocity = new Vector2(transform.localScale.x * dashSpeed, rb.velocity.y);
            dashTime -= Time.deltaTime;

            if (dashTime <= 0)
            {
                isDashing = false;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Unfreeze Y position after dashing
            }
        }
        else if (!isSheep && groundTag == "BlackGround")
        {
            if (DoubleTapCheck(KeyCode.A) || DoubleTapCheck(KeyCode.LeftArrow))
            {
                StartDash(-1);
            }
            else if (DoubleTapCheck(KeyCode.D) || DoubleTapCheck(KeyCode.RightArrow))
            {
                StartDash(1);
            }
        }
    }

    private void StartDash(int direction)
    {
        isDashing = true;
        dashTime = dashDuration;
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = new Vector2(direction * dashSpeed, rb.velocity.y);
        animator.SetTrigger("DashTrigger"); // Trigger dash animation
    }

    private bool DoubleTapCheck(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            if (lastKey == key && Time.time - lastKeyTime < 0.3f)
            {
                lastKey = KeyCode.None;
                return true;
            }

            lastKey = key;
            lastKeyTime = Time.time;
        }

        return false;
    }

    private void Flip()
    {
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void FollowPlayerWithCamera()
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = transform.position.x;
        cameraPosition.y = transform.position.y;
        mainCamera.transform.position = cameraPosition;
    }

    private void HandleCharacterSwitch()
    {
        if ((groundTag == "WhiteGround" && !isSheep) || (groundTag == "BlackGround" && isSheep))
        {
            isSheep = groundTag == "WhiteGround";
            animator.runtimeAnimatorController = transformAnimatorController; // Use the transform animation controller
            animator.SetTrigger("TransformTrigger");

            StartCoroutine(SwitchCharacterAfterDelay(isSheep ? sheepAnimatorController : wolfAnimatorController));
        }
    }

    private IEnumerator SwitchCharacterAfterDelay(RuntimeAnimatorController newController)
    {
        // Wait for the transform animation to complete
        animator.runtimeAnimatorController = newController;
        transformAnimatorController = animator.runtimeAnimatorController == sheepAnimatorController ? wolfAnimatorController : sheepAnimatorController;
        yield return new WaitForSeconds(1f);
    }

    private void UpdateAnimations()
    {
        float moveInput = Input.GetAxis("Horizontal");
        if (!isJumping)
        {
            animator.SetBool("Walk", moveInput != 0);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), groundCheckRadius);
    }
}
