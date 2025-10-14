using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var keyring = other.GetComponentInParent<PlayerKeyring>();
        if (keyring == null) return;

        keyring.AddKey(amount);

        if (audioSource && pickupSfx) audioSource.PlayOneShot(pickupSfx);

        // feedback mínimo por si quieres ver que entró
        Debug.Log("[KeyPickup] llave recogida");

        gameObject.SetActive(false);
        Destroy(gameObject, 2f);
    }
}
