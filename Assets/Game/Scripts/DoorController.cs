using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class DoorController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Collider blockCollider;      // BoxCollider que BLOQUEA el paso
    [SerializeField] private NavMeshObstacle navObstacle; // Obstacle con Carve
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSfx;

    [Header("Logic")]
    [SerializeField] private bool requiresKey = true;
    [SerializeField] private bool consumesKey = true;     // Si quieres que no se gaste, pon false

    [Header("Exit (Opcional)")]
    [SerializeField] private bool isExitDoor = false;     // Si es salida de nivel
    [SerializeField] private string nextSceneName = "";   // Nombre exacto en Build Settings

    [Header("Rotación opcional (sin Animator)")]
    [SerializeField] private Transform doorVisual;        // Déjalo vacío si no quieres rotar
    [SerializeField] private bool rotateOnOpen = false;   // OFF por defecto
    [SerializeField] private Vector3 openEuler = new Vector3(0, 90, 0);
    [SerializeField] private float rotateSpeed = 240f;

    public bool IsOpen { get; private set; }

    private Quaternion _closedRot;
    private Quaternion _openRot;
    private bool _rotating;

    private void Awake()
    {
        if (doorVisual == null) doorVisual = transform;
        _closedRot = doorVisual.rotation;
        _openRot = Quaternion.Euler(openEuler) * doorVisual.rotation;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        SetBlocked(true); // La puerta empieza cerrada
    }

    private void Update()
    {
        if (_rotating && rotateOnOpen && doorVisual)
        {
            doorVisual.rotation = Quaternion.RotateTowards(doorVisual.rotation, _openRot, rotateSpeed * Time.deltaTime);
            if (Quaternion.Angle(doorVisual.rotation, _openRot) < 0.5f)
                _rotating = false;
        }
    }

    public bool TryOpen(PlayerKeyring keyring)
    {
        if (IsOpen) return true;

        if (requiresKey)
        {
            if (keyring == null) return false;
            if (consumesKey)
            {
                if (!keyring.TryUseKey(1)) return false;
            }
            else
            {
                if (keyring.KeyCount <= 0) return false;
            }
        }

        OpenNow();
        return true;
    }

    public void OpenNow()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (audioSource && openSfx) audioSource.PlayOneShot(openSfx);

        // Desbloquear paso y NavMesh
        SetBlocked(false);

        if (rotateOnOpen) _rotating = true;

        if (isExitDoor)
        {
            if (openSfx) Invoke(nameof(LoadNextScene), 0.25f);
            else LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.name == nextSceneName)
                Debug.LogWarning($"[DoorController] nextSceneName apunta a la escena actual ({nextSceneName}).");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning($"[DoorController] isExitDoor activo pero 'nextSceneName' vacío en {name}.");
        }
    }

    private void SetBlocked(bool blocked)
    {
        if (blockCollider) blockCollider.enabled = blocked;
        if (navObstacle) navObstacle.carving = blocked; // Carve ON cuando está cerrada
    }
}
