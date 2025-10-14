using UnityEngine;
using System.Collections.Generic;

public class EnemyDamageHitbox : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int damage = 15;
    [SerializeField] private string targetTag = "Player";

    [Header("Control")]
    [SerializeField] private bool active = false;
    [SerializeField] private bool debugActivateOnStart = false;

    private Collider col;
    private HashSet<Health> hitThisSwing = new HashSet<Health>();

    public void Anim_HitboxOn() { SetActive(true); }
    public void Anim_HitboxOff() { SetActive(false); }

    void Awake()
    {
        col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        SetActive(debugActivateOnStart);
    }

    public void SetActive(bool v)
    {
        active = v;
        if (col) col.enabled = active;
        if (active) hitThisSwing.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active) return;
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        var h = other.GetComponentInParent<Health>();
        if (h == null || h.IsDead()) return;
        if (hitThisSwing.Contains(h)) return;

        h.TakeDamage(damage);
        hitThisSwing.Add(h);
    }
}
