using System.Collections;
using TMPro;
using UnityEngine;

public sealed class KingBirdAnnouncementUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform animatedContent;
    [SerializeField] private UnityEngine.UI.Image glow;
    [SerializeField] private TMP_Text message;
    [Min(0.2f)] [SerializeField] private float durationSeconds = 2.2f;

    private Coroutine animationRoutine;

    public void Configure(
        CanvasGroup configuredCanvasGroup,
        RectTransform configuredContent,
        UnityEngine.UI.Image configuredGlow,
        TMP_Text configuredMessage)
    {
        canvasGroup = configuredCanvasGroup;
        animatedContent = configuredContent;
        glow = configuredGlow;
        message = configuredMessage;
    }

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        gameObject.SetActive(true);
        if (message != null)
            message.text = "¡ES EL PÁJARO REY!";
        animationRoutine = StartCoroutine(Animate());
    }

    public void HideImmediate()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (animatedContent != null)
            animatedContent.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / durationSeconds);
            float fadeIn = Mathf.Clamp01(progress / 0.18f);
            float fadeOut = Mathf.Clamp01((1f - progress) / 0.28f);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 5f) * 0.055f;

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
            if (animatedContent != null)
                animatedContent.localScale = Vector3.one * Mathf.Lerp(0.82f, pulse, fadeIn);
            if (glow != null)
            {
                Color color = glow.color;
                color.a = alpha * (0.66f + Mathf.Sin(progress * Mathf.PI * 8f) * 0.18f);
                glow.color = color;
            }

            yield return null;
        }

        animationRoutine = null;
        gameObject.SetActive(false);
    }
}
