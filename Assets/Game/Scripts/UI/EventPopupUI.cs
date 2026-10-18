using TMPro;
using UnityEngine;

public class EventPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private GameplayEventController owner;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public void Initialize(GameplayEventController controller)
    {
        owner = controller;
        Hide();
    }

    public void Show(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;
        if (descriptionText != null)
            descriptionText.text = description;

        isOpen = true;
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Hide()
    {
        isOpen = false;
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void Accept()
    {
        if (isOpen)
            owner?.AcceptCurrentEvent();
    }

    public void Reject()
    {
        if (isOpen)
            owner?.RejectCurrentEvent();
    }
}
