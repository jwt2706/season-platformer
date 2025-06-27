using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    public GameObject pauseTextUI;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool jumpPressed;
    private int moveDirection = 0;

    private PlayerControls controls;
    private bool isPaused = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;


    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.MoveLeft.performed += ctx => moveDirection = -1;
        controls.Player.MoveLeft.canceled += ctx => { if (moveDirection == -1) moveDirection = 0; };

        controls.Player.MoveRight.performed += ctx => moveDirection = 1;
        controls.Player.MoveRight.canceled += ctx => { if (moveDirection == 1) moveDirection = 0; };

        controls.Player.Jump.performed += ctx => jumpPressed = true;

        controls.Player.Pause.performed += ctx => TogglePause();

        // only allow reset when game is paused
        controls.Player.Reset.performed += ctx =>
        {
            if (isPaused)
                ResetGame();
        };
    }

    void ResetGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (pauseTextUI != null)
            pauseTextUI.SetActive(false);
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (isPaused) return;

        float horizontalVelocity = moveDirection * moveSpeed;

        Vector2 currentVelocity = rb.linearVelocity;

        // Check for wall using a Raycast (optional improvement later)
        if (!IsTouchingWall())
            currentVelocity.x = moveDirection * moveSpeed;
        else
            currentVelocity.x = 0; // stop pushing into the wall

        rb.linearVelocity = currentVelocity;


        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        // Use current velocity, not input
        float actualSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", actualSpeed);

        // Flip sprite only if actually moving
        if (Mathf.Abs(moveDirection) > 0)
            spriteRenderer.flipX = moveDirection < 0;
    }

    bool IsTouchingWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * moveDirection, 0.6f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }


    void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseTextUI != null)
            pauseTextUI.SetActive(isPaused);

        Debug.Log("Game " + (isPaused ? "Paused" : "Unpaused"));
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}
