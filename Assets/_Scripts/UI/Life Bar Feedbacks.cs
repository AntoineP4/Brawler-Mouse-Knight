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

    Image barImage;
    Color barOriginalColor;
    Vector3 barOriginalScale;

    int lastHP = -1;
    Coroutine barFlashRoutine;
    Coroutine barPopRoutine;

    void Awake()
    {
        barImage = GetComponent<Image>();
        if (barImage != null)
            barOriginalColor = barImage.color;

        barOriginalScale = transform.localScale;

        if (hearts == null || hearts.Length == 0)
            hearts = GetComponentsInChildren<Image>(true);
    }

    void Start()
    {
        lastHP = GetCurrentHP();
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
}
