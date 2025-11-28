using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.VisualScripting;
using System.Collections;

public class LowHPPostProcessManager : MonoBehaviour
{
    [SerializeField] private float lowHpIntensity = 0.85f;
    [SerializeField] private float normalIntensity = 0.414f;
    [SerializeField] private Color lowHpColor = new Color(0.5f, 0f, 0f);
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Pulsation (rythme cardiaque)")]
    [SerializeField] private float heartbeatFrequency = 0.8f;
    [SerializeField] private float firstBeatMultiplier = 0.25f;
    [SerializeField] private float secondBeatMultiplier = 0.15f;
    [SerializeField] private float pulseAmplitude = 0.25f;

    private Vignette vignette;
    private Coroutine fadeRoutine;
    private int lastHP = int.MinValue;
    private const string HpVariableName = "PV player";

    private float baseIntensity;
    private Color currentColor;
    private bool isLowHpState;
    private float heartbeatTimer;

    private void Start()
    {
        RefreshVignetteReference();

        if (TryGetCurrentHP(out int currentHP))
        {
            lastHP = currentHP;
            SetLowHP(currentHP == 1);
        }
    }

    private void Update()
    {
        RefreshVignetteReference();
        if (vignette == null) return;

        if (TryGetCurrentHP(out int currentHP))
        {
            if (currentHP != lastHP)
            {
                lastHP = currentHP;
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

    private void RefreshVignetteReference()
    {
        if (vignette != null) return;

        Volume[] volumes = FindObjectsOfType<Volume>();
        foreach (var v in volumes)
        {
            if (v.profile != null && v.profile.TryGet(out Vignette found))
            {
                vignette = found;
                baseIntensity = vignette.intensity.value;
                currentColor = vignette.color.value;
                return;
            }
        }
    }

    private bool TryGetCurrentHP(out int hp)
    {
        hp = 0;

        var sceneVars = Variables.Scene(gameObject.scene);
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

    private void SetLowHP(bool isLowHP)
    {
        if (vignette == null) return;

        isLowHpState = isLowHP;
        heartbeatTimer = 0f;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        float targetIntensity = isLowHP ? lowHpIntensity : normalIntensity;
        Color targetColor = isLowHP ? lowHpColor : normalColor;
        fadeRoutine = StartCoroutine(FadeVignette(targetIntensity, targetColor));
    }

    private IEnumerator FadeVignette(float targetIntensity, Color targetColor)
    {
        float startIntensity = baseIntensity;
        Color startColor = currentColor;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = t / fadeDuration;

            baseIntensity = Mathf.Lerp(startIntensity, targetIntensity, k);
            currentColor = Color.Lerp(startColor, targetColor, k);

            yield return null;
        }

        baseIntensity = targetIntensity;
        currentColor = targetColor;
        fadeRoutine = null;
    }
}
