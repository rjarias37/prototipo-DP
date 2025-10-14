using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class MinotaurController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    [Header("Jugador")]
    [SerializeField] private Transform player;

    [Header("Configuración")]
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float runSpeed = 2f;

    [Header("Input (Nuevo Sistema)")]
    [SerializeField] private InputActionProperty attack1;
    [SerializeField] private InputActionProperty attack2;
    [SerializeField] private InputActionProperty attack3;
    [SerializeField] private InputActionProperty hit;
    [SerializeField] private InputActionProperty die;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = walkSpeed;
    }

    void OnEnable()
    {
        EnableAndBind(attack1, OnAttack1);
        EnableAndBind(attack2, OnAttack2);
        EnableAndBind(attack3, OnAttack3);
        EnableAndBind(hit, OnHit);
        EnableAndBind(die, OnDie);
    }

    void OnDisable()
    {
        UnbindAndDisable(attack1, OnAttack1);
        UnbindAndDisable(attack2, OnAttack2);
        UnbindAndDisable(attack3, OnAttack3);
        UnbindAndDisable(hit, OnHit);
        UnbindAndDisable(die, OnDie);
    }

    void Update()
    {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= chaseRange)
        {
            agent.speed = runSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.speed = walkSpeed;
            // patrullaje opcional
        }

        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    void OnAttack1(InputAction.CallbackContext _) => animator.SetTrigger("Attack1");
    void OnAttack2(InputAction.CallbackContext _) => animator.SetTrigger("Attack2");
    void OnAttack3(InputAction.CallbackContext _) => animator.SetTrigger("Attack3");
    void OnHit(InputAction.CallbackContext _) => animator.SetTrigger("Hit");
    void OnDie(InputAction.CallbackContext _) => animator.SetTrigger("Die");

    static void EnableAndBind(InputActionProperty prop, System.Action<InputAction.CallbackContext> cb)
    {
        if (!prop.reference) return;
        var a = prop.action;
        if (a == null) return;
        a.Enable();
        a.performed += cb;
    }

    static void UnbindAndDisable(InputActionProperty prop, System.Action<InputAction.CallbackContext> cb)
    {
        if (!prop.reference) return;
        var a = prop.action;
        if (a == null) return;
        a.performed -= cb;
        a.Disable();
    }
}
