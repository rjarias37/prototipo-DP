using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private DoorController door;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!door) door = GetComponentInParent<DoorController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var keyring = other.GetComponentInParent<PlayerKeyring>();
        bool opened = door && door.TryOpen(keyring);

        // (Opcional) feedback si no se abre:
        // if (!opened) Debug.Log("Puerta cerrada: necesitas una llave.");
    }
}

