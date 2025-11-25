using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        [TextArea(2, 4)]
        public string text;
        public float duration;
    }

    public Line[] lines;
}
