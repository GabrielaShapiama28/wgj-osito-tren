using TMPro;
using UnityEngine;

public sealed class GameplayEventController : MonoBehaviour
{
    [Header("Intervalos de Gameplay activo")]
    [Min(1f)] [SerializeField] private float motherFavorIntervalSeconds = 60f;
    [Min(1f)] [SerializeField] private float normalBirdIntervalSeconds = 20f;
    [Min(0.5f)] [SerializeField] private float birdVisibleSeconds = 5f;

    [Header("Pop-up de Mamá")]
    [SerializeField] private EventPopupUI popup;

    [Header("Pájaros en la ventana")]
    [SerializeField] private GameObject birdRoot;
    [SerializeField] private UnityEngine.UI.Image birdImage;
    [SerializeField] private TMP_Text birdTimeText;
    [SerializeField] private Sprite[] normalBirdSprites;
    [SerializeField] private Sprite kingBirdSprite;

    [Header("Final")]
    [SerializeField] private GameplayFinalUI finalUI;

    private GameManager manager;
    private string currentEventId;
    private GameplayEventType currentEventType;
    private float birdTimeRemaining;
    private bool birdIsVisible;
    private bool finalDisplayed;

    public float MotherFavorIntervalSeconds => motherFavorIntervalSeconds;
    public float NormalBirdIntervalSeconds => normalBirdIntervalSeconds;
    public float BirdVisibleSeconds => birdVisibleSeconds;
    public bool HasActiveEvent => (popup != null && popup.IsOpen) || birdIsVisible || finalDisplayed;

    private void Start()
    {
        manager = GameManager.Instance;
        popup?.Initialize(this);
        HideBird();
        finalUI?.Hide();

        if (manager == null)
        {
            Debug.LogError("Gameplay necesita un GameManager con GameClock.");
            return;
        }

        if (!manager.Clock.HasStarted)
            manager.StartGame();
        else if (!manager.Clock.HasReachedEndTime)
            manager.ResumeGame();
    }

    private void Update()
    {
        if (manager == null || finalDisplayed)
            return;

        if (popup != null && popup.IsOpen)
            return;

        bool kingDue = manager.Clock.CurrentHour >= 17
            && !manager.IsEventResolved("king-bird-17")
            && !manager.KingBirdPhotographed;

        if (kingDue && (!birdIsVisible || currentEventType != GameplayEventType.KingBird))
        {
            if (birdIsVisible)
                ResolveIgnoredBird();
            ShowBird(true);
            return;
        }

        bool dayEnded = manager.Clock.HasReachedEndTime || manager.Clock.CurrentHour >= 20;
        bool resolvingKingAtDayEnd = birdIsVisible && currentEventType == GameplayEventType.KingBird;
        if (dayEnded && !resolvingKingAtDayEnd)
        {
            ShowFinal();
            return;
        }

        if (!dayEnded)
            manager.AdvanceGameplayIntervals(Time.unscaledDeltaTime);

        if (birdIsVisible)
        {
            birdTimeRemaining = Mathf.Max(0f, birdTimeRemaining - Time.unscaledDeltaTime);
            RefreshBirdTimer();
            if (birdTimeRemaining <= 0f)
                ResolveIgnoredBird();
            return;
        }

        if (manager.IsMotherFavorDue(motherFavorIntervalSeconds))
        {
            OpenMotherFavor();
            return;
        }

        if (manager.IsNormalBirdDue(normalBirdIntervalSeconds))
            ShowBird(false);
    }

    public void AcceptCurrentEvent()
    {
        if (manager == null
            || string.IsNullOrEmpty(currentEventId)
            || currentEventType != GameplayEventType.MotherFavor)
            return;

        popup?.Hide();
        manager.BeginMinigame(currentEventId, GameplayEventType.MotherFavor);
        currentEventId = null;
        SceneNavigator.Load(SceneNavigator.FruitMinigame);
    }

    public void RejectCurrentEvent()
    {
        if (manager == null
            || string.IsNullOrEmpty(currentEventId)
            || currentEventType != GameplayEventType.MotherFavor)
            return;

        manager.RejectGameplayEvent(currentEventId, currentEventType);
        popup?.Hide();
        currentEventId = null;
        manager.ResumeGame();
    }

