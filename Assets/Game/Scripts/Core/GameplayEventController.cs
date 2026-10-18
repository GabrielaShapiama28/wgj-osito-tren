using System.Collections.Generic;
using UnityEngine;

public class GameplayEventController : MonoBehaviour
{
    [Header("Eventos de Mamá")]
    [Tooltip("Horas completas en las que puede aparecer el favor de Mamá.")]
    [SerializeField] private int[] motherFavorHours = { 9, 12, 15 };
    [SerializeField] private EventPopupUI popup;

    private GameManager manager;
    private string currentEventId;
    private GameplayEventType currentEventType;

    private void Start()
    {
        manager = GameManager.Instance;
        popup?.Initialize(this);

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
        if (manager == null || popup == null || popup.IsOpen)
            return;

        TryOpenNextDueEvent();
    }

    public void AcceptCurrentEvent()
    {
        if (manager == null || string.IsNullOrEmpty(currentEventId))
            return;

        popup.Hide();

        if (currentEventType == GameplayEventType.EndOfDay)
        {
            manager.ResolveEvent(currentEventId, true);
            manager.CompleteDay();
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }

        manager.BeginMinigame(currentEventId, currentEventType);
        SceneNavigator.Load(currentEventType == GameplayEventType.MotherFavor
            ? SceneNavigator.FruitMinigame
            : SceneNavigator.BirdPhotoMinigame);
    }

    public void RejectCurrentEvent()
    {
        if (manager == null || string.IsNullOrEmpty(currentEventId))
            return;

        bool isEndOfDay = currentEventType == GameplayEventType.EndOfDay;
        manager.ResolveEvent(currentEventId, false);
        popup.Hide();

        if (isEndOfDay)
            manager.CompleteDay();

        currentEventId = null;
    }

    private void TryOpenNextDueEvent()
    {
        int currentHour = manager.Clock.CurrentHour;

        if (currentHour >= 20 && !manager.IsEventResolved("end-of-day-20"))
        {
            OpenEvent(
                "end-of-day-20",
                GameplayEventType.EndOfDay,
                "Fin del viaje",
                manager.EvaluateEnding() + "\n\nAceptar vuelve al menú. Rechazar cierra este mensaje."
            );
            return;
        }

        if (currentHour >= 17 && !manager.IsEventResolved("king-bird-17"))
        {
            OpenEvent(
                "king-bird-17",
                GameplayEventType.KingBird,
                "¡Apareció el Pájaro Rey!",
                "Es la oportunidad que Osito esperaba. ¿Quieres intentar fotografiarlo?"
            );
            return;
        }

        List<GameplayEventType> candidates = new List<GameplayEventType>();
        foreach (int hour in motherFavorHours)
        {
            string eventId = $"mother-hour-{hour}";
            if (currentHour >= hour && !manager.IsEventResolved(eventId))
                candidates.Add(GameplayEventType.MotherFavor);
        }

        if (candidates.Count == 0)
            return;

        // Preparado para añadir más eventos por hora: por ahora la única opción es Mamá.
        GameplayEventType selected = candidates[Random.Range(0, candidates.Count)];
        int dueHour = FindFirstPendingMotherHour(currentHour);
        OpenEvent(
            $"mother-hour-{dueHour}",
            selected,
            "Mamá necesita ayuda",
            "Mamá le pide a Osito recoger frutas. ¿Quieres aceptar el favor?"
        );
    }

    private int FindFirstPendingMotherHour(int currentHour)
    {
        int selectedHour = currentHour;
        foreach (int hour in motherFavorHours)
        {
            if (hour <= currentHour && !manager.IsEventResolved($"mother-hour-{hour}"))
                selectedHour = Mathf.Min(selectedHour, hour);
        }

        return selectedHour;
    }

    private void OpenEvent(
        string eventId,
        GameplayEventType eventType,
        string title,
        string description)
    {
        currentEventId = eventId;
        currentEventType = eventType;
        popup.Show(title, description);
    }
}
