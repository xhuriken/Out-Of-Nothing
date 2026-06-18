using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonologueManager : MonoBehaviour
{
    public static MonologueManager Instance { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private MonologueUI monologueUI;

    [Header("Monologue Events Database")]
    [SerializeField] [Tooltip("All dialogue/monologue events that can be evaluated automatically based on conditions.")]
    private List<MonologueEventSO> monologueEvents = new List<MonologueEventSO>();

    [Header("Evaluation Settings")]
    [SerializeField] [Tooltip("How often (in seconds) the system checks the ball counts on the board.")]
    private float ballCountCheckInterval = 1f;
    [SerializeField] [Tooltip("How often (in seconds) the system rolls for random playtime monologues.")]
    private float randomCheckInterval = 15f;

    // Runtime state tracking
    private readonly HashSet<MonologueEventSO> _triggeredEvents = new HashSet<MonologueEventSO>();
    private readonly HashSet<MonologueEventSO> _metBallRequirements = new HashSet<MonologueEventSO>();

    private Coroutine _ballCheckCoroutine;
    private Coroutine _randomCheckCoroutine;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find MonologueUI in the scene if not set
        if (monologueUI == null)
        {
            monologueUI = FindAnyObjectByType<MonologueUI>();
            if (monologueUI == null)
            {
                Debug.LogWarning("MonologueManager: MonologueUI component not found in the scene.");
            }
        }
    }

    private void Start()
    {
        // Initialize evaluations
        _ballCheckCoroutine = StartCoroutine(Co_CheckBallCounts());
        _randomCheckCoroutine = StartCoroutine(Co_RandomPlaytimeCheck());
        
        CraftingManager.OnCraftExecuted += OnCraftExecuted;

        // Trigger GameStart monologues
        TriggerGameStartMonologues();
    }

    private void OnDestroy()
    {
        if (_ballCheckCoroutine != null) StopCoroutine(_ballCheckCoroutine);
        if (_randomCheckCoroutine != null) StopCoroutine(_randomCheckCoroutine);

        CraftingManager.OnCraftExecuted -= OnCraftExecuted;
    }

    /// <summary>
    /// Triggers a monologue event immediately.
    /// </summary>
    /// <param name="monologueEvent">The scriptable object representing the monologue event.</param>
    public void TriggerMonologue(MonologueEventSO monologueEvent)
    {
        if (monologueEvent == null)
        {
            Debug.LogWarning("MonologueManager: Triggered monologue event is null.");
            return;
        }

        if (monologueUI == null)
        {
            monologueUI = FindAnyObjectByType<MonologueUI>();
            if (monologueUI == null)
            {
                Debug.LogError("MonologueManager: Cannot show monologue because MonologueUI is missing from the scene!");
                return;
            }
        }

        MonologueLine line = monologueEvent.GetLine();
        monologueUI.ShowLine(line.text, line.exposureTime);
    }

    /// <summary>
    /// Displays a monologue line directly with the specified text and exposure time.
    /// </summary>
    /// <param name="text">The raw text to display.</param>
    /// <param name="exposureTime">The duration to keep the text on screen after typing finishes.</param>
    public void TriggerMonologueDirect(string text, float exposureTime)
    {
        if (monologueUI == null)
        {
            monologueUI = FindAnyObjectByType<MonologueUI>();
            if (monologueUI == null)
            {
                Debug.LogError("MonologueManager: Cannot show monologue because MonologueUI is missing from the scene!");
                return;
            }
        }

        monologueUI.ShowLine(text, exposureTime);
    }


    // --- EVALUATION UTILITIES ---

    private bool ShouldEvaluate(MonologueEventSO mEvent)
    {
        if (mEvent == null) return false;
        if (mEvent.TriggerOnlyOnce && _triggeredEvents.Contains(mEvent)) return false;
        return true;
    }

    // --- GAME START EVALUATION ---
    private void TriggerGameStartMonologues()
    {
        foreach (var mEvent in monologueEvents)
        {
            if (mEvent == null || mEvent.ConditionType != MonologueConditionType.GameStart) continue;
            if (!ShouldEvaluate(mEvent)) continue;

            StartCoroutine(Co_TriggerAfterDelay(mEvent));
        }
    }

    private IEnumerator Co_TriggerAfterDelay(MonologueEventSO mEvent)
    {
        yield return new WaitForSeconds(mEvent.StartDelay);

        // Double check condition in case it triggered via another pathway during the delay
        if (ShouldEvaluate(mEvent))
        {
            TriggerMonologue(mEvent);
            _triggeredEvents.Add(mEvent);
        }
    }

    // --- BALL COUNT EVALUATION ---
    private IEnumerator Co_CheckBallCounts()
    {
        var wait = new WaitForSeconds(ballCountCheckInterval);
        while (true)
        {
            yield return wait;

            foreach (var mEvent in monologueEvents)
            {
                if (mEvent == null || mEvent.ConditionType != MonologueConditionType.BallCount) continue;
                if (!ShouldEvaluate(mEvent)) continue;

                bool currentlyMet = AreBallRequirementsMet(mEvent);
                bool previouslyMet = _metBallRequirements.Contains(mEvent);

                // Trigger on positive transition (not-met -> met)
                if (currentlyMet && !previouslyMet)
                {
                    TriggerMonologue(mEvent);
                    _triggeredEvents.Add(mEvent);
                }

                if (currentlyMet)
                {
                    _metBallRequirements.Add(mEvent);
                }
                else
                {
                    _metBallRequirements.Remove(mEvent);
                }
            }
        }
    }

    private bool AreBallRequirementsMet(MonologueEventSO mEvent)
    {
        var requirements = mEvent.BallRequirements;
        if (requirements == null || requirements.Length == 0) return false;

        foreach (var req in requirements)
        {
            if (req.ballData == null) continue;

            int activeCount = GetActiveBallCount(req.ballData.id);
            if (activeCount < req.requiredCount)
            {
                return false;
            }
        }

        return true;
    }

    private int GetActiveBallCount(string ballId)
    {
        if (BallPoolManager.Instance != null)
        {
            return BallPoolManager.Instance.GetActiveBallCount(ballId);
        }

        // Fallback: Search hierarchy if manager is missing (e.g. in test scenes)
        int count = 0;
        BallEntity[] balls = FindObjectsByType<BallEntity>(FindObjectsSortMode.None);
        foreach (var ball in balls)
        {
            if (ball != null && ball.Data != null && ball.Data.id == ballId && ball.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    // --- CRAFTING EVALUATION ---
    private void OnCraftExecuted(CraftRecipeSO recipe)
    {
        foreach (var mEvent in monologueEvents)
        {
            if (mEvent == null || mEvent.ConditionType != MonologueConditionType.CraftCompleted) continue;
            if (!ShouldEvaluate(mEvent)) continue;

            // Trigger if target recipe matches (or if target recipe is null, meaning "any craft")
            if (mEvent.TargetRecipe == null || mEvent.TargetRecipe == recipe)
            {
                TriggerMonologue(mEvent);
                _triggeredEvents.Add(mEvent);
            }
        }
    }

    // --- RANDOM PLAYTIME EVALUATION ---
    private IEnumerator Co_RandomPlaytimeCheck()
    {
        var wait = new WaitForSeconds(randomCheckInterval);
        while (true)
        {
            yield return wait;

            foreach (var mEvent in monologueEvents)
            {
                if (mEvent == null || mEvent.ConditionType != MonologueConditionType.RandomPlaytime) continue;
                if (!ShouldEvaluate(mEvent)) continue;

                if (Random.value <= mEvent.TriggerChance)
                {
                    TriggerMonologue(mEvent);
                    _triggeredEvents.Add(mEvent);
                    
                    // Break so we only trigger one random event at a time to prevent overlapping voices
                    break;
                }
            }
        }
    }
}
