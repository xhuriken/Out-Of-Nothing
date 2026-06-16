using UnityEngine;

[System.Serializable]
public struct MonologueLine
{
    [TextArea(3, 10)]
    [Tooltip("The text to show. You can use Text Animator tags (e.g. <wave>Hello</wave>).")]
    public string text;

    [Tooltip("How long (in seconds) the text stays on screen after the typewriter animation finishes.")]
    public float exposureTime;
}

[CreateAssetMenu(fileName = "NewMonologueEvent", menuName = "Monologue/Monologue Event")]
public class MonologueEventSO : ScriptableObject
{
    public enum SelectionMode
    {
        Random,
        Sequential,
        SinglePredefined
    }

    [Header("Configuration")]
    [SerializeField] private SelectionMode selectionMode = SelectionMode.Random;

    [Header("Content")]
    [SerializeField] private MonologueLine[] lines;

    private int _currentIndex = 0;

    /// <summary>
    /// Gets a monologue line based on the selection mode.
    /// </summary>
    public MonologueLine GetLine()
    {
        if (lines == null || lines.Length == 0)
        {
            return new MonologueLine { text = "...", exposureTime = 2f };
        }

        switch (selectionMode)
        {
            case SelectionMode.SinglePredefined:
                return lines[0];

            case SelectionMode.Sequential:
                MonologueLine seqLine = lines[_currentIndex];
                _currentIndex = (_currentIndex + 1) % lines.Length;
                return seqLine;

            case SelectionMode.Random:
            default:
                int randomIndex = Random.Range(0, lines.Length);
                return lines[randomIndex];
        }
    }

    /// <summary>
    /// Resets the sequential index back to 0.
    /// </summary>
    public void ResetSequence()
    {
        _currentIndex = 0;
    }
}
