using TMPro;
using UnityEngine;

public sealed class BirdPhotoMinigameController : MonoBehaviour
{
    [Min(1f)] [SerializeField] private float durationSeconds = 10f;
    [SerializeField] private UnityEngine.UI.Image birdImage;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject decisionButtons;
    [SerializeField] private UnityEngine.UI.Button continueButton;

    private float remaining;
    private bool finished;
    private bool succeeded;
    private bool continued;

    private void Awake()
    {
        remaining = durationSeconds;
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (resultText != null)
            resultText.text = "Encuadra al pájaro y prueba el resultado.";
        RefreshTimer();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
            if (birdImage != null)
                birdImage.sprite = GameManager.Instance.ActiveBirdSprite;
        }
        continueButton?.onClick.AddListener(ContinueToGameplay);
    }

    private void Update()
    {
        if (finished)
            return;

        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        RefreshTimer();
        if (remaining <= 0f)
            Finish(false);
    }

    public void TestSuccess() => Finish(true);
    public void TestFailure() => Finish(false);

    public void Configure(
        UnityEngine.UI.Image configuredBirdImage,
        TMP_Text configuredTimerText,
        TMP_Text configuredResultText,
        GameObject configuredDecisionButtons,
        UnityEngine.UI.Button configuredContinueButton)
    {
        birdImage = configuredBirdImage;
        timerText = configuredTimerText;
        resultText = configuredResultText;
        decisionButtons = configuredDecisionButtons;
        continueButton = configuredContinueButton;
    }

    private void Finish(bool wasSuccessful)
    {
        if (finished)
            return;

        finished = true;
        succeeded = wasSuccessful;
        if (resultText != null)
            resultText.text = succeeded ? "¡Fotografía conseguida!" : "La fotografía no salió.";
        if (decisionButtons != null)
            decisionButtons.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private void ContinueToGameplay()
    {
        if (!finished || continued)
            return;

        continued = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyMinigameResultMinutes(succeeded, 10f);
        SceneNavigator.Load(SceneNavigator.Gameplay);
    }

    private void RefreshTimer()
    {
        if (timerText != null)
            timerText.text = $"Tiempo para la foto: {Mathf.CeilToInt(remaining):00}";
    }

    private void OnDestroy()
    {
        continueButton?.onClick.RemoveListener(ContinueToGameplay);
    }
}
