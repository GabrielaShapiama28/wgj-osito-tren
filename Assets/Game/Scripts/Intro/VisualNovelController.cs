
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VisualNovelController : MonoBehaviour
{
    [Header("Visual Novel")]
    [SerializeField] private GameObject visualNovelPanel;
    [SerializeField] private Image cgImage;
    [SerializeField] private GameObject continueButton;

    [Header("CG Images")]
    [SerializeField] private Sprite[] cgs;

    [Header("Next Scenario")]
    [SerializeField] private string nextSceneName;

    private int currentIndex = 0;
    private bool isFinished = false;

    private void Start()
    {
        if (cgs == null || cgs.Length == 0)
        {
            Debug.LogError("No CG images assigned.");
            return;
        }

        if (cgImage == null ||
            visualNovelPanel == null ||
            continueButton == null)
        {
            Debug.LogError(
                "Assign all required references in the Inspector."
            );
            return;
        }

        currentIndex = 0;
        isFinished = false;

        continueButton.SetActive(false);
        OpenVisualNovel();
    }

    private void Update()
    {
        if (isFinished ||
            visualNovelPanel == null ||
            !visualNovelPanel.activeInHierarchy)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            NextCG();
        }
    }

    public void OpenVisualNovel()
    {
        currentIndex = 0;
        isFinished = false;

        continueButton.SetActive(false);
        ShowCurrentCG();
        visualNovelPanel.SetActive(true);
    }

    private void ShowCurrentCG()
    {
        cgImage.sprite = cgs[currentIndex];
    }

    private void NextCG()
    {
        if (currentIndex < cgs.Length - 1)
        {
            currentIndex++;
            ShowCurrentCG();
        }
        else
        {
            FinishVisualNovel();
        }
    }

    private void FinishVisualNovel()
    {
        isFinished = true;
        continueButton.SetActive(true);
    }

    public void LoadNextScenario()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "Enter the next Scene name in the Inspector."
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
