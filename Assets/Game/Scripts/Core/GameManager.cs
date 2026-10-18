
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameClock))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private GameClock gameClock;
    private readonly HashSet<string> resolvedEvents = new HashSet<string>();
    private string activeEventId;
    private GameplayEventType activeEventType;

    public GameClock Clock => gameClock;
    public bool KingBirdPhotographed { get; private set; }
    public int AcceptedEvents { get; private set; }
    public int RejectedEvents { get; private set; }

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

    public void BeginMinigame(string eventId, GameplayEventType eventType)
    {
        activeEventId = eventId;
        activeEventType = eventType;
        ResolveEvent(eventId, true);
        PauseGame();
    }

    public void ApplyMinigameResult(bool succeeded, int hoursToAdvance = 0)
    {
        if (succeeded && activeEventType == GameplayEventType.KingBird)
            KingBirdPhotographed = true;

        // Punto de extensión del MVP: más adelante se aplicarán aquí energía,
        // afecto y el coste temporal de las misiones (por ejemplo, +2 horas).
        if (hoursToAdvance > 0)
            gameClock.AdvanceTime(hoursToAdvance * 60f);

        Debug.Log($"Resultado de {activeEventId}: {(succeeded ? "éxito" : "sin completar")}");
        activeEventId = null;
    }

    public string EvaluateEnding()
    {
        return KingBirdPhotographed
            ? "Final: Osito fotografió al Pájaro Rey."
            : "Final: Osito no logró fotografiar al Pájaro Rey.";
    }

    public void ResetSession()
    {
        resolvedEvents.Clear();
        activeEventId = null;
        AcceptedEvents = 0;
        RejectedEvents = 0;
        KingBirdPhotographed = false;
        gameClock.ResetClock();
    }

    private void OnDestroy()
    {
        if (gameClock != null)
            gameClock.OnDayFinished -= FinishGame;

        if (Instance == this)
            Instance = null;
    }
}
