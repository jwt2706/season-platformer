using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool jumpPressed;
    private int moveDirection = 0; // -1 for left, 1 for right, 0 for none

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();

        // Movement
        controls.Player.MoveLeft.performed += ctx => moveDirection = -1;
        controls.Player.MoveLeft.canceled += ctx => { if (moveDirection == -1) moveDirection = 0; };

        controls.Player.MoveRight.performed += ctx => moveDirection = 1;
        controls.Player.MoveRight.canceled += ctx => { if (moveDirection == 1) moveDirection = 0; };

        // Jump
        controls.Player.Jump.performed += ctx => jumpPressed = true;
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }
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
