using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Salud")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Eventos (opcional)")]
    public UnityEvent onDeath;
    public UnityEvent<int> onHealthChanged;

    private Animator animator;

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead()) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        onHealthChanged?.Invoke(currentHealth);

        if (IsDead())
        {
            animator?.SetTrigger("Die");
            onDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead()) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(currentHealth);
    }

    public bool IsDead() => currentHealth <= 0;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth);
    }
}
