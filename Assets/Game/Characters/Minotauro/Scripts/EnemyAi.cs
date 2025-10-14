using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    [Header("Sensing")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float losHeightSelf = 1.4f;
    [SerializeField] private float losHeightPlayer = 1.4f;
    [SerializeField] private LayerMask losMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float loseSightGrace = 0.6f;
    [SerializeField] private float attackRange = 2.5f; // cuándo intenta atacar

    [Header("Movement")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolRepathTime = 3f;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 5.0f;
    [SerializeField] private float stoppingDistance = 1.2f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private bool randomizeAttackVariant = true;

    [Header("Damage")]
    [SerializeField] private int damage = 999;          // letal en nivel 1
    // MODO NUEVO: cápsula de impacto para ampliar hit-zone
    [SerializeField] private bool useCapsuleHit = true;
    [SerializeField] private float hitCapsuleLength = 2.9f; // alcance hacia delante
    [SerializeField] private float hitCapsuleRadius = 0.9f; // “grosor” del tubo
    //[SerializeField] private float hitCapsuleHeight = 0.0f; // 0 = cápsula horizontal (recomendado)
    [SerializeField] private float hitCenterHeight = 1.1f;  // altura del centro de la cápsula
    // MODO LEGADO (por si quieres volver)
    [SerializeField] private float hitRange = 2.6f;
    [SerializeField] private float hitFwdAngle = 120f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip evilLaugh;
    [SerializeField] private bool laughOnlyOnce = true;

    [Header("Debug")]
    [SerializeField] private bool debugAnimator = false;
    [SerializeField] private bool debugHit = false;

    // Estado
    private bool sawTargetPrev;
    private bool laughedOnce;
    private float lastAttackTime;
    private float seenUntil;

    // Patrulla
    private Vector3 patrolOrigin;
    private Vector3 patrolTarget;
    private float nextPatrolChooseTime;

    // Animator: Attack* como Trigger o Bool
    [SerializeField] private string attackParamPrefix = "Attack";
    private readonly List<(string name, int hash)> attackTriggerParams = new();
    private readonly List<(string name, int hash)> attackBoolParams = new();
    public List <AnimationClip> attackClip=new(); 

    // Muerte del jugador
    private bool playerDead;
    private Health playerHealth;

    // Buffer sin GC para OverlapCapsule
    private readonly Collider[] hitBuffer = new Collider[8];

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (sfx == null)
        {
            sfx = GetComponent<AudioSource>();
            if (sfx == null) sfx = GetComponentInChildren<AudioSource>();
        }
        if (sfx != null)
        {
            sfx.playOnAwake = false;
            if (sfx.spatialBlend < 0.99f) sfx.spatialBlend = 1f;
        }

        patrolOrigin = transform.position;
        if (animator != null) animator.applyRootMotion = false; // evita patinaje
    }

    private void Start()
    {
        CacheAnimatorParams();

        // Seguridad al iniciar
        for (int i = 0; i < attackTriggerParams.Count; i++)
            animator.ResetTrigger(attackTriggerParams[i].hash);
        for (int i = 0; i < attackBoolParams.Count; i++)
            animator.SetBool(attackBoolParams[i].hash, false);

        agent.stoppingDistance = stoppingDistance;
        agent.speed = patrolSpeed;
        laughedOnce = false;

        TryBindPlayerHealth();
    }

    private void TryBindPlayerHealth()
    {
        playerHealth = null;
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>() ??
                           player.GetComponentInParent<Health>() ??
                           player.GetComponentInChildren<Health>();
            if (playerHealth != null)
                playerHealth.onDeath.AddListener(OnPlayerDied);
        }
    }

    private void OnPlayerDied()
    {
        playerDead = true;
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        for (int i = 0; i < attackTriggerParams.Count; i++)
            animator.ResetTrigger(attackTriggerParams[i].hash);
        for (int i = 0; i < attackBoolParams.Count; i++)
            animator.SetBool(attackBoolParams[i].hash, false);
    }

    private void CacheAnimatorParams()
    {
        attackTriggerParams.Clear();
        attackBoolParams.Clear();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                if (debugAnimator) Debug.LogWarning("EnemyAI: No se encontró Animator.");
                return;
            }
        }

        if (animator.runtimeAnimatorController == null)
        {
            if (debugAnimator) Debug.LogWarning("EnemyAI: Animator sin RuntimeAnimatorController.");
            return;
        }

        var ps = animator.parameters;
        if (debugAnimator)
        {
            var sb = new System.Text.StringBuilder("EnemyAI: Parámetros detectados -> ");
            for (int i = 0; i < ps.Length; i++) sb.Append($"[{ps[i].type}:{ps[i].name}] ");
            Debug.Log(sb.ToString());
        }

        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            if (!p.name.StartsWith(attackParamPrefix)) continue;

            if (p.type == AnimatorControllerParameterType.Trigger)
                attackTriggerParams.Add((p.name, Animator.StringToHash(p.name)));
            else if (p.type == AnimatorControllerParameterType.Bool)
                attackBoolParams.Add((p.name, Animator.StringToHash(p.name)));
        }
    }

    private void Update()
    {
        if (player == null || agent == null || animator == null) return;

        if (playerDead)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            return;
        }

        bool sees = ComputeDetection();

        if (sees && !sawTargetPrev && !laughedOnce && evilLaugh != null && sfx != null)
        {
            sfx.PlayOneShot(evilLaugh);
            if (laughOnlyOnce) laughedOnce = true;
        }

        if (sees)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            float dist = Vector3.Distance(transform.position, player.position);
            bool byDist = dist <= attackRange;
            bool byPath = agent.hasPath && agent.remainingDistance <= attackRange + 0.15f;
            if (byDist && byPath)
                TryAttack();
        }
        else
        {
            PatrolUpdate();
        }

        sawTargetPrev = sees;
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void PatrolUpdate()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        bool needNew =
            Time.time >= nextPatrolChooseTime ||
            (patrolTarget != Vector3.zero && Vector3.Distance(transform.position, patrolTarget) <= agent.stoppingDistance + 0.3f);

        if (needNew)
        {
            patrolTarget = FindRandomPoint(patrolOrigin, patrolRadius);
            nextPatrolChooseTime = Time.time + patrolRepathTime;
        }

        if (patrolTarget != Vector3.zero)
            agent.SetDestination(patrolTarget);
    }

    private Vector3 FindRandomPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 sample = origin + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(sample, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    private bool ComputeDetection()
    {
        Vector3 toPlayer = player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;
        float maxSqr = detectionRadius * detectionRadius;

        if (sqrDist > maxSqr && Time.time > seenUntil)
            return false;

        if (sqrDist <= maxSqr)
        {
            Vector3 origin = transform.position + Vector3.up * losHeightSelf;
            Vector3 target = player.position + Vector3.up * losHeightPlayer;
            Vector3 dir = target - origin;
            float len = dir.magnitude;
            if (len > 0.0001f)
            {
                dir /= len;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, len, losMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == player || hit.transform.IsChildOf(player))
                    {
                        seenUntil = Time.time + loseSightGrace;
                        return true;
                    }
                }
            }
        }

        return Time.time <= seenUntil;
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        if (attackTriggerParams.Count == 0 && attackBoolParams.Count == 0)
            CacheAnimatorParams();

        if (attackTriggerParams.Count > 0)
        {
            int index = randomizeAttackVariant ? Random.Range(0, attackTriggerParams.Count) : 0;
            animator.ResetTrigger(attackTriggerParams[index].hash);
            animator.SetTrigger(attackTriggerParams[index].hash);
        }
        else if (attackBoolParams.Count > 0)
        {
            int index = randomizeAttackVariant ? Random.Range(0, attackBoolParams.Count) : 0;
            StartCoroutine(PulseBoolAttack(attackBoolParams[index].hash));
        }

        lastAttackTime = Time.time;
    }

    // === Evento de Animación (impacto) ===
    private void Anim_DealDamage()
    {
        if (playerDead || player == null) return;

        if (useCapsuleHit)
        {
            // Cápsula horizontal delante del minotauro
            Vector3 baseCenter = transform.position + Vector3.up * hitCenterHeight;
            Vector3 p0 = baseCenter;
            Vector3 p1 = baseCenter + transform.forward * hitCapsuleLength;

            int count = Physics.OverlapCapsuleNonAlloc(p0, p1, hitCapsuleRadius, hitBuffer, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var col = hitBuffer[i];
                if (col == null) continue;

                // Solo aceptar al jugador o sus hijos
                if (col.transform != player && !col.transform.IsChildOf(player)) continue;

                // Línea de visión para no golpear a través de muros
                Vector3 origin = baseCenter;
                Vector3 target = player.position + Vector3.up * losHeightPlayer;
                Vector3 dir = (target - origin);
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    dir /= len;
                    if (Physics.Raycast(origin, dir, out RaycastHit hit, len, losMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.transform != player && !hit.transform.IsChildOf(player)) continue;
                    }
                }

                var hp = player.GetComponent<Health>() ?? player.GetComponentInParent<Health>() ?? player.GetComponentInChildren<Health>();
                if (hp != null) hp.TakeDamage(damage);
                return; // ya golpeó
            }
            return;
        }

        // ——— Modo legado (cono) ———
        Vector3 o = transform.position + Vector3.up * 1.2f;
        Vector3 t = player.position + Vector3.up * 1.0f;
        Vector3 toPlayer = t - o;
        float dist = toPlayer.magnitude;
        if (dist <= hitRange && dist > 0.001f)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle <= hitFwdAngle * 0.5f)
            {
                if (Physics.Raycast(o, toPlayer.normalized, out RaycastHit hit2, dist + 0.1f, losMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit2.transform == player || hit2.transform.IsChildOf(player))
                    {
                        var hp = player.GetComponent<Health>() ?? player.GetComponentInParent<Health>() ?? player.GetComponentInChildren<Health>();
                        if (hp != null) hp.TakeDamage(damage);
                    }
                }
            }
        }
    }

    private IEnumerator PulseBoolAttack(int boolHash)
    {
        for (int i = 0; i < attackBoolParams.Count; i++)
            animator.SetBool(attackBoolParams[i].hash, false);

        animator.SetBool(boolHash, true);
        yield return null;
        animator.SetBool(boolHash, false);
    }

    private void OnDrawGizmosSelected()
    {
        // Detección y ataque
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Patrulla
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(patrolOrigin == Vector3.zero ? transform.position : patrolOrigin, patrolRadius);

        // LOS debug
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * losHeightSelf;
        Vector3 target = (player != null ? player.position : transform.position) + Vector3.up * losHeightPlayer;
        Gizmos.DrawLine(origin, target);

        // Cápsula de golpe (si está activa)
        if (useCapsuleHit)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Vector3 baseCenter = transform.position + Vector3.up * hitCenterHeight;
            Vector3 p0 = baseCenter;
            Vector3 p1 = baseCenter + transform.forward * hitCapsuleLength;
            Gizmos.DrawWireSphere(p0, hitCapsuleRadius);
            Gizmos.DrawWireSphere(p1, hitCapsuleRadius);
            Gizmos.DrawLine(p0 + Vector3.up * hitCapsuleRadius, p1 + Vector3.up * hitCapsuleRadius);
            Gizmos.DrawLine(p0 - Vector3.up * hitCapsuleRadius, p1 - Vector3.up * hitCapsuleRadius);
            Gizmos.DrawLine(p0 + Vector3.right * hitCapsuleRadius, p1 + Vector3.right * hitCapsuleRadius);
            Gizmos.DrawLine(p0 - Vector3.right * hitCapsuleRadius, p1 - Vector3.right * hitCapsuleRadius);
        }
    }
}
