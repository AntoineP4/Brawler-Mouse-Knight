using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;
using System.Collections;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;

public class LowHPPostProcessManager : MonoBehaviour
{
    [SerializeField] private float lowHpIntensity = 0.85f;
    [SerializeField] private float normalIntensity = 0.414f;
    [SerializeField] private Color lowHpColor = new Color(0.5f, 0f, 0f);
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Transition d'activation")]
    [SerializeField] private float activationFadeDuration = 0.8f;

    [Header("Pulsation (rythme cardiaque)")]
    [SerializeField] private float heartbeatFrequency = 0.8f;
    [SerializeField] private float firstBeatMultiplier = 0.25f;
    [SerializeField] private float secondBeatMultiplier = 0.15f;
    [SerializeField] private float pulseAmplitude = 0.25f;

    [Header("Damage Flash")]
    [SerializeField] private float damageFlashDuration = 0.08f;
    [SerializeField] private float damageFlashIntensityMultiplier = 1.22f;
    [SerializeField] private Color damageFlashColor = new Color(0f, 0f, 0f, 0.65f);

    [Header("FMOD")]
    [SerializeField] private EventReference lowHpHeartbeatEvent;

    [Header("Auto")]
    [SerializeField] private bool autoActivateOnStart = true;

    private Vignette vignette;
    private Coroutine fadeRoutine;
    private Coroutine damageFlashRoutine;
    private Coroutine delayedActivateRoutine;

    private int lastHP = int.MinValue;
    private const string HpVariableName = "PV player";

    private float baseIntensity;
    private Color currentColor;
    private bool isLowHpState;
    private float heartbeatTimer;

    private bool systemActive = false;

    private EventInstance lowHpHeartbeatInstance;
    private bool lowHpHeartbeatPlaying = false;

    private Scene cachedActiveScene;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        cachedActiveScene = SceneManager.GetActiveScene();
        ForceRefreshSceneBindings();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        if (vignette != null)
        {
            baseIntensity = normalIntensity;
            currentColor = normalColor;
            vignette.intensity.value = baseIntensity;
            vignette.color.value = currentColor;
        }

        if (TryGetCurrentHP(out int currentHP))
            lastHP = currentHP;

