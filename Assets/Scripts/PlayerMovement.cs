using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Wrap Settings")]
    public float wrapXMin = -16f;
    public float wrapXMax = 16f;

    [Header("UI References")]
    public GameObject pauseTextUI;
    public TextMeshProUGUI pauseScoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool jumpPressed;
    private int moveDirection = 0;

    private PlayerControls controls;
    private bool isPaused = false;
    private bool isGameOver = false;

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

        controls.Player.Reset.performed += ctx =>
        {
            if (isPaused || isGameOver)
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
        rb.freezeRotation = true;

        if (pauseTextUI != null)
            pauseTextUI.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isPaused || isGameOver) return;

        HandleMovement();
        CheckGameOver();
    }

    void HandleMovement()
    {
        float horizontalVelocity = moveDirection * moveSpeed;
        Vector2 currentVelocity = rb.linearVelocity;

        if (!IsTouchingWall())
            currentVelocity.x = moveDirection * moveSpeed;
        else
            currentVelocity.x = 0;

        rb.linearVelocity = currentVelocity;

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        float actualSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", actualSpeed);
        animator.SetBool("IsGrounded", isGrounded);

        if (Mathf.Abs(moveDirection) > 0)
            spriteRenderer.flipX = moveDirection < 0;

        Vector3 pos = transform.position;
        if (pos.x < wrapXMin) pos.x = wrapXMax;
        else if (pos.x > wrapXMax) pos.x = wrapXMin;
        transform.position = pos;
    }

    void CheckGameOver()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetTimeRemaining() <= 0)
        {
            isGameOver = true;
            Time.timeScale = 0f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (finalScoreText != null)
                finalScoreText.text = $"Game Over!\nFinal Score: {GameManager.Instance.GetScore()}\n\nPress (r) or (select) to reset.";

            Debug.Log("Game Over! Final Score: " + GameManager.Instance.GetScore());
        }
    }

    bool IsTouchingWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * moveDirection, 0.6f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }

    void TogglePause()
    {
        if (isGameOver) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseTextUI != null)
            pauseTextUI.SetActive(isPaused);

        if (pauseScoreText != null)
            pauseScoreText.text = $"Game Puased!\nCurrent Score: {GameManager.Instance.GetScore()}\n\nPress (p) or (start) to continue.\nPress (r) or (select) to reset.";

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
