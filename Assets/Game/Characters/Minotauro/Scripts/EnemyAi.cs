using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;
    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    [Header("Rangos de detección")]
    public float detectionRange = 30f;
    public float fieldOfView = 120f;
    public float attackRange = 2.5f;
    public float attackCooldown = 3f;

    [Header("Velocidades")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 7f;

    [Header("Búsqueda al perder de vista")]
    public float searchDuration = 5f;
    public float wanderRadius = 8f;

    [Header("Sonido")]
    public AudioClip detectionLaugh;

    private bool hasLineOfSight = false;
    private float lostSightTimer = 0f;
    private Vector3 lastKnownPosition;
    private bool isSearching = false;
    private bool isAttacking = false;
    private bool hasPlayedLaugh = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        agent.speed = walkSpeed;
    }

    void Update()
    {
        Vector3 directionToPlayer = (target.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        bool withinView = angleToPlayer < fieldOfView * 0.5f;
        bool withinRange = distanceToPlayer <= detectionRange;

        NavMeshHit hit;
        bool hasClearLine = !NavMesh.Raycast(transform.position, target.position, out hit, NavMesh.AllAreas);

        bool canSeePlayer = withinView && withinRange && hasClearLine;

        if (canSeePlayer)
        {
            hasLineOfSight = true;
            isSearching = false;
            lostSightTimer = 0f;
            lastKnownPosition = target.position;

            if (!hasPlayedLaugh && detectionLaugh != null)
            {
                audioSource.PlayOneShot(detectionLaugh);
                hasPlayedLaugh = true;
            }

            MoveTowardTarget(target.position);

            if (distanceToPlayer <= attackRange)
            {
                if (!isAttacking)
                {
                    agent.isStopped = true;

                    animator.ResetTrigger("Attack1");
                    animator.ResetTrigger("Attack2");
                    animator.ResetTrigger("Attack3");

                    int rand = Random.Range(1, 4);
                    animator.SetTrigger($"Attack{rand}");

                    isAttacking = true;
                    Invoke(nameof(ResetAttack), attackCooldown);
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }
        else if (hasLineOfSight)
        {
            hasLineOfSight = false;
            isSearching = true;
            hasPlayedLaugh = false;
            lostSightTimer = 0f;
            agent.speed = walkSpeed;
            agent.SetDestination(lastKnownPosition);
        }
        else if (isSearching)
        {
            lostSightTimer += Time.deltaTime;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                WanderAround();
            }

            if (lostSightTimer > searchDuration)
            {
                WanderAround();
                lostSightTimer = 0f;
            }

            animator.SetFloat("Speed", 1.5f); // caminar mientras busca
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    void MoveTowardTarget(Vector3 destination)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, destination);

        if (distanceToPlayer > attackRange + 0.5f)
        {
            agent.isStopped = false;
            agent.speed = runSpeed;

            // Forzar siempre animación de correr mientras persigue
            animator.SetFloat("Speed", 7f);
        }
        else
        {
            // Nos acercamos mucho, detenemos movimiento y animación
            agent.velocity = Vector3.zero;
            agent.isStopped = true;

            animator.SetFloat("Speed", 0f);
        }

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(destination, out navHit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }


    void WanderAround()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius + transform.position;
        NavMeshHit navHit;

        if (NavMesh.SamplePosition(randomDir, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    void ResetAttack()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -fieldOfView / 2, 0) * forward;
        Vector3 rightLimit = Quaternion.Euler(0, fieldOfView / 2, 0) * forward;

        Gizmos.DrawRay(transform.position + Vector3.up, leftLimit * detectionRange);
        Gizmos.DrawRay(transform.position + Vector3.up, rightLimit * detectionRange);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
