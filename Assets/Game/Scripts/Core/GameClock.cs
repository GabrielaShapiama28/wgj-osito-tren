
using System;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    [SerializeField] private float secondsPerGameMinute = 1f;

    private float currentMinutes;
    private bool isRunning;

    public event Action OnDayFinished;

    public string CurrentTime
    {
        get
        {
            int total = Mathf.FloorToInt(currentMinutes);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }

    public void StartClock()
    {
        currentMinutes = 8 * 60;
        isRunning = true;
    }

    public void PauseClock() => isRunning = false;

    public void ResumeClock() => isRunning = true;

    public void AdvanceTime(float minutes)
    {
        currentMinutes = Mathf.Min(
            currentMinutes + Mathf.Max(0f, minutes),
            20 * 60
        );

        CheckEndOfDay();
    }

    private void Update()
    {
        if (!isRunning) return;

        currentMinutes += Time.deltaTime /
                          Mathf.Max(0.01f, secondsPerGameMinute);

        CheckEndOfDay();
    }

    private void CheckEndOfDay()
    {
        if (currentMinutes < 20 * 60) return;

        currentMinutes = 20 * 60;
        isRunning = false;
        OnDayFinished?.Invoke();
    }
}
