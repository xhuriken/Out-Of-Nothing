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

public enum MonologueConditionType
{
    ManualOnly,     // Only triggered via custom scripts
    GameStart,      // Triggered automatically after a delay when the game starts
    BallCount,      // Triggered when specific quantities of balls are present on the board
    CraftCompleted, // Triggered when a crafting recipe is completed
    RandomPlaytime  // Triggered randomly during gameplay
}

[System.Serializable]
public struct BallRequirement
{
    [Tooltip("The type of ball to check.")]
    public BallDataSO ballData;
    [Tooltip("The minimum number of this ball type required on the field.")]
    public int requiredCount;
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

    [Header("Line Selection")]
    [SerializeField] private SelectionMode selectionMode = SelectionMode.Random;
    [SerializeField] private MonologueLine[] lines;

    [Header("Trigger Condition")]
    [SerializeField] private MonologueConditionType conditionType = MonologueConditionType.ManualOnly;
    [SerializeField] [Tooltip("If true, this monologue will trigger only once per game session.")] private bool triggerOnlyOnce = true;

    [Header("Condition: Game Start Settings")]
    [SerializeField] [Tooltip("Delay in seconds before triggering after game start.")] private float startDelay = 2f;

    [Header("Condition: Ball Count Settings")]
    [SerializeField] private BallRequirement[] ballRequirements;

    [Header("Condition: Craft Settings")]
    [SerializeField] [Tooltip("Trigger only when this recipe is crafted. If null, triggers on any successful craft.")] private CraftRecipeSO targetRecipe;

    [Header("Condition: Random Settings")]
    [SerializeField] [Range(0f, 1f)] [Tooltip("Probability (0 to 1) of triggering at each interval.")] private float triggerChance = 0.3f;

    private int _currentIndex = 0;

    // Public Getters for Manager Evaluation
    public MonologueConditionType ConditionType => conditionType;
    public bool TriggerOnlyOnce => triggerOnlyOnce;
    public float StartDelay => startDelay;
    public BallRequirement[] BallRequirements => ballRequirements;
    public CraftRecipeSO TargetRecipe => targetRecipe;
    public float TriggerChance => triggerChance;

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
