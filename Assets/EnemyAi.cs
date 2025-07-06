using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    private enum EnemyState { Patrolling, Chasing, Attacking }

    [Header("Referencias")]
    public Transform target;

    [Header("Rangos")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    [Header("Velocidades")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Patrullaje")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Animaciones")]
    public int attackVariants = 3; // Ajusta este número al máximo de variantes que tienes configuradas

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyState currentState = EnemyState.Patrolling;
    private float lastAttackTime = 0f;
    private bool playerDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("❌ Falta NavMeshAgent en " + gameObject.name);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Vector3 sourcePosition = transform.position;
            NavMeshHit closestHit;
            if (NavMesh.SamplePosition(sourcePosition, out closestHit, 500f, NavMesh.AllAreas))
            {
                agent.Warp(closestHit.position);
            }
            else
            {
                Debug.LogError("❌ EnemyAI: No encontré un punto cercano en la NavMesh.");
                enabled = false;
                return;
            }
        }

        FindPlayer();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.speed = walkSpeed;
            agent.SetDestination(patrolPoints[0].position);
            currentState = EnemyState.Patrolling;
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            playerDead = false;
        }
        else
        {
            playerDead = true;
        }
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Actualizar animación de velocidad
        UpdateSpeedAnimation();

        if (playerDead || target == null)
        {
            FindPlayer();
            if (target == null)
            {
                agent.isStopped = true;
                if (animator != null) animator.SetFloat("Speed", 0);
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolState(dist);
                break;

            case EnemyState.Chasing:
                HandleChaseState(dist);
                break;

            case EnemyState.Attacking:
                HandleAttackState(dist);
                break;
        }
    }

    void HandlePatrolState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
            agent.speed = runSpeed;
            agent.SetDestination(target.position);
            return;
        }

        Patrol();
    }

    void HandleChaseState(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Patrolling;
            agent.speed = walkSpeed;

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            return;
        }

        agent.SetDestination(target.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            agent.isStopped = true;
        }
    }

    void HandleAttackState(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chasing;
            agent.isStopped = false;
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void AttackPlayer()
    {
        if (animator == null) return;

        int attackIndex = Random.Range(1, attackVariants + 1);

        // Solo activar el trigger si existe el parámetro
        switch (attackIndex)
        {
            case 1:
                if (ParameterExists("Attack1")) animator.SetTrigger("Attack1");
                break;
            case 2:
                if (ParameterExists("Attack2")) animator.SetTrigger("Attack2");
                break;
            case 3:
                if (ParameterExists("Attack3")) animator.SetTrigger("Attack3");
                break;
            case 4:
                if (ParameterExists("Attack4")) animator.SetTrigger("Attack4");
                break;
            case 5:
                if (ParameterExists("Attack5")) animator.SetTrigger("Attack5");
                break;
        }
    }

    bool ParameterExists(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void UpdateSpeedAnimation()
    {
        if (animator == null) return;

        Vector3 horizontalVelocity = new Vector3(agent.velocity.x, 0, agent.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Si la velocidad es muy baja pero el agente está activo, usar una velocidad mínima
        if (currentSpeed < 0.1f && agent.pathPending)
        {
            currentSpeed = 0.5f;
        }

        float smoothSpeed = Mathf.Lerp(animator.GetFloat("Speed"), currentSpeed, Time.deltaTime * 5f);
        animator.SetFloat("Speed", smoothSpeed);
    }

    // Llamado desde eventos de animación
    public void DealDamage()
    {
        Debug.Log("🔥 Jugador dañado!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}