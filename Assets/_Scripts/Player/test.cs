using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToggleJuice : MonoBehaviour
{
    [SerializeField] private float popScaleOn = 1.12f;
    [SerializeField] private float popScaleOff = 0.90f;
    [SerializeField] private float duration = 0.12f;

    private Toggle _toggle;
    private Transform _target;
    private Vector3 _baseScale;
    private Coroutine _routine;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _target = transform;
        _baseScale = _target.localScale;

        if (_toggle != null)
            _toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        float peak = value ? popScaleOn : popScaleOff;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(PopRoutine(peak));
    }

    private IEnumerator PopRoutine(float peak)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;

            if (p < 0.5f)
            {
                float a = p / 0.5f;
                _target.localScale = Vector3.Lerp(_baseScale, _baseScale * peak, a);
            }
            else
            {
                float a = (p - 0.5f) / 0.5f;
                _target.localScale = Vector3.Lerp(_baseScale * peak, _baseScale, a);
            }

            yield return null;
        }

        _target.localScale = _baseScale;
        _routine = null;
    }
}
