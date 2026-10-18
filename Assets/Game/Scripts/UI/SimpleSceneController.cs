using UnityEngine;

public class SimpleSceneController : MonoBehaviour
{
    public void ContinueToGameplay() => SceneNavigator.Load(SceneNavigator.Gameplay);
    public void ReturnToMainMenu() => SceneNavigator.Load(SceneNavigator.MainMenu);
}
