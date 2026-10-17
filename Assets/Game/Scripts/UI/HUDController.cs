
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameClock gameClock;
    [SerializeField] private TMP_Text clockText;

    private void Update()
    {
        if (gameClock == null || clockText == null)
            return;

        clockText.text = gameClock.CurrentTime;
    }
}
