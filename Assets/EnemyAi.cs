using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform target; // Jugador
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    private NavMeshAgent agent;
    private Animator animator; // Si usas animaciones
    private bool playerInSight = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("FALTA el componente NavMeshAgent en " + gameObject.name);
        }
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        if (distanceToPlayer <= detectionRange)
        {
            playerInSight = true;
            agent.speed = runSpeed;
            agent.SetDestination(target.position);

            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        playerInSight = false;
        agent.speed = walkSpeed;

        // Aqu� puedes poner l�gica de patrullaje o waypoint
        // esto es una prueba para git
        agent.SetDestination(transform.position);
    }

    void AttackPlayer()
    {
        Debug.Log("�Jugador atacado!");
        // Aqu� puedes colocar una animaci�n de ataque o l�gica de da�o
        Destroy(target.gameObject); // Esto simula que el jugador muere
    }
}
