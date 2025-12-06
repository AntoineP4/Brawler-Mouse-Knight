using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeInDuration = 1.5f;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeInDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }
}
