using UnityEngine;
using UnityEngine.InputSystem;

public class HeroAttackController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTypeParam = "AttackType";
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField, Range(0.8f, 1f)] private float forceExitAt = 0.98f;

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionProperty attack1;
    [SerializeField] private InputActionProperty attack2;
    [SerializeField] private InputActionProperty attack3;

    private bool isDead;

    void Awake()
    {
        var h = GetComponent<Health>();
        if (h) h.onDeath.AddListener(() => isDead = true);
    }

    void OnEnable()
    {
        Enable(attack1, OnAttack1);
        Enable(attack2, OnAttack2);
        Enable(attack3, OnAttack3);
    }

    void OnDisable()
    {
        Disable(attack1, OnAttack1);
        Disable(attack2, OnAttack2);
        Disable(attack3, OnAttack3);
    }

    void Update()
    {
        if (!animator) return;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsTag("Attack"))
        {
            animator.SetInteger(attackTypeParam, 0);
            if (!string.IsNullOrEmpty(idleStateName) && st.normalizedTime >= forceExitAt)
                animator.CrossFade(idleStateName, 0.08f, 0, 0f);
        }
    }

    void OnAttack1(InputAction.CallbackContext _) => TryAttack(1);
    void OnAttack2(InputAction.CallbackContext _) => TryAttack(2);
    void OnAttack3(InputAction.CallbackContext _) => TryAttack(3);

    void TryAttack(int type)
    {
        if (isDead || !animator) return;
        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsTag("Attack")) return;
        animator.SetInteger(attackTypeParam, type);
    }

    static void Enable(InputActionProperty a, System.Action<InputAction.CallbackContext> cb)
    {
        if (!a.reference) return;
        a.action.Enable();
        a.action.performed += new System.Action<InputAction.CallbackContext>(cb);
    }

    static void Disable(InputActionProperty a, System.Action<InputAction.CallbackContext> cb)
    {
        if (!a.reference) return;
        a.action.performed -= new System.Action<InputAction.CallbackContext>(cb);
        a.action.Disable();
    }
}
