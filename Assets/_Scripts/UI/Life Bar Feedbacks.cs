using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class LifeBarFeedbacks : MonoBehaviour
{
    [Header("Hearts")]
    public Image[] hearts;

    [Header("Timing")]
    public float duration = 0.25f;

    [Header("Heart Pop")]
    public float healStartScale = 0.3f;
    public float healPopScale = 1.3f;

    [Header("Colors")]
    public Color healColor = Color.green;
    public Color damageColor = Color.red;

    [Header("Bar Pop")]
    public float barPopScale = 1.12f;

    [Header("Low HP Pulse")]
    public int lowHPHeartIndex = 0;
    public float lowHPPulseScale = 0.8f;
    public float lowHPPulseBeatDuration = 0.18f;
    public float lowHPPulsePauseDuration = 0.35f;

    Image barImage;
    Color barOriginalColor;
    Vector3 barOriginalScale;

    int lastHP = -1;
    Coroutine barFlashRoutine;
    Coroutine barPopRoutine;

    Coroutine pulseRoutine;
    bool isPulsing = false;
    RectTransform lowHPHeart;
    Vector3 lowHPHeartOriginalScale;

    void Awake()
    {
        barImage = GetComponent<Image>();
        if (barImage != null)
            barOriginalColor = barImage.color;

        barOriginalScale = transform.localScale;

        if (hearts == null || hearts.Length == 0)
            hearts = GetComponentsInChildren<Image>(true);

        InitLowHPHeart();
    }

    void Start()
    {
        lastHP = GetCurrentHP();
        if (lastHP == 1)
            StartPulse();
    }

    void Update()
    {
        int hp = GetCurrentHP();
        if (hp == lastHP) return;

        hp = Mathf.Clamp(hp, 0, hearts.Length);
        int oldHP = Mathf.Clamp(lastHP < 0 ? hp : lastHP, 0, hearts.Length);

        if (hp > oldHP)
        {
            HandleHeal(oldHP, hp);
            FlashBar(healColor);
            PopBar();
        }
        else if (hp < oldHP)
        {
            HandleDamage(hp, oldHP);
            FlashBar(damageColor);
            PopBar();
        }

        if (hp == 1 && !isPulsing)
            StartPulse();
        else if (hp != 1 && isPulsing)
            StopPulse();

        lastHP = hp;
    }

    int GetCurrentHP()
    {
        try
        {
            return Variables.Scene(gameObject.scene).Get<int>("PV player");
        }
        catch
        {
            return lastHP < 0 ? hearts.Length : lastHP;
        }
    }

    void HandleHeal(int oldHP, int newHP)
    {
        for (int i = oldHP; i < newHP; i++)
        {
            if (i < hearts.Length)
                CreateHealGhost(hearts[i]);
        }
    }

    void HandleDamage(int newHP, int oldHP)
    {
        for (int i = newHP; i < oldHP; i++)
        {
            if (i < hearts.Length)
                CreateDamageGhost(hearts[i]);
        }
    }

    void CreateHealGhost(Image source)
    {
        RectTransform src = source.rectTransform;
        Transform parent = src.parent;

        GameObject g = new GameObject("HeartHealFX");
        g.transform.SetParent(parent, false);

        RectTransform rect = g.AddComponent<RectTransform>();
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = src.pivot;
        rect.anchoredPosition = src.anchoredPosition;
        rect.sizeDelta = src.sizeDelta;
        rect.localScale = src.localScale;

        Image img = g.AddComponent<Image>();
        img.sprite = source.sprite;
        img.preserveAspect = true;
        img.color = healColor;

        StartCoroutine(HealRoutine(rect, img));
    }

    void CreateDamageGhost(Image source)
    {
        RectTransform src = source.rectTransform;
        Transform parent = src.parent;

        GameObject g = new GameObject("HeartDamageFX");
        g.transform.SetParent(parent, false);

        RectTransform rect = g.AddComponent<RectTransform>();
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = src.pivot;
        rect.anchoredPosition = src.anchoredPosition;
        rect.sizeDelta = src.sizeDelta;
        rect.localScale = src.localScale;

        Image img = g.AddComponent<Image>();
        img.sprite = source.sprite;
        img.preserveAspect = true;
        img.color = Color.white;

        StartCoroutine(DamageRoutine(rect, img));
    }

    IEnumerator HealRoutine(RectTransform rect, Image img)
    {
        Vector3 baseScale = rect.localScale;
        Color baseCol = Color.white;

        rect.localScale = baseScale * healStartScale;
        img.color = healColor;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / duration;

            float scale = (n < 0.5f)
                ? Mathf.Lerp(healStartScale, healPopScale, n / 0.5f)
                : Mathf.Lerp(healPopScale, 1f, (n - 0.5f) / 0.5f);

            rect.localScale = baseScale * scale;
            img.color = Color.Lerp(healColor, baseCol, n);

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    IEnumerator DamageRoutine(RectTransform rect, Image img)
    {
        Color start = Color.white;
        Color end = damageColor;
        end.a = 0f;

        Vector3 baseScale = rect.localScale;
        Vector3 endScale = baseScale * 0.8f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / duration;

            img.color = Color.Lerp(start, end, n);
            rect.localScale = Vector3.Lerp(baseScale, endScale, n);

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    void FlashBar(Color color)
    {
        if (barFlashRoutine != null)
            StopCoroutine(barFlashRoutine);

        barFlashRoutine = StartCoroutine(FlashBarRoutine(color));
    }

    IEnumerator FlashBarRoutine(Color flashColor)
    {
        float t = 0f;
        barImage.color = flashColor;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / duration;
            barImage.color = Color.Lerp(flashColor, barOriginalColor, n);
            yield return null;
        }

        barImage.color = barOriginalColor;
    }

    void PopBar()
    {
        if (barPopRoutine != null)
            StopCoroutine(barPopRoutine);

        barPopRoutine = StartCoroutine(BarPopRoutine());
    }

    IEnumerator BarPopRoutine()
    {
        float t = 0f;
        Vector3 start = barOriginalScale;
        Vector3 peak = barOriginalScale * barPopScale;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = t / duration;
            transform.localScale = Vector3.Lerp(peak, start, n);
            yield return null;
        }

        transform.localScale = start;
    }

    void InitLowHPHeart()
    {
        if (hearts != null && hearts.Length > 0)
        {
            int index = Mathf.Clamp(lowHPHeartIndex, 0, hearts.Length - 1);
            lowHPHeart = hearts[index] != null ? hearts[index].rectTransform : null;
            if (lowHPHeart != null)
                lowHPHeartOriginalScale = lowHPHeart.localScale;
        }
    }

    void StartPulse()
    {
        if (lowHPHeart == null)
            InitLowHPHeart();

        if (lowHPHeart == null)
            return;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        lowHPHeartOriginalScale = lowHPHeart.localScale;
        pulseRoutine = StartCoroutine(LowHPPulseRoutine());
        isPulsing = true;
    }

    void StopPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        if (lowHPHeart != null)
            lowHPHeart.localScale = lowHPHeartOriginalScale;

        isPulsing = false;
    }

    IEnumerator LowHPPulseRoutine()
    {
        float baseDuration = Mathf.Max(0.01f, lowHPPulseBeatDuration);
        float interBeatDelay = baseDuration * 0.35f;

        while (true)
        {
            if (lowHPHeart == null)
                yield break;

            Vector3 baseScale = lowHPHeartOriginalScale;

            float minScale = lowHPPulseScale;
            float maxScale = 1f + (1f - minScale) * 0.35f;
            float secondMinScale = Mathf.Lerp(1f, minScale, 0.5f);

            float t = 0f;
            while (t < baseDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / baseDuration);
                float eased = 1f - Mathf.Cos(n * Mathf.PI * 0.5f);
                float s = Mathf.Lerp(1f, minScale, eased);
                lowHPHeart.localScale = baseScale * s;
                yield return null;
            }

            t = 0f;
            float upDuration = baseDuration * 0.6f;
            while (t < upDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / upDuration);
                float eased = 1f - Mathf.Cos(n * Mathf.PI * 0.5f);
                float s = Mathf.Lerp(minScale, maxScale, eased);
                lowHPHeart.localScale = baseScale * s;
                yield return null;
            }

            t = 0f;
            float relaxDuration = baseDuration * 0.4f;
            while (t < relaxDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / relaxDuration);
                float eased = 1f - Mathf.Cos(n * Mathf.PI * 0.5f);
                float s = Mathf.Lerp(maxScale, 1f, eased);
                lowHPHeart.localScale = baseScale * s;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(interBeatDelay);

            t = 0f;
            while (t < baseDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / baseDuration);
                float eased = 1f - Mathf.Cos(n * Mathf.PI * 0.5f);
                float s = Mathf.Lerp(1f, secondMinScale, eased);
                lowHPHeart.localScale = baseScale * s;
                yield return null;
            }

            t = 0f;
            while (t < baseDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / baseDuration);
                float eased = 1f - Mathf.Cos(n * Mathf.PI * 0.5f);
                float s = Mathf.Lerp(secondMinScale, 1f, eased);
                lowHPHeart.localScale = baseScale * s;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(lowHPPulsePauseDuration);
        }
    }
}
