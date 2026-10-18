
using UnityEngine;
using System.Collections;

public class WinPopupAnimation : MonoBehaviour
{
    public float duration = 0.4f;
    public float startScale = 0.1f;

    private Vector3 finalScale;
    private Coroutine animationCoroutine;

    void Awake()
    {
        finalScale = transform.localScale;
    }

    public void Show()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(ScaleUp());
    }

    IEnumerator ScaleUp()
    {
        transform.localScale = Vector3.one * startScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            // La animación empieza rápido y se ralentiza al final.
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            transform.localScale = Vector3.Lerp(
                Vector3.one * startScale,
                finalScale,
                easedProgress
            );

            yield return null;
        }

        transform.localScale = finalScale;
        animationCoroutine = null;
    }
}