    public void PhotographVisibleBird()
    {
        if (manager == null || !birdIsVisible || string.IsNullOrEmpty(currentEventId))
            return;

        Sprite photographedSprite = birdImage != null ? birdImage.sprite : null;
        string eventId = currentEventId;
        GameplayEventType eventType = currentEventType;
        HideBird();
        manager.BeginBirdMinigame(eventId, eventType, photographedSprite);
        currentEventId = null;
        SceneNavigator.Load(SceneNavigator.BirdPhotoMinigame);
    }

    public void Configure(
        EventPopupUI configuredPopup,
        GameObject configuredBirdRoot,
        UnityEngine.UI.Image configuredBirdImage,
        TMP_Text configuredBirdTimeText,
        Sprite[] configuredNormalBirdSprites,
        Sprite configuredKingBirdSprite,
        GameplayFinalUI configuredFinalUI)
    {
        popup = configuredPopup;
        birdRoot = configuredBirdRoot;
        birdImage = configuredBirdImage;
        birdTimeText = configuredBirdTimeText;
        normalBirdSprites = configuredNormalBirdSprites;
        kingBirdSprite = configuredKingBirdSprite;
        finalUI = configuredFinalUI;
    }

    public void ConfigureIntervals(
        float configuredMotherFavorIntervalSeconds,
        float configuredNormalBirdIntervalSeconds,
        float configuredBirdVisibleSeconds)
    {
        motherFavorIntervalSeconds = Mathf.Max(1f, configuredMotherFavorIntervalSeconds);
        normalBirdIntervalSeconds = Mathf.Max(1f, configuredNormalBirdIntervalSeconds);
        birdVisibleSeconds = Mathf.Max(0.5f, configuredBirdVisibleSeconds);
    }

    private void OpenMotherFavor()
    {
        if (manager.MotherRelationship <= 0 || popup == null)
            return;

        manager.ConsumeMotherFavorInterval(motherFavorIntervalSeconds);
        currentEventId = manager.CreateMotherEventId();
        currentEventType = GameplayEventType.MotherFavor;
        manager.PauseGame();
        popup.Show(
            "Mamá necesita ayuda",
            "Mamá le pide a Osito recoger diez frutas. ¿Quieres aceptar el favor?"
        );
    }

    private void ShowBird(bool kingBird)
    {
        if (birdRoot == null || birdImage == null)
            return;

        Sprite sprite = kingBird ? kingBirdSprite : PickRandomNormalBird();
        if (sprite == null)
            return;

        if (!kingBird)
            manager.ConsumeNormalBirdInterval(normalBirdIntervalSeconds);

        currentEventType = kingBird ? GameplayEventType.KingBird : GameplayEventType.NormalBird;
        currentEventId = manager.CreateBirdEventId(kingBird);
        birdImage.sprite = sprite;
        birdTimeRemaining = birdVisibleSeconds;
        birdIsVisible = true;
        birdRoot.SetActive(true);
        RefreshBirdTimer();
    }

    private Sprite PickRandomNormalBird()
    {
        if (normalBirdSprites == null || normalBirdSprites.Length == 0)
            return null;
        return normalBirdSprites[Random.Range(0, normalBirdSprites.Length)];
    }

    private void ResolveIgnoredBird()
    {
        if (manager != null && !string.IsNullOrEmpty(currentEventId))
            manager.IgnoreBirdEvent(currentEventId, currentEventType);

        HideBird();
        currentEventId = null;
    }

    private void HideBird()
    {
        birdIsVisible = false;
        birdTimeRemaining = 0f;
        if (birdRoot != null)
            birdRoot.SetActive(false);
    }

    private void RefreshBirdTimer()
    {
        if (birdTimeText != null)
            birdTimeText.text = $"{Mathf.CeilToInt(birdTimeRemaining)} s";
    }

    private void ShowFinal()
    {
        if (finalDisplayed)
            return;

        finalDisplayed = true;
        if (birdIsVisible)
            ResolveIgnoredBird();
        popup?.Hide();
        manager.PauseGame();
        manager.CompleteDay();
        finalUI?.Show(manager.DidWin, manager.EvaluateEnding());
    }
}
