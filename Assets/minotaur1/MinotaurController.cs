using UnityEngine;
using UnityEngine.AI;

public class MinotaurController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Animator animator;

    [Header("Configuraci�n")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    private NavMeshAgent agent;
    private bool playerDead;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Buscar jugador de forma segura
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerDead = false;
        }
        else
        {
            playerDead = true;
            Debug.LogWarning("Jugador no encontrado!");
        }
    }

    void Update()
    {
        // 1. Verificar si el jugador existe
        if (playerDead || player == null)
        {
            FindPlayer(); // Intentar encontrar de nuevo
            agent.isStopped = true; // Detener movimiento
            animator.SetFloat("Speed", 0); // Animaci�n idle
            return;
        }

        // 2. Calcular velocidad
        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0, agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // 3. Actualizar animator
        float smoothSpeed = Mathf.Lerp(animator.GetFloat("Speed"), currentSpeed, Time.deltaTime * 5f);
        animator.SetFloat("Speed", smoothSpeed);

        // 4. Perseguir solo si el jugador est� vivo
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // 5. Rotaci�n (opcional)
        if (currentSpeed > 0.1f)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}