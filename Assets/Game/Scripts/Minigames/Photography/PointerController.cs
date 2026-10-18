
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PointerController : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public RectTransform safeZone;

    public GameObject winPopup;
    public GameObject losePopup;

    public float moveSpeed = 100f;
    public float minSafeZoneX = -350f;
    public float maxSafeZoneX = 355f;

    [Header("Integración Bearly in Time")]
    [Min(1f)] [SerializeField] private float timeLimitSeconds = 10f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private UnityEngine.UI.Image birdImage;

    private RectTransform pointerTransform;
    private Vector3 targetPosition;
    private bool finished = false;
    private bool succeeded;
    private bool continued;
    private float remainingSeconds;
    private Sprite selectedBirdSprite;

    public bool IsFinished => finished;
    public bool Succeeded => succeeded;
    public float RemainingSeconds => remainingSeconds;
    public Sprite SelectedBirdSprite => selectedBirdSprite;

    public void ConfigureIntegration(
        float configuredTimeLimitSeconds,
        TMP_Text configuredTimerText,
        UnityEngine.UI.Image configuredBirdImage)
    {
        timeLimitSeconds = Mathf.Max(1f, configuredTimeLimitSeconds);
        timerText = configuredTimerText;
        birdImage = configuredBirdImage;
    }

    void RandomizeSafeZone()
    {
        Vector2 position = safeZone.anchoredPosition;

        position.x = Random.Range(minSafeZoneX, maxSafeZoneX);

        safeZone.anchoredPosition = position;
    }

    void Awake()
    {
        if (timerText == null)
            timerText = GameObject.Find("PhotoTimerText")?.GetComponent<TMP_Text>();

        if (birdImage == null)
            birdImage = GameObject.Find("BirdVisual")?.GetComponent<UnityEngine.UI.Image>();

        selectedBirdSprite = GameManager.Instance != null
            ? GameManager.Instance.ActiveBirdSprite
            : null;
        ApplySelectedBirdSprite();
    }

    void Start()
    {
        pointerTransform = GetComponent<RectTransform>();
        targetPosition = pointB.position;
        remainingSeconds = timeLimitSeconds;

        RandomizeSafeZone();

        if (winPopup != null)
            winPopup.SetActive(false);

        if (losePopup != null)
            losePopup.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
            if (selectedBirdSprite == null)
                selectedBirdSprite = GameManager.Instance.ActiveBirdSprite;
        }

        ApplySelectedBirdSprite();
        RefreshTimer();
    }

    void LateUpdate()
    {
        // La foto debe conservar exactamente el asset elegido en Gameplay durante todo el QTE.
        ApplySelectedBirdSprite();
    }

    void Update()
    {
        if (finished)
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ContinueToGameplay();
            }
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
        RefreshTimer();
        if (remainingSeconds <= 0f)
        {
            Finish(false);
            return;
        }

        pointerTransform.position = Vector3.MoveTowards(
            pointerTransform.position,
            targetPosition,
            moveSpeed * Time.unscaledDeltaTime
        );

        if (Vector3.Distance(pointerTransform.position, pointA.position) < 0.1f)
        {
            targetPosition = pointB.position;
        }
        else if (Vector3.Distance(pointerTransform.position, pointB.position) < 0.1f)
        {
            targetPosition = pointA.position;
        }

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    void CheckSuccess()
    {
        bool isInsideSafeZone =
            RectTransformUtility.RectangleContainsScreenPoint(
                safeZone,
                pointerTransform.position,
                null
            );

        Finish(isInsideSafeZone);
    }

    public void TestFinish(bool wasSuccessful)
    {
        Finish(wasSuccessful);
    }

    public void ContinueToGameplay()
    {
        if (!finished || continued)
            return;

        continued = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyMinigameResultMinutes(succeeded, 10f);

        SceneNavigator.Load(SceneNavigator.Gameplay);
    }

    private void Finish(bool wasSuccessful)
    {
        if (finished)
            return;

        finished = true;
        succeeded = wasSuccessful;
        remainingSeconds = Mathf.Max(0f, remainingSeconds);
        RefreshTimer();
        ShowPopup(succeeded ? winPopup : losePopup);
    }

    void ShowPopup(GameObject popup)
    {
        if (popup == null)
            return;

        WinPopupAnimation animation =
            popup.GetComponent<WinPopupAnimation>();

        if (animation != null)
        {
            animation.Show();
        }
        else
        {
            popup.SetActive(true);
        }
    }

    private void RefreshTimer()
    {
        if (timerText == null)
            return;

        timerText.text = finished
            ? "ESPACIO para continuar"
            : $"Tiempo: {Mathf.CeilToInt(remainingSeconds):00}";
    }

    private void ApplySelectedBirdSprite()
    {
        if (birdImage == null || selectedBirdSprite == null)
            return;

        if (birdImage.sprite != selectedBirdSprite)
            birdImage.sprite = selectedBirdSprite;
        birdImage.preserveAspect = true;
    }
}
