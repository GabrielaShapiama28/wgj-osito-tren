
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameClock))]
public class GameManager : MonoBehaviour
{
    public const int MaximumStatusValue = 9;
    public const int InitialStatusValue = 6;

    public static GameManager Instance { get; private set; }

    private GameClock gameClock;
    private readonly HashSet<string> resolvedEvents = new HashSet<string>();
    private string activeEventId;
    private GameplayEventType activeEventType;
    private bool activeMinigameResultApplied;
    private float motherFavorElapsed;
    private float normalBirdElapsed;
    private int motherEventSequence;
    private int birdEventSequence;
    private Sprite activeBirdSprite;

    public GameClock Clock => gameClock;
    public bool KingBirdPhotographed { get; private set; }
    public int AcceptedEvents { get; private set; }
    public int RejectedEvents { get; private set; }
    public bool HasLastMinigameResult { get; private set; }
    public bool LastMinigameSucceeded { get; private set; }
    public string LastMinigameEventId { get; private set; }
    public GameplayEventType LastMinigameEventType { get; private set; }
    public int MotherRelationship { get; private set; } = InitialStatusValue;
    public int BearEnergy { get; private set; } = InitialStatusValue;
    public int SelfRelationship { get; private set; } = InitialStatusValue;
    public float MotherFavorElapsed => motherFavorElapsed;
    public float NormalBirdElapsed => normalBirdElapsed;
    public Sprite ActiveBirdSprite => activeBirdSprite;
    public bool DidWin => KingBirdPhotographed && BearEnergy > 3 && MotherRelationship > 3;

