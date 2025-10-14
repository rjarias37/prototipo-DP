using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;           // Jugador 1
    [SerializeField] private Transform cameraTransform;  // Normalmente este mismo transform
    [SerializeField] private Transform pivot;            // (Opcional) Jugador1/CameraPivot

    [Header("3ª Persona")]
    [SerializeField] private float distance = 3.2f;
    [SerializeField] private Vector2 offset = new(0.35f, -0.25f); // X lado (hombro), Y altura extra
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 5.0f;

    [Header("1ª Persona")]
    [SerializeField] private Vector3 fpOffset = new(0f, 1.6f, 0f);
    [SerializeField] private float fpForward = 0.35f;

    [Header("Mira (Look)")]
    [SerializeField] private InputActionProperty look;       // Vector2 (Mouse Delta / Right Stick)
    [SerializeField] private InputActionProperty toggleView; // Button (opcional)
    [SerializeField] private float sensitivity = 85f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float lookSmoothing = 14f;  // 10–16 suave
    [SerializeField] private float lookDeadZone = 0.035f;

    [Header("Suavizado")]
    [SerializeField] private float followSmoothTime = 0.12f; // SmoothDamp del anchor/pivot
    [SerializeField] private float rotateLerp = 10f;         // suaviza pos/rot final

    [Header("Colisión")]
    [SerializeField] private LayerMask collisionMask = ~0;   // NO incluyas la capa del Player
    [SerializeField] private float sphereRadius = 0.32f;
    [SerializeField] private float wallPadding = 0.20f;
    [SerializeField] private float anchorHeight = 1.5f;      // usado si no hay pivot
    [SerializeField] private float distanceSmooth = 0.12f;   // suaviza distancia bloqueada

    [Header("Extras (opcionales)")]
    [SerializeField] private InputActionProperty zoomAxis;      // Mouse Scroll / triggers
    [SerializeField] private InputActionProperty shoulderSwap;  // Q / LB
    [SerializeField] private InputActionProperty recenter;      // R / R3
    [SerializeField] private bool rotateTargetYaw = false;      // déjalo en false
    [SerializeField] private bool isFirstPerson = false;
    [SerializeField] private bool lockCursor = true;

    // Estado
    float yaw, pitch, targetYaw, targetPitch;
    Vector3 smAnchor, anchorVel;
    float currentDistance, distanceVel;
    int shoulderSign = 1;

    void Reset()
    {
        if (!cameraTransform) cameraTransform = transform;
        if (!target && GameObject.FindWithTag("Player"))
            target = GameObject.FindWithTag("Player").transform;
        // Si existe un hijo llamado CameraPivot en el target, tómalo
        if (!pivot && target)
        {
            var p = target.Find("CameraPivot");
            if (p) pivot = p;
        }
    }

    void Awake()
    {
        if (!cameraTransform) cameraTransform = transform;
        Vector3 startRef = pivot ? pivot.position : (target ? target.position + Vector3.up * anchorHeight : transform.position);
        float startYaw = target ? target.eulerAngles.y : transform.eulerAngles.y;

        yaw = targetYaw = startYaw;
        pitch = targetPitch = Mathf.Clamp(15f, minPitch, maxPitch);
        smAnchor = startRef;
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    void OnEnable()
    {
        look.action?.Enable();
        toggleView.action?.Enable();
        zoomAxis.action?.Enable();
        shoulderSwap.action?.Enable();
        recenter.action?.Enable();

        if (toggleView.action != null) toggleView.action.performed += OnToggleView;
        if (shoulderSwap.action != null) shoulderSwap.action.performed += _ => shoulderSign *= -1;
        if (recenter.action != null) recenter.action.performed += _ => RecenterYaw();

        if (lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void OnDisable()
    {
        if (toggleView.action != null) toggleView.action.performed -= OnToggleView;
        if (lockCursor) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        look.action?.Disable();
        toggleView.action?.Disable();
        zoomAxis.action?.Disable();
        shoulderSwap.action?.Disable();
        recenter.action?.Disable();
    }

    void OnValidate()
    {
        if (maxDistance < minDistance) maxDistance = minDistance;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        if (sphereRadius < 0.05f) sphereRadius = 0.05f;
        if (wallPadding < 0.01f) wallPadding = 0.01f;
        if (anchorHeight < 0f) anchorHeight = 0f;
    }

    void OnToggleView(InputAction.CallbackContext _) => isFirstPerson = !isFirstPerson;

    void LateUpdate()
    {
        if (!cameraTransform) return;
        if (!target && !pivot) return;

        // 1) Entrada de look con dead-zone y filtro exponencial
        Vector2 delta = Vector2.zero;
        if (look.action != null) delta = look.action.ReadValue<Vector2>();
        else if (Mouse.current != null) delta = Mouse.current.delta.ReadValue(); // ¡OJO!: != null (evita CS0029)

        if (delta.sqrMagnitude < lookDeadZone * lookDeadZone) delta = Vector2.zero;

        float dt = Time.unscaledDeltaTime;
        targetYaw += delta.x * sensitivity * dt;
        targetPitch -= delta.y * sensitivity * dt;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float k = 1f - Mathf.Exp(-(lookSmoothing <= 0f ? 1e9f : lookSmoothing) * dt);
        yaw = Mathf.LerpAngle(yaw, targetYaw, k);
        pitch = Mathf.Lerp(pitch, targetPitch, k);

        // 2) Anchor/pivot suavizado
        Vector3 rawAnchor = pivot ? pivot.position : (target.position + Vector3.up * anchorHeight);
        smAnchor = Vector3.SmoothDamp(smAnchor, rawAnchor, ref anchorVel, followSmoothTime);

        // 3) Zoom opcional
        if (zoomAxis.action != null)
        {
            float z = zoomAxis.action.ReadValue<float>();
            if (Mathf.Abs(z) > 0.0001f)
            {
                float step = (Mathf.Abs(z) > 5f ? 0.6f : 0.2f);
                distance = Mathf.Clamp(distance - Mathf.Sign(z) * step, minDistance, maxDistance);
            }
        }

        if (isFirstPerson) FirstPerson(smAnchor);
        else ThirdPerson(smAnchor);

        // 4) (opcional) alinear el target al yaw de cámara
        if (rotateTargetYaw && target)
        {
            Vector3 flat = cameraTransform.forward; flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion tRot = Quaternion.LookRotation(flat);
                target.rotation = Quaternion.Slerp(target.rotation, tRot, 1f - Mathf.Exp(-rotateLerp * Time.deltaTime));
            }
        }
    }

    void ThirdPerson(Vector3 anchor)
    {
        Quaternion lookRot = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 shoulder = lookRot * new Vector3(offset.x * shoulderSign, offset.y, 0f);
        Vector3 orbitAnchor = anchor + shoulder;

        float goalDist = Mathf.Clamp(distance, minDistance, maxDistance);
        Vector3 desired = orbitAnchor - lookRot * Vector3.forward * goalDist;

        // SphereCast anti-clipping → distancia bloqueada
        float blocked = goalDist;
        Vector3 dir = desired - orbitAnchor;
        float dist = dir.magnitude;
        if (dist > 0.001f)
        {
            dir /= dist;
            if (Physics.SphereCast(orbitAnchor, sphereRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
                blocked = Mathf.Max(hit.distance - wallPadding, minDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, blocked, ref distanceVel, distanceSmooth);
        Vector3 finalPos = orbitAnchor - lookRot * Vector3.forward * currentDistance;

        float t = 1f - Mathf.Exp(-rotateLerp * Time.deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, finalPos, t);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, lookRot, t);
    }

    void FirstPerson(Vector3 anchor)
    {
        Vector3 head = (pivot ? pivot.position : target.position) + fpOffset + target.forward * fpForward;
        float t = 1f - Mathf.Exp(-rotateLerp * Time.deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, head, t);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, Quaternion.Euler(pitch, yaw, 0f), t);
    }

    void RecenterYaw()
    {
        float y = pivot ? pivot.eulerAngles.y : target.eulerAngles.y;
        targetYaw = yaw = y;
    }
}
