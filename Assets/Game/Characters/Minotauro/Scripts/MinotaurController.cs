using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class MinotaurController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Jugador")]
    public Transform player;

    [Header("Configuración")]
    public float chaseRange = 10f;
    public float walkSpeed = 1.2f;
    public float runSpeed = 2f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = walkSpeed;
    }

    void Update()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= chaseRange)
            {
                // En modo persecución
                agent.speed = runSpeed;
                agent.SetDestination(player.position);
            }
            else
            {
                // Patrullaje (velocidad normal)
                agent.speed = walkSpeed;
                // Aquí puedes agregar lógica de patrullaje más adelante
            }
        }

        // Ajustar parámetro Speed del Animator
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // Pruebas de animaciones
        if (Input.GetKeyDown(KeyCode.Alpha1)) animator.SetTrigger("Attack1");
        if (Input.GetKeyDown(KeyCode.Alpha2)) animator.SetTrigger("Attack2");
        if (Input.GetKeyDown(KeyCode.Alpha3)) animator.SetTrigger("Attack3");
        if (Input.GetKeyDown(KeyCode.H)) animator.SetTrigger("Hit");
        if (Input.GetKeyDown(KeyCode.K)) animator.SetTrigger("Die");
    }
}
