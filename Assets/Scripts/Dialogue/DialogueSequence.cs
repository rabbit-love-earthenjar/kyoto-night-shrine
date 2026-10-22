using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    public string speaker;
    [TextArea(2, 6)] public string text;
    public Sprite portrait;
}

// Content stays in data assets so the author can add text without editing code.
[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Night Shrine/Dialogue Sequence")]
public sealed class DialogueSequence : ScriptableObject
{
    [SerializeField] private string sequenceId;
    [SerializeField] private DialogueLine[] lines = new DialogueLine[0];

    public string SequenceId => sequenceId;
    public DialogueLine[] Lines => lines;
    public bool HasContent
    {
        get
        {
            if (lines == null) return false;
            foreach (DialogueLine line in lines)
                if (line != null && !string.IsNullOrWhiteSpace(line.text)) return true;
            return false;
        }
    }
}
