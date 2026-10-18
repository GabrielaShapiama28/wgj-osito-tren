using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneNavigator
{
    public const string MainMenu = "MainMenu";
    public const string Intro = "Intro";
    public const string Gameplay = "Gameplay";
    public const string FruitMinigame = "FruitMinigame";
    public const string BirdPhotoMinigame = "BirdPhotoMinigame";

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
