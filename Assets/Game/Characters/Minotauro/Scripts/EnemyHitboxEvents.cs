using UnityEngine;

public class EnemyHitboxEvents : MonoBehaviour
{
    [SerializeField] private EnemyDamageHitbox weaponHitbox;

    // Llamado desde eventos de animación
    public void Anim_HitboxOn()
    {
        if (weaponHitbox != null) weaponHitbox.SetActive(true);
    }

    public void Anim_HitboxOff()
    {
        if (weaponHitbox != null) weaponHitbox.SetActive(false);
    }
}
