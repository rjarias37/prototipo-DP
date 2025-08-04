using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;

    [Header("Animaciones")]
    public Animator animator;

    [Header("Referencia Cámara")]
    public CameraController cameraController;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // --- CORRECCIÓN 1: Usar GetAxisRaw para eliminar el deslizamiento ---
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        // --- CORRECCIÓN 2: Invertir los ejes para que las teclas funcionen correctamente ---
        // Si solo W/S están invertidas, quita el "-" de "horizontal"
        Vector3 move = transform.right * -horizontal + transform.forward * -vertical;
        
        controller.Move(move.normalized * speed * Time.deltaTime);

        // Animar caminar
        bool isWalking = move.magnitude > 0.1f;
        animator.SetBool("isWalking", isWalking);

        // Atacar solo si está en tercera persona
        if (!cameraController.isFirstPerson && Input.GetButtonDown("Fire1"))
            animator.SetTrigger("Attack");

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
