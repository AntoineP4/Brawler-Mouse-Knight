using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FMODUnity;
using FMOD.Studio;

public class SceneTransitioner : MonoBehaviour
{
    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public string sceneToLoad = "NomDeVotreScene";
    public float fadeDuration = 1.5f;

    [Header("FMOD")]
    public bool stopFmodOnTransition = true;
    public string busPath = "bus:/";
    public float fmodGraceTimeBeforeLoad = 0.15f;

    [Header("Debug")]
    public bool debugLogs = true;

    bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        StartCoroutine(FadeToBlackAndLoadScene());
    }

    IEnumerator FadeToBlackAndLoadScene()
    {
        if (debugLogs)
        {
            Debug.Log($"[SceneTransitioner] Triggered in '{SceneManager.GetActiveScene().name}'. timeScale={Time.timeScale}, target='{sceneToLoad}'");
        }

        if (fadeCanvasGroup != null)
        {
            float timer = 0f;
            float startAlpha = fadeCanvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = (fadeDuration <= 0f) ? 1f : Mathf.Clamp01(timer / fadeDuration);
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            if (debugLogs) Debug.LogWarning("[SceneTransitioner] fadeCanvasGroup NULL -> no visual fade.");
        }

        if (stopFmodOnTransition)
        {
            Bus bus;
            FMOD.RESULT res = RuntimeManager.StudioSystem.getBus(busPath, out bus);

            if (debugLogs) Debug.Log($"[SceneTransitioner] FMOD getBus('{busPath}') -> {res}");

            if (res == FMOD.RESULT.OK)
            {
                bus.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

                if (debugLogs)
                    Debug.Log($"[SceneTransitioner] stopAllEvents(ALLOWFADEOUT) sent. Waiting {fmodGraceTimeBeforeLoad}s...");

                float w = 0f;
                while (w < fmodGraceTimeBeforeLoad)
                {
                    w += Time.unscaledDeltaTime;
                    RuntimeManager.StudioSystem.update();
                    yield return null;
                }
            }
            else
            {
                if (debugLogs)
                    Debug.LogWarning("[SceneTransitioner] getBus failed -> check busPath and FMOD init in this scene.");
            }
        }

        if (debugLogs) Debug.Log($"[SceneTransitioner] Loading '{sceneToLoad}'...");
        SceneManager.LoadScene(sceneToLoad);
    }
}
