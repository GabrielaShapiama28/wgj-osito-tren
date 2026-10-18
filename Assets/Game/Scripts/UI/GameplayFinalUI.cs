using TMPro;
using UnityEngine;

public sealed class GameplayFinalUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private UnityEngine.UI.Image resultImage;
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;

    private void Awake()
    {
        restartButton?.onClick.AddListener(Restart);
        mainMenuButton?.onClick.AddListener(MainMenu);
        Hide();
    }

    public void Configure(
        GameObject configuredRoot,
        UnityEngine.UI.Image configuredResultImage,
        Sprite configuredWinSprite,
        Sprite configuredLoseSprite,
        TMP_Text configuredConditionText,
        UnityEngine.UI.Button configuredRestartButton,
        UnityEngine.UI.Button configuredMainMenuButton)
    {
        root = configuredRoot;
        resultImage = configuredResultImage;
        winSprite = configuredWinSprite;
        loseSprite = configuredLoseSprite;
        conditionText = configuredConditionText;
        restartButton = configuredRestartButton;
        mainMenuButton = configuredMainMenuButton;
    }

    public void Show(bool won, string details)
    {
        if (resultImage != null)
            resultImage.sprite = won ? winSprite : loseSprite;
        if (conditionText != null)
            conditionText.text = details;
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private static void Restart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetSession();
        SceneNavigator.Load(SceneNavigator.Gameplay);
    }

    private static void MainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetSession();
        SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private void OnDestroy()
    {
        restartButton?.onClick.RemoveListener(Restart);
        mainMenuButton?.onClick.RemoveListener(MainMenu);
    }
}
