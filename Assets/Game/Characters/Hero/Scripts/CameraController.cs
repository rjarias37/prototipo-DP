using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // Cuerpo del personaje
    public Transform cameraTransform; // Cámara principal
    public Vector3 thirdPersonOffset = new Vector3(0f, 2f, -4f);
    public Vector3 firstPersonOffset = new Vector3(0f, 1.6f, 0.5f); // Z positivo para adelantar la cámara
    
    [Header("Collision Settings")]
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;
    
    [Header("Camera Behavior")]
    public float rotationSpeed = 3f;
    public bool isFirstPerson = false;
    public float firstPersonForwardOffset = 0.5f; // Nueva mejora: offset frontal adicional

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Inicializar con rotación actual
        yaw = target.eulerAngles.y;
        pitch = 15f; // Ángulo ligeramente hacia abajo por defecto
    }

    void Update()
    {
        // Cambiar entre primera y tercera persona
        if (Input.GetKeyDown(KeyCode.T))
            isFirstPerson = !isFirstPerson;

        // Manejar entrada del ratón
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 70f); // Límites conservadores
    }

    void LateUpdate()
    {
        if (target == null || cameraTransform == null) return;

        if (isFirstPerson)
        {
            HandleFirstPerson();
        }
        else
        {
            HandleThirdPerson();
        }
    }

    void HandleFirstPerson()
    {
        // MEJORA PRINCIPAL: Posición con offset hacia adelante
        Vector3 forwardOffset = target.forward * firstPersonForwardOffset;
        Vector3 desiredPosition = target.position + firstPersonOffset + forwardOffset;
        
        // Aplicar posición directamente (sin suavizado para evitar temblores)
        cameraTransform.position = desiredPosition;
        
        // Rotación basada en el ratón
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        
        // Rotar el personaje para que coincida con la dirección de la cámara
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void HandleThirdPerson()
    {
        // Calcular posición ideal
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 idealPosition = target.position + rotation * thirdPersonOffset;
        
        // Verificar colisión con paredes
        Vector3 adjustedPosition = AdjustForWalls(idealPosition);
        
        // Aplicar posición
        cameraTransform.position = adjustedPosition;
        
        // Hacer que la cámara mire al jugador
        cameraTransform.LookAt(target.position + Vector3.up * 1.5f);
        
        // Rotar personaje para que mire en dirección a la cámara
        Vector3 lookDirection = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
        if (lookDirection != Vector3.zero)
        {
            target.rotation = Quaternion.Slerp(
                target.rotation, 
                Quaternion.LookRotation(lookDirection), 
                Time.deltaTime * 10f
            );
        }
    }

    Vector3 AdjustForWalls(Vector3 idealPosition)
    {
        Vector3 direction = idealPosition - target.position;
        float distance = direction.magnitude;
        direction.Normalize();

        RaycastHit hit;
        if (Physics.Raycast(target.position, direction, out hit, distance, wallLayer))
        {
            return hit.point - direction * wallCheckDistance;
        }
        
        return idealPosition;
    }

    // Método para ajustar la posición desde el editor o durante pruebas
    public void SetFirstPersonPosition(float x, float y, float z, float forwardOffset)
    {
        firstPersonOffset = new Vector3(x, y, z);
        firstPersonForwardOffset = forwardOffset;
    }
}