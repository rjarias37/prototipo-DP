using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HeroMovementController : MonoBehaviour
{
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float jumpForce = 8.0f;
    public float gravity = 20.0f;
    public Transform camara;


    private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;
    public Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D o flechas
        float moveZ = Input.GetAxis("Vertical");   // W/S o flechas
        Vector3 camForward = camara.forward;
        Vector3 camRight = camara.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

Vector3 move = camForward * moveZ + camRight * moveX;


        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (controller.isGrounded)
        {
            moveDirection = move.normalized * currentSpeed;

            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
                animator.SetBool("isJumping", true);
            }
        }

        // Aplicar gravedad
        moveDirection.y -= gravity * Time.deltaTime;

        // Mover al personaje
        controller.Move(moveDirection * Time.deltaTime);

        // Animaciones
        float movementMagnitude = new Vector2(moveX, moveZ).magnitude;
        animator.SetFloat("Speed", movementMagnitude * (isRunning ? 2f : 1f));

        // Si está en el suelo y no saltó, quitar el salto
        if (controller.isGrounded && moveDirection.y <= 0)
        {
            animator.SetBool("isJumping", false);
        }
    }
}
