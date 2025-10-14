using UnityEngine;
using UnityEngine.InputSystem;

public class HealthDebugHotKeys : MonoBehaviour
{
    [SerializeField] private int damageStep = 10;
    [SerializeField] private int healStep = 10;

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var k = Keyboard.current;
        if (k == null) return;

        if (k.hKey.wasPressedThisFrame) Debug.Log($"[HealthDebug] Heal +{healStep}");
        if (k.jKey.wasPressedThisFrame) Debug.Log($"[HealthDebug] Damage -{damageStep}");
        if (k.kKey.wasPressedThisFrame) Debug.Log("[HealthDebug] Kill = 0");
#endif
    }
}
