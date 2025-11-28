using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float charactersPerSecond = 35f;

    public event Action OnLineStarted;

    private Coroutine dialogueRoutine;
    private Action onSequenceEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        subtitleGroup.alpha = 0f;
        subtitleGroup.gameObject.SetActive(false);
        subtitleText.text = string.Empty;
    }

    public void PlaySequence(DialogueSequence sequence, Action onEnded = null)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            return;

        onSequenceEnded = onEnded;

        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = StartCoroutine(RunSequence(sequence));
    }

    private IEnumerator RunSequence(DialogueSequence sequence)
    {
        subtitleGroup.gameObject.SetActive(true);
        yield return FadeTo(1f);

        for (int i = 0; i < sequence.lines.Length; i++)
        {
            OnLineStarted?.Invoke();
            yield return PlayLine(sequence.lines[i]);
        }

        subtitleText.text = string.Empty;
        yield return FadeTo(0f);
        subtitleGroup.gameObject.SetActive(false);
        dialogueRoutine = null;

        var callback = onSequenceEnded;
        onSequenceEnded = null;
        callback?.Invoke();
    }

    private IEnumerator PlayLine(DialogueSequence.Line line)
    {
        string fullText = line.text;
        subtitleText.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
            yield break;

        float delayPerChar = charactersPerSecond <= 0f ? 0f : 1f / charactersPerSecond;

        if (delayPerChar <= 0f)
        {
            subtitleText.text = fullText;
        }
        else
        {
            int index = 0;
            while (index < fullText.Length)
            {
                index++;
                subtitleText.text = fullText.Substring(0, index);
                yield return new WaitForSeconds(delayPerChar);
            }
        }

        float holdTime = Mathf.Max(0f, line.duration);
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = subtitleGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;
            subtitleGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerp);
            yield return null;
        }

        subtitleGroup.alpha = targetAlpha;
    }

    public bool IsPlaying
    {
        get { return dialogueRoutine != null; }
    }
}
