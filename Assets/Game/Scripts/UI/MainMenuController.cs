using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    public void Play()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetSession();

        SceneNavigator.Load(SceneNavigator.Intro);
    }

    public void ShowInstructions() => ShowOnly(instructionsPanel);
    public void ShowCredits() => ShowOnly(creditsPanel);
    public void ShowSettings() => ShowOnly(settingsPanel);
    public void ClosePanels() => ShowOnly(null);
    public void Quit() => SceneNavigator.Quit();

    private void ShowOnly(GameObject panel)
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(panel == instructionsPanel);
        if (creditsPanel != null)
            creditsPanel.SetActive(panel == creditsPanel);
        if (settingsPanel != null)
            settingsPanel.SetActive(panel == settingsPanel);
    }
}
