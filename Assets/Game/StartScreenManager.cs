// Assets/Game/StartScreenManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StartScreenManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button startButton;
    [SerializeField] private Behaviour[] disableWhileShowing;
    [SerializeField] private bool pauseWhileShowing = true;
    [SerializeField] private bool lockCursorOnHide = true;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMap = "UI";
    [SerializeField] private string gameplayActionMap = "Player";
    [SerializeField] private float fadeOut = 0.5f;

    // NUEVO
    [Header("Nuevo: Delay + SFX")]
    [SerializeField] private float delayBeforeStart = 3f;     // segundos en UI antes de empezar
    [SerializeField] private AudioSource audioSource;          // opcional (en el mismo StartPanel)
    [SerializeField] private AudioClip clickSfx;               // sonido del botón

    void Reset()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
        if (!startButton) startButton = GetComponentInChildren<Button>(true);
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
        if (!panel) { Debug.LogError("Asigna un CanvasGroup al campo 'panel' del StartScreenManager."); enabled = false; return; }

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
    }

    void OnEnable()
    {
        if (startButton) startButton.onClick.AddListener(Begin);
    }

    void OnDisable()
    {
        if (startButton) startButton.onClick.RemoveListener(Begin);
    }

    // Antes: Begin() arrancaba HideRoutine directamente. Ahora dispara una rutina con SFX + delay.
    public void Begin() => StartCoroutine(BeginRoutine());

    System.Collections.IEnumerator BeginRoutine()
    {
        if (audioSource && clickSfx) audioSource.PlayOneShot(clickSfx);
        // Usamos tiempo no escalado porque la UI está en pausa
        float t = 0f;
        while (t < delayBeforeStart)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return HideRoutine();
    }

    System.Collections.IEnumerator HideRoutine()
    {
        float t = 0f;
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

        if (lockCursorOnHide)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (pauseWhileShowing) Time.timeScale = 1f;
    }
}