    public event Action<string, GameplayEventType, bool> OnMinigameResultApplied;
    public event Action OnStatusChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameClock = GetComponent<GameClock>();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        gameClock.OnDayFinished += FinishGame;
        if (!gameClock.HasStarted)
            StartGame();
    }

    public void StartGame()
    {
        Debug.Log("¡Comienza la aventura de Osito!");
        gameClock.StartClock();
    }

    public void PauseGame()
    {
        gameClock.PauseClock();
    }

    public void ResumeGame()
    {
        gameClock.ResumeClock();
    }

    public void FinishGame()
    {
        Debug.Log($"¡Terminó el día! {EvaluateEnding()}");
    }

    public void CompleteDay()
    {
        gameClock.CompleteDay();
    }

    public bool IsEventResolved(string eventId) => resolvedEvents.Contains(eventId);

    public void ResolveEvent(string eventId, bool accepted)
    {
        if (string.IsNullOrWhiteSpace(eventId) || !resolvedEvents.Add(eventId))
            return;

        if (accepted)
            AcceptedEvents++;
        else
            RejectedEvents++;
    }

    public string CreateMotherEventId() => $"mother-favor-{++motherEventSequence}";

    public string CreateBirdEventId(bool kingBird)
    {
        return kingBird ? "king-bird-17" : $"normal-bird-{++birdEventSequence}";
    }

    public void AdvanceGameplayIntervals(float realSeconds)
    {
        float safeDelta = Mathf.Max(0f, realSeconds);
        if (MotherRelationship > 0)
            motherFavorElapsed += safeDelta;
        normalBirdElapsed += safeDelta;
    }

    public bool IsMotherFavorDue(float intervalSeconds)
    {
        return MotherRelationship > 0
            && motherFavorElapsed >= Mathf.Max(0.1f, intervalSeconds);
    }

    public bool IsNormalBirdDue(float intervalSeconds)
    {
        return normalBirdElapsed >= Mathf.Max(0.1f, intervalSeconds);
    }

    public void ConsumeMotherFavorInterval(float intervalSeconds)
    {
        motherFavorElapsed = Mathf.Max(0f, motherFavorElapsed - Mathf.Max(0.1f, intervalSeconds));
    }

    public void ConsumeNormalBirdInterval(float intervalSeconds)
    {
        normalBirdElapsed = Mathf.Max(0f, normalBirdElapsed - Mathf.Max(0.1f, intervalSeconds));
    }

    public void RejectGameplayEvent(string eventId, GameplayEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(eventId) || IsEventResolved(eventId))
            return;

        ResolveEvent(eventId, false);
        if (eventType == GameplayEventType.MotherFavor)
        {
            MotherRelationship = ClampStatus(MotherRelationship - 2);
            OnStatusChanged?.Invoke();
        }
    }

    public void IgnoreBirdEvent(string eventId, GameplayEventType eventType)
    {
        ResolveEvent(eventId, false);
    }

    public void BeginMinigame(string eventId, GameplayEventType eventType)
    {
        activeEventId = eventId;
        activeEventType = eventType;
        activeMinigameResultApplied = false;
        ResolveEvent(eventId, true);
        PauseGame();
    }

    public void BeginBirdMinigame(string eventId, GameplayEventType eventType, Sprite birdSprite)
    {
        BeginMinigame(eventId, eventType);
        activeBirdSprite = birdSprite;
    }

    public void ApplyMinigameResult(bool succeeded, int hoursToAdvance = 0)
    {
        ApplyMinigameResultMinutes(succeeded, hoursToAdvance * 60f);
    }

    public void ApplyMinigameResultMinutes(bool succeeded, float minutesToAdvance)
    {
        if (activeMinigameResultApplied)
            return;

        activeMinigameResultApplied = true;
        string completedEventId = activeEventId;
        GameplayEventType completedEventType = activeEventType;

        if (succeeded)
        {
            switch (activeEventType)
            {
                case GameplayEventType.MotherFavor:
                    MotherRelationship = ClampStatus(MotherRelationship + 2);
                    BearEnergy = ClampStatus(BearEnergy - 2);
                    break;
                case GameplayEventType.NormalBird:
                    BearEnergy = ClampStatus(BearEnergy + 2);
                    break;
                case GameplayEventType.KingBird:
                    KingBirdPhotographed = true;
                    break;
            }
        }

        // Punto de extensión del MVP: más adelante se aplicarán aquí energía,
        // afecto y otros efectos derivados del resultado de la misión.
        if (minutesToAdvance > 0f)
            gameClock.AdvanceTime(minutesToAdvance);

        HasLastMinigameResult = true;
        LastMinigameSucceeded = succeeded;
        LastMinigameEventId = completedEventId;
        LastMinigameEventType = completedEventType;

        Debug.Log($"Resultado de {completedEventId}: {(succeeded ? "éxito" : "sin completar")}");
        OnMinigameResultApplied?.Invoke(completedEventId, completedEventType, succeeded);
        OnStatusChanged?.Invoke();
        activeEventId = null;
        activeBirdSprite = null;
    }

    public string EvaluateEnding()
    {
        return DidWin
            ? "GANASTE: fotografiaste al Pájaro Rey y equilibraste tus relaciones."
            : "PERDISTE: necesitabas fotografiar al Pájaro Rey, conservar más de 5 de energía y mantener a Mamá con al menos 5 puntos.";
    }

    public void ResetSession()
    {
        resolvedEvents.Clear();
        activeEventId = null;
        activeMinigameResultApplied = false;
        AcceptedEvents = 0;
        RejectedEvents = 0;
        KingBirdPhotographed = false;
        HasLastMinigameResult = false;
        LastMinigameSucceeded = false;
        LastMinigameEventId = null;
        LastMinigameEventType = default;
        MotherRelationship = InitialStatusValue;
        BearEnergy = InitialStatusValue;
        SelfRelationship = InitialStatusValue;
        motherFavorElapsed = 0f;
        normalBirdElapsed = 0f;
        motherEventSequence = 0;
        birdEventSequence = 0;
        activeBirdSprite = null;
        gameClock.ResetClock();
        OnStatusChanged?.Invoke();
    }

    private static int ClampStatus(int value)
    {
        return Mathf.Clamp(value, 0, MaximumStatusValue);
    }

    private void OnDestroy()
    {
        if (gameClock != null)
            gameClock.OnDayFinished -= FinishGame;

        if (Instance == this)
            Instance = null;
    }
}
