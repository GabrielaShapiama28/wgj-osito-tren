using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayBackgroundScroller : MonoBehaviour
{
    [SerializeField] private RectTransform backgroundTrack;
    [SerializeField] private GameClock gameClock;
    [SerializeField] private float startTotalMinutes = 8f * 60f;
    [SerializeField] private float endTotalMinutes = 20f * 60f;
    [SerializeField] private float startX;
    [SerializeField] private float endX = -13440f;

    public float NormalizedProgress
    {
        get
        {
            if (gameClock == null)
                return 0f;

            return Mathf.InverseLerp(
                startTotalMinutes,
                endTotalMinutes,
                gameClock.CurrentTotalMinutes);
        }
    }

    private void OnEnable()
    {
        TryBindClock();
        RefreshPosition();
    }

    private void LateUpdate()
    {
        TryBindClock();
        RefreshPosition();
    }

    public void Configure(
        RectTransform track,
        GameClock clock,
        float firstX,
        float lastX)
    {
        backgroundTrack = track;
        gameClock = clock;
        startX = firstX;
        endX = lastX;
        RefreshPosition();
    }

    public void RefreshPosition()
    {
        if (backgroundTrack == null)
            return;

        Vector2 position = backgroundTrack.anchoredPosition;
        position.x = Mathf.Lerp(startX, endX, NormalizedProgress);
        backgroundTrack.anchoredPosition = position;
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
