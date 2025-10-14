using UnityEngine;
using UnityEngine.InputSystem;

public class AutoLoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private float showSeconds = 5f;

    [Header("Opcional")]
    [SerializeField] private Behaviour[] disableWhileShowing;
    [SerializeField] private bool pauseWhileShowing = true;
    [SerializeField] private bool lockCursorOnHide = true;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMap = "UI";
    [SerializeField] private string gameplayActionMap = "Player";
    [SerializeField] private float fadeOut = 0.5f;

    void Reset()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
        if (!panel) { Debug.LogError("AutoLoadingScreen: asigna un CanvasGroup en 'panel'."); enabled = false; return; }

        if (pauseWhileShowing) Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (disableWhileShowing != null)
            foreach (var b in disableWhileShowing) if (b) b.enabled = false;

        if (playerInput && !string.IsNullOrEmpty(uiActionMap))
            playerInput.SwitchCurrentActionMap(uiActionMap);

        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        StartCoroutine(HideRoutine());
    }

    System.Collections.IEnumerator HideRoutine()
    {
        float t = 0f;
        while (t < showSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);

        if (playerInput && !string.IsNullOrEmpty(gameplayActionMap))
            playerInput.SwitchCurrentActionMap(gameplayActionMap);

        if (disableWhileShowing != null)
            foreach (var b in disableWhileShowing) if (b) b.enabled = true;

        if (lockCursorOnHide) { Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked; }
        if (pauseWhileShowing) Time.timeScale = 1f;
    }
}
