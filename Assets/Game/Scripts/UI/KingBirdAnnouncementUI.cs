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
    private bool hasRestPose;
    private Vector2 restPosition;
    private Vector3 restScale;
    private Color restGlow;
    private RectTransform animatedBird;
    private Vector2 birdRestPosition;
    private Vector3 birdRestScale;

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

    public void Show(RectTransform bird = null)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        RestorePose();

        if (!hasRestPose)
        {
            if (animatedContent != null)
            {
                restPosition = animatedContent.anchoredPosition;
                restScale = animatedContent.localScale;
            }
            if (glow != null)
                restGlow = glow.color;
            hasRestPose = true;
        }

        animatedBird = bird;
        if (animatedBird != null)
        {
            birdRestPosition = animatedBird.anchoredPosition;
            birdRestScale = animatedBird.localScale;
        }

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
        RestorePose();
        gameObject.SetActive(false);
    }

    private void RestorePose()
    {
        if (hasRestPose)
        {
            if (animatedContent != null)
            {
                animatedContent.anchoredPosition = restPosition;
                animatedContent.localScale = restScale;
            }
            if (glow != null)
                glow.color = restGlow;
        }
        if (animatedBird != null)
        {
            animatedBird.anchoredPosition = birdRestPosition;
            animatedBird.localScale = birdRestScale;
            animatedBird = null;
        }
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / durationSeconds);
            float fadeIn = Mathf.Clamp01(elapsed / 0.12f);
            float fadeOut = Mathf.Clamp01((1f - progress) / 0.28f);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            float entrance = Mathf.Clamp01(elapsed / 0.4f);
            float easeOut = 1f - Mathf.Pow(1f - entrance, 3f);
            float scale = elapsed < 0.24f
                ? Mathf.Lerp(0.72f, 1.12f, Mathf.Clamp01(elapsed / 0.24f))
                : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((elapsed - 0.24f) / 0.22f));

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
            if (animatedContent != null)
            {
                animatedContent.localScale = restScale * scale;
                animatedContent.anchoredPosition = restPosition + Vector2.down * (70f * (1f - easeOut));
            }
            if (animatedBird != null)
            {
                animatedBird.localScale = birdRestScale * (elapsed < 0.24f
                    ? Mathf.Lerp(0.82f, 1.18f, Mathf.Clamp01(elapsed / 0.24f))
                    : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((elapsed - 0.24f) / 0.3f)));
                animatedBird.anchoredPosition = birdRestPosition + new Vector2(100f, 30f) * (1f - easeOut);
            }
            if (glow != null)
            {
                // One entrance flash, then a steady golden announcement.
                Color color = Color.Lerp(Color.white, restGlow, Mathf.Clamp01(elapsed / 0.35f));
                color.a = alpha * Mathf.Lerp(1f, 0.85f, entrance);
                glow.color = color;
            }

            yield return null;
        }

        animationRoutine = null;
        RestorePose();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
