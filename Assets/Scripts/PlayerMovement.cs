using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


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

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.MoveLeft.performed += ctx => moveDirection = -1;
        controls.Player.MoveLeft.canceled += ctx => { if (moveDirection == -1) moveDirection = 0; };

        controls.Player.MoveRight.performed += ctx => moveDirection = 1;
        controls.Player.MoveRight.canceled += ctx => { if (moveDirection == 1) moveDirection = 0; };

        controls.Player.Jump.performed += ctx => jumpPressed = true;

        controls.Player.Pause.performed += ctx => TogglePause();
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (pauseTextUI != null)
            pauseTextUI.SetActive(false);
    }

    void Update()
    {
        if (isPaused) return;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }
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
