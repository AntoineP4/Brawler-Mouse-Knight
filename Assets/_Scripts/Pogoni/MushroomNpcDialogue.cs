using UnityEngine;

public class MushroomNpcDialogue : MonoBehaviour
{
    [SerializeField] private DialogueSequence dialogue;

    private void OnTriggerEnter(Collider other)
    {
        if (DialogueManager.Instance == null)
            return;

        if (!other.TryGetComponent(out StarterAssets.StarterAssetsInputs _))
            return;

        if (dialogue != null)
            DialogueManager.Instance.PlaySequence(dialogue);
    }

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null || dialogue == null)
            return;

        DialogueManager.Instance.PlaySequence(dialogue);
    }
}
