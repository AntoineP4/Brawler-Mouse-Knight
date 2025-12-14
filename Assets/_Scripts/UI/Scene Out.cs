using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using FMODUnity;
using FMOD.Studio;

public class SceneTransitioner : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public string sceneToLoad = "NomDeVotreScene";
    public float fadeDuration = 1.5f;

    bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(FadeToBlackAndLoadScene());
        }
    }

    IEnumerator FadeToBlackAndLoadScene()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        Bus masterBus;
        RuntimeManager.StudioSystem.getBus("bus:/", out masterBus);
        masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        SceneManager.LoadScene(sceneToLoad);
    }
}
