
using System;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    [SerializeField] private float secondsPerGameMinute = 1f;
    [SerializeField] private int startHour = 8;
    [SerializeField] private int endHour = 20;

    private float currentMinutes;
    private bool isRunning;
    private bool hasStarted;
    private bool endTimeReached;
    private bool dayFinished;

    public event Action OnEndTimeReached;
    public event Action OnDayFinished;

    public float CurrentTotalMinutes => currentMinutes;
    public int CurrentHour => Mathf.FloorToInt(currentMinutes / 60f);
    public bool IsRunning => isRunning;
    public bool HasStarted => hasStarted;
    public bool HasReachedEndTime => endTimeReached;

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
        currentMinutes = startHour * 60;
        isRunning = true;
        hasStarted = true;
        endTimeReached = false;
        dayFinished = false;
    }

    public void ResetClock()
    {
        currentMinutes = startHour * 60;
        isRunning = false;
        hasStarted = false;
        endTimeReached = false;
        dayFinished = false;
    }

    public void PauseClock() => isRunning = false;

    public void ResumeClock()
    {
        if (!endTimeReached && !dayFinished)
            isRunning = true;
    }

    public void AdvanceTime(float minutes)
    {
        currentMinutes = Mathf.Min(
            currentMinutes + Mathf.Max(0f, minutes),
            endHour * 60
        );

        CheckEndTime();
    }

    public void CompleteDay()
    {
        if (dayFinished) return;

        dayFinished = true;
        isRunning = false;
        OnDayFinished?.Invoke();
    }

    private void Update()
    {
        if (!isRunning) return;

        currentMinutes += Time.deltaTime /
                          Mathf.Max(0.01f, secondsPerGameMinute);

        CheckEndTime();
    }

    private void CheckEndTime()
    {
        if (currentMinutes < endHour * 60 || endTimeReached) return;

        currentMinutes = endHour * 60;
        isRunning = false;
        endTimeReached = true;
        OnEndTimeReached?.Invoke();
    }
}
