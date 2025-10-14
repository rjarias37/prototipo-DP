using UnityEngine;
using UnityEngine.InputSystem;

public class InputSanityCheck : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionReference move;   // Vector2
    [SerializeField] private InputActionReference jump;   // Button
    [SerializeField] private InputActionReference attack; // Button (opcional)

    void Reset() => playerInput = FindAnyObjectByType<PlayerInput>();

    void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("InputSanityCheck: No se encontró PlayerInput en la escena.");
                return;
            }
        }

        EnableIfValid(move);
        EnableIfValid(jump);
        EnableIfValid(attack);
    }

    void OnDisable()
    {
        DisableIfValid(move);
        DisableIfValid(jump);
        DisableIfValid(attack);
    }

    static void EnableIfValid(InputActionReference actionRef)
    {
        if (actionRef == null) return;
        var action = actionRef.action;
        if (action == null) return;
        if (!action.enabled) action.Enable();
    }

    static void DisableIfValid(InputActionReference actionRef)
    {
        if (actionRef == null) return;
        var action = actionRef.action;
        if (action == null) return;
        if (action.enabled) action.Disable();
    }
}
