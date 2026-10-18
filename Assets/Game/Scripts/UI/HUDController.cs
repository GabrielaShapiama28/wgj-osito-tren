
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameClock gameClock;
    [SerializeField] private TMP_Text clockText;

    private void OnEnable()
    {
        TryBindClock();
    }

    private void Update()
    {
        TryBindClock();

        if (gameClock == null || clockText == null)
            return;

        clockText.text = gameClock.CurrentTime;
    }

    private void TryBindClock()
    {
        if (gameClock == null && GameManager.Instance != null)
            gameClock = GameManager.Instance.Clock;
    }
}
