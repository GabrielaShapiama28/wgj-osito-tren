using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayClockHandsUI : MonoBehaviour
{
    [SerializeField] private RectTransform hoursHand;
    [SerializeField] private RectTransform minutesHand;
    [SerializeField] private GameClock gameClock;
    [SerializeField] private float initialTotalMinutes = 8f * 60f;

    private void OnEnable()
    {
        TryBindClock();
        RefreshHands();
    }

    private void LateUpdate()
    {
        TryBindClock();
        RefreshHands();
    }

    public void Configure(
        RectTransform configuredHoursHand,
        RectTransform configuredMinutesHand,
        GameClock configuredClock)
    {
        hoursHand = configuredHoursHand;
        minutesHand = configuredMinutesHand;
        gameClock = configuredClock;
        RefreshHands();
    }

    public void RefreshHands()
    {
        float totalMinutes = gameClock != null && gameClock.HasStarted
            ? gameClock.CurrentTotalMinutes
            : initialTotalMinutes;

        float minuteWithinHour = Mathf.Repeat(totalMinutes, 60f);
        float hourWithinCycle = Mathf.Repeat(totalMinutes / 60f, 12f);

        if (hoursHand != null)
            hoursHand.localEulerAngles = new Vector3(0f, 0f, 90f - hourWithinCycle * 30f);

        if (minutesHand != null)
            minutesHand.localEulerAngles = new Vector3(0f, 0f, -minuteWithinHour * 6f);
    }

    private void TryBindClock()
    {
        if (gameClock != null)
            return;

        if (GameManager.Instance != null)
            gameClock = GameManager.Instance.Clock;

        if (gameClock == null)
            gameClock = FindFirstObjectByType<GameClock>();
    }
}
