using System.Collections;
using UnityEngine;
using StarterAssets;
using Unity.VisualScripting;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerDeathScreen : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeOutDuration = 1.0f;
    public float blackScreenDuration = 1.0f;
    public float fadeInDuration = 1.0f;

    public string deathAnimationName = "Death";
    public string deathEndEventName = "DeathReload";

    Animator playerAnimator;
    ThirdPersonController thirdPersonController;
    PogoDashAbility pogoDashAbility;
    StarterAssetsInputs starterAssetsInputs;
#if ENABLE_INPUT_SYSTEM
    PlayerInput playerInput;
#endif

    bool isRunning;

    void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        pogoDashAbility = GetComponent<PogoDashAbility>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        playerInput = GetComponent<PlayerInput>();
#endif
    }

    public void DeathTriggered()
    {
        if (isRunning) return;
        if (fadeCanvasGroup == null) return;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        isRunning = true;

        SetPlayerControl(false);

        if (playerAnimator != null && !string.IsNullOrEmpty(deathAnimationName))
            playerAnimator.CrossFadeInFixedTime(deathAnimationName, 0.05f, 0, 0f);

        yield return Fade(0f, 1f, fadeOutDuration);

        if (!string.IsNullOrEmpty(deathEndEventName))
        {
            for (int i = 0; i < 3; i++)
            {
                CustomEvent.Trigger(gameObject, deathEndEventName);
                yield return null;
            }
        }

        if (thirdPersonController != null)
            thirdPersonController.ResetCameraAfterTeleport();

        if (blackScreenDuration > 0f)
            yield return new WaitForSecondsRealtime(blackScreenDuration);

        yield return Fade(1f, 0f, fadeInDuration);

        SetPlayerControl(true);

        isRunning = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        fadeCanvasGroup.alpha = from;

        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha = to;
            yield break;
        }

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }

    void SetPlayerControl(bool enable)
    {
        if (thirdPersonController != null)
            thirdPersonController.CanMove = enable;

        if (pogoDashAbility != null)
            pogoDashAbility.enabled = enable;

        if (starterAssetsInputs != null)
            starterAssetsInputs.enabled = enable;

#if ENABLE_INPUT_SYSTEM
        if (playerInput != null)
            playerInput.enabled = enable;
#endif
    }
}
