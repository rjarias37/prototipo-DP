using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private float fadeIn = 0.35f;
    [SerializeField] private float fadeOut = 0.35f;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string uiActionMap = "UI";
    [SerializeField] private string gameplayActionMap = "Player";

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Scene 1";

    private bool shown;

    void Reset()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
        if (!retryButton) retryButton = GetComponentInChildren<Button>(true);
    }

    void Awake()
    {
        if (!panel) panel = GetComponent<CanvasGroup>();
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    void OnEnable()
    {
        if (retryButton) retryButton.onClick.AddListener(Retry);
        if (exitButton) exitButton.onClick.AddListener(ExitToMenu);
    }

    void OnDisable()
    {
        if (retryButton) retryButton.onClick.RemoveListener(Retry);
        if (exitButton) exitButton.onClick.RemoveListener(ExitToMenu);
    }

    public void Show()
    {
        if (shown) return;
        shown = true;
        StartCoroutine(ShowRoutine());
    }

    System.Collections.IEnumerator ShowRoutine()
    {
        Time.timeScale = 0f;
        if (playerInput && !string.IsNullOrEmpty(uiActionMap))
            playerInput.SwitchCurrentActionMap(uiActionMap);

        panel.gameObject.SetActive(true);
        panel.interactable = true;
        panel.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        panel.alpha = 1f;
    }

    void HideImmediate()
    {
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
        panel.gameObject.SetActive(false);
        shown = false;
    }

    public void Retry() => StartCoroutine(ReloadCurrentScene());
    public void ExitToMenu() => StartCoroutine(LoadScene(mainMenuSceneName));

    System.Collections.IEnumerator ReloadCurrentScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        yield return LoadScene(scene);
    }

    System.Collections.IEnumerator LoadScene(string sceneName)
    {
        // fade out en tiempo no escalado
        float t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        HideImmediate();

        // restaurar juego antes de cambiar de escena
        if (playerInput && !string.IsNullOrEmpty(gameplayActionMap))
            playerInput.SwitchCurrentActionMap(gameplayActionMap);
        Time.timeScale = 1f;

        // carga fiable
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
    }
}
