using TMPro;
using UnityEngine;

public sealed class GameplayStatusUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image motherFill;
    [SerializeField] private UnityEngine.UI.Image selfFill;
    [SerializeField] private TMP_Text motherValueText;
    [SerializeField] private TMP_Text selfValueText;
    [SerializeField] private GameObject motherHappy;
    [SerializeField] private GameObject motherSerious;
    [SerializeField] private GameObject motherSad;

    private GameManager manager;

    private void OnEnable()
    {
        BindManager();
        Refresh();
    }

    private void Update()
    {
        if (manager == null)
            BindManager();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnStatusChanged -= Refresh;
    }

    public void Configure(
        UnityEngine.UI.Image configuredMotherFill,
        UnityEngine.UI.Image configuredSelfFill,
        TMP_Text configuredMotherValue,
        TMP_Text configuredSelfValue,
        GameObject configuredHappy,
        GameObject configuredSerious,
        GameObject configuredSad)
    {
        motherFill = configuredMotherFill;
        selfFill = configuredSelfFill;
        motherValueText = configuredMotherValue;
        selfValueText = configuredSelfValue;
        motherHappy = configuredHappy;
        motherSerious = configuredSerious;
        motherSad = configuredSad;
    }

    private void BindManager()
    {
        if (manager == GameManager.Instance)
            return;

        if (manager != null)
            manager.OnStatusChanged -= Refresh;
        manager = GameManager.Instance;
        if (manager != null)
            manager.OnStatusChanged += Refresh;
    }

    private void Refresh()
    {
        if (manager == null)
            return;

        SetBar(motherFill, motherValueText, manager.MotherRelationship);
        SetBar(selfFill, selfValueText, manager.SelfRelationship);

        int mother = manager.MotherRelationship;
        if (motherHappy != null)
            motherHappy.SetActive(mother >= 10);
        if (motherSerious != null)
            motherSerious.SetActive(mother >= 5 && mother < 10);
        if (motherSad != null)
            motherSad.SetActive(mother < 5);
    }

    private static void SetBar(UnityEngine.UI.Image fill, TMP_Text label, int value)
    {
        if (fill != null)
        {
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = value / (float)GameManager.MaximumStatusValue;
            fill.color = value >= 10
                ? new Color32(252, 207, 75, 255)
                : value >= 5
                    ? new Color32(244, 147, 62, 255)
                    : new Color32(220, 76, 86, 255);
        }

        if (label != null)
            label.text = $"{value}/15";
    }
}
