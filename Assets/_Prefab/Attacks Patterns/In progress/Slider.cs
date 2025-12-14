using UnityEngine;
using System.Collections;

public class AttackHitByScale : MonoBehaviour
{
    [SerializeField] float targetScale = 1f;
    [SerializeField] float tolerance = 0.02f;

    [SerializeField] Renderer zoneRenderer;
    [SerializeField] Renderer fillRenderer;

    [SerializeField] float hitDuration = 0.10f;

    static readonly int HitID = Shader.PropertyToID("_Hit");

    MaterialPropertyBlock zoneBlock;
    MaterialPropertyBlock fillBlock;

    bool triggered;
    Coroutine routine;

    void Awake()
    {
        zoneBlock = new MaterialPropertyBlock();
        fillBlock = new MaterialPropertyBlock();
        ApplyHit(0f);
    }

    void Update()
    {
        if (triggered) return;
        if (transform.localScale.x >= targetScale - tolerance)
        {
            triggered = true;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(HitRoutine());
        }
    }

    IEnumerator HitRoutine()
    {
        float t = 0f;
        ApplyHit(1f);
        while (t < hitDuration)
        {
            t += Time.unscaledDeltaTime;
            ApplyHit(1f - Mathf.Clamp01(t / hitDuration));
            yield return null;
        }
        ApplyHit(0f);
    }

    void ApplyHit(float v)
    {
        if (zoneRenderer != null)
        {
            zoneRenderer.GetPropertyBlock(zoneBlock);
            zoneBlock.SetFloat(HitID, v);
            zoneRenderer.SetPropertyBlock(zoneBlock);
        }
        if (fillRenderer != null)
        {
            fillRenderer.GetPropertyBlock(fillBlock);
            fillBlock.SetFloat(HitID, v);
            fillRenderer.SetPropertyBlock(fillBlock);
        }
    }

    public void ResetTrigger()
    {
        triggered = false;
        ApplyHit(0f);
    }
}
