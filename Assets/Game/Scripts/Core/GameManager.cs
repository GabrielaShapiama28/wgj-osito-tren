
using UnityEngine;

[RequireComponent(typeof(GameClock))]
public class GameManager : MonoBehaviour
{
    private GameClock gameClock;

    private void Awake()
    {
        gameClock = GetComponent<GameClock>();
    }

    private void Start()
    {
        gameClock.OnDayFinished += FinishGame;
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
        Debug.Log("¡Terminó el día!");
        // Después conectaremos EndingEvaluator.
    }

    private void OnDestroy()
    {
        if (gameClock != null)
            gameClock.OnDayFinished -= FinishGame;
    }
}
