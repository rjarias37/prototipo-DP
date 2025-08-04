using UnityEngine;

public class HeroAttackController : MonoBehaviour
{
    public Animator animator;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.X))
        {
            animator.SetInteger("AttackType", 1); // Standing Horizontal
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            animator.SetInteger("AttackType", 2); // Standing Backhand
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            animator.SetInteger("AttackType", 3); // Standing 360 Low
        }

        // Resetear el AttackType cuando ya está en animación de ataque
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            animator.SetInteger("AttackType", 0);
        }
    }
}
