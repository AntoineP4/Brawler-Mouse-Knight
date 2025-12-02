using UnityEngine;

public class MushroomNpcDialogue : MonoBehaviour
{
    [SerializeField] private DialogueSequence dialogue;
    [SerializeField] private Animator animator;

    [SerializeField] private string talkingBoolName = "IsTalking";
    [SerializeField] private string talkVariantParamName = "TalkVariant";
    [SerializeField] private int talkVariantsCount = 3;

    private bool hasPlayed = false;
    private bool isTalkingActive = false;

    private Collider dialogueTriggerCollider;

    private void Awake()
    {
        dialogueTriggerCollider = GetComponent<Collider>();
        if (dialogueTriggerCollider != null && !dialogueTriggerCollider.isTrigger)
        {
            dialogueTriggerCollider = null;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayed) return;
        if (DialogueManager.Instance == null) return;
        if (!other.TryGetComponent(out StarterAssets.StarterAssetsInputs _)) return;
        if (dialogue == null) return;

        hasPlayed = true;
        StartDialogue();
        DisableTrigger();
    }

    public void TriggerDialogue()
    {
        if (hasPlayed) return;
        if (DialogueManager.Instance == null || dialogue == null) return;

        hasPlayed = true;
        StartDialogue();
        DisableTrigger();
    }

    private void StartDialogue()
    {
        isTalkingActive = true;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnLineStarted += HandleLineStarted;

        SetRandomTalkVariant();
        SetTalking(true);

        DialogueManager.Instance.PlaySequence(dialogue, OnDialogueFinished);
    }

    private void OnDialogueFinished()
    {
        isTalkingActive = false;
        SetTalking(false);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnLineStarted -= HandleLineStarted;
    }

    private void HandleLineStarted()
    {
        if (!isTalkingActive) return;
        SetRandomTalkVariant();
    }

    private void SetTalking(bool value)
    {
        if (animator != null)
            animator.SetBool(talkingBoolName, value);
    }

    private void SetRandomTalkVariant()
    {
        if (animator == null || talkVariantsCount <= 0) return;

        int randomIndex = Random.Range(0, talkVariantsCount);
        animator.SetInteger(talkVariantParamName, randomIndex);
    }

    private void DisableTrigger()
    {
        if (dialogueTriggerCollider != null)
            dialogueTriggerCollider.enabled = false;
    }
}