        if (autoActivateOnStart)
            ActivateLowHpSystem();
    }

    private void Update()
    {
        if (vignette == null)
        {
            RefreshVignetteReference();
            if (vignette == null) return;
        }

        if (!systemActive)
        {
            vignette.intensity.value = baseIntensity;
            vignette.color.value = currentColor;
            return;
        }

        if (TryGetCurrentHP(out int currentHP))
        {
            if (currentHP != lastHP)
            {
                bool lostHP = currentHP < lastHP;
                lastHP = currentHP;

                if (lostHP)
                {
                    if (damageFlashRoutine != null)
                        StopCoroutine(damageFlashRoutine);

                    damageFlashRoutine = StartCoroutine(DamageFlash());
                }

                SetLowHP(currentHP == 1);
            }
        }

        float finalIntensity = baseIntensity;

        if (isLowHpState)
        {
            heartbeatTimer += Time.deltaTime;
            float phase = heartbeatTimer % heartbeatFrequency;

            if (phase < heartbeatFrequency * firstBeatMultiplier)
                finalIntensity = baseIntensity * (1f + pulseAmplitude);
            else if (phase < heartbeatFrequency * (firstBeatMultiplier + secondBeatMultiplier))
                finalIntensity = baseIntensity * (1f + pulseAmplitude * 0.6f);
        }
        else
        {
            heartbeatTimer = 0f;
        }

        vignette.intensity.value = finalIntensity;
        vignette.color.value = currentColor;
    }

    private void OnDestroy()
    {
        StopLowHpHeartbeat();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        cachedActiveScene = newScene;
        ForceRefreshSceneBindings();

        if (systemActive && TryGetCurrentHP(out int currentHP))
        {
            lastHP = currentHP;
            SetLowHP(currentHP == 1, activationFadeDuration);
        }
    }

    private void ForceRefreshSceneBindings()
    {
        vignette = null;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }

        RefreshVignetteReference();
    }

    private void RefreshVignetteReference()
    {
        if (vignette != null) return;

        Scene active = GetActiveSceneSafe();
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);

        for (int i = 0; i < volumes.Length; i++)
        {
            var v = volumes[i];
            if (v == null) continue;

            bool inActiveScene = v.gameObject.scene == active;
            bool global = v.isGlobal;

            if (!inActiveScene && !global) continue;

            if (v.profile != null && v.profile.TryGet(out Vignette found))
            {
                vignette = found;
                return;
            }
        }
    }

    private bool TryGetCurrentHP(out int hp)
    {
        hp = 0;

        Scene active = GetActiveSceneSafe();
        var sceneVars = Variables.Scene(active);
        if (sceneVars == null || !sceneVars.IsDefined(HpVariableName)) return false;

        try
        {
            hp = sceneVars.Get<int>(HpVariableName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Scene GetActiveSceneSafe()
    {
        if (cachedActiveScene.IsValid())
            return cachedActiveScene;
        return SceneManager.GetActiveScene();
    }

    private void SetLowHP(bool isLowHP, float? customFadeDuration = null)
    {
        if (vignette == null) return;

        bool wasLowHp = isLowHpState;
        isLowHpState = isLowHP;
        heartbeatTimer = 0f;

        if (!wasLowHp && isLowHpState) StartLowHpHeartbeat();
        else if (wasLowHp && !isLowHpState) StopLowHpHeartbeat();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        float targetIntensity = isLowHP ? lowHpIntensity : normalIntensity;
        Color targetColor = isLowHP ? lowHpColor : normalColor;

        float durationToUse = customFadeDuration.HasValue ? customFadeDuration.Value : fadeDuration;
        fadeRoutine = StartCoroutine(FadeVignette(targetIntensity, targetColor, durationToUse));
    }

    private IEnumerator FadeVignette(float targetIntensity, Color targetColor, float duration)
    {
        float startIntensity = baseIntensity;
        Color startColor = currentColor;
        float t = 0f;

        if (duration <= 0f)
        {
            baseIntensity = targetIntensity;
            currentColor = targetColor;
            fadeRoutine = null;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            baseIntensity = Mathf.Lerp(startIntensity, targetIntensity, k);
            currentColor = Color.Lerp(startColor, targetColor, k);

            yield return null;
        }

        baseIntensity = targetIntensity;
        currentColor = targetColor;
        fadeRoutine = null;
    }

    private IEnumerator DamageFlash()
    {
        float flashDuration = damageFlashDuration;
        float flashIntensity = baseIntensity * damageFlashIntensityMultiplier;
        Color flashColor = damageFlashColor;

        float originalIntensity = baseIntensity;
        Color originalColor = currentColor;

        float t = 0f;

        if (flashDuration <= 0f)
        {
            baseIntensity = originalIntensity;
            currentColor = originalColor;
            damageFlashRoutine = null;
            yield break;
        }

        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float k = t / flashDuration;

            baseIntensity = Mathf.Lerp(flashIntensity, originalIntensity, k);
            currentColor = Color.Lerp(flashColor, originalColor, k);

            yield return null;
        }

        baseIntensity = originalIntensity;
        currentColor = originalColor;

        damageFlashRoutine = null;
    }

    public void ActivateLowHpSystem()
    {
        if (systemActive) return;
        systemActive = true;

        if (TryGetCurrentHP(out int currentHP))
        {
            lastHP = currentHP;
            SetLowHP(currentHP == 1, activationFadeDuration);
        }
    }

    public void DisableAutoActivate()
    {
        autoActivateOnStart = false;
    }

    public void ActivateLowHpSystemAfterDelay(float delay)
    {
        DisableAutoActivate();

        if (delayedActivateRoutine != null)
            StopCoroutine(delayedActivateRoutine);

        delayedActivateRoutine = StartCoroutine(DelayedActivate(delay));
    }

    private IEnumerator DelayedActivate(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ActivateLowHpSystem();
        delayedActivateRoutine = null;
    }

    private void StartLowHpHeartbeat()
    {
        if (lowHpHeartbeatPlaying) return;
        if (lowHpHeartbeatEvent.IsNull) return;

        lowHpHeartbeatInstance = RuntimeManager.CreateInstance(lowHpHeartbeatEvent);
        lowHpHeartbeatInstance.start();
        lowHpHeartbeatPlaying = true;
    }

    private void StopLowHpHeartbeat()
    {
        if (!lowHpHeartbeatPlaying) return;

        if (lowHpHeartbeatInstance.isValid())
        {
            lowHpHeartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            lowHpHeartbeatInstance.release();
        }

        lowHpHeartbeatPlaying = false;
    }
}
