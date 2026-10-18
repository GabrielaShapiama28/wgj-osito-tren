using UnityEngine;

public class MinigamePlaceholderController : MonoBehaviour
{
    public void CompleteMinigame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyMinigameResult(true);

        SceneNavigator.Load(SceneNavigator.Gameplay);
    }
}
