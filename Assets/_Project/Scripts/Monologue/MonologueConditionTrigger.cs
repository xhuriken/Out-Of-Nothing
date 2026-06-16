using UnityEngine;
using System.Collections;

public class MonologueConditionTrigger : MonoBehaviour
{
    public enum ConditionType
    {
        BallCount,
        CraftCompleted,
        RandomPlaytime
    }

    [System.Serializable]
    public struct BallRequirement
    {
        [Tooltip("The type of ball to check.")]
        public BallDataSO ballData;
        [Tooltip("The minimum number of this ball type required on the field.")]
        public int requiredCount;
    }

    [Header("Monologue Configuration")]
    [SerializeField] private MonologueEventSO monologueEvent;
    [SerializeField] [Tooltip("If true, this monologue will trigger only once during the entire game session.")] private bool triggerOnlyOnce = true;
    
    [Header("Condition Configuration")]
    [SerializeField] private ConditionType conditionType = ConditionType.BallCount;

    [Header("Ball Count Settings")]
    [SerializeField] private BallRequirement[] ballRequirements;
    [SerializeField] [Tooltip("How often (in seconds) the system checks the ball counts on the board.")] private float checkInterval = 1f;

    [Header("Craft Settings")]
    [SerializeField] [Tooltip("The specific recipe that must be crafted. If left empty, any successful craft will trigger the monologue.")] private CraftRecipeSO targetRecipe;

    [Header("Random Playtime Settings")]
    [SerializeField] [Tooltip("Time interval between periodic checks.")] private float timeInterval = 30f;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Probability (0 to 1) of triggering the monologue at each check.")] private float triggerChance = 0.3f;

    private bool _hasTriggered = false;
    private bool _requirementsWereMet = false;
    private Coroutine _checkCoroutine;

    private void Start()
    {
        if (monologueEvent == null)
        {
            Debug.LogWarning($"MonologueConditionTrigger on {gameObject.name}: No MonologueEventSO assigned!");
            return;
        }

        switch (conditionType)
        {
            case ConditionType.BallCount:
                _checkCoroutine = StartCoroutine(Co_CheckBallCounts());
                break;
            case ConditionType.CraftCompleted:
                CraftingManager.OnCraftExecuted += OnCraftExecuted;
                break;
            case ConditionType.RandomPlaytime:
                _checkCoroutine = StartCoroutine(Co_RandomPlaytimeCheck());
                break;
        }
    }

    private void OnDestroy()
    {
        if (conditionType == ConditionType.CraftCompleted)
        {
            CraftingManager.OnCraftExecuted -= OnCraftExecuted;
        }

        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
        }
    }

    private void TriggerMonologue()
    {
        if (_hasTriggered && triggerOnlyOnce) return;

        if (MonologueManager.Instance != null)
        {
            MonologueManager.Instance.TriggerMonologue(monologueEvent);
            _hasTriggered = true;

            if (triggerOnlyOnce)
            {
                CleanUpTrigger();
            }
        }
        else
        {
            Debug.LogError("MonologueConditionTrigger: MonologueManager instance is missing in the scene.");
        }
    }

    private void CleanUpTrigger()
    {
        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
            _checkCoroutine = null;
        }
        
        if (conditionType == ConditionType.CraftCompleted)
        {
            CraftingManager.OnCraftExecuted -= OnCraftExecuted;
        }
    }

    // --- BALL COUNT LOGIC ---
    private IEnumerator Co_CheckBallCounts()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            yield return wait;

            bool currentMet = AreBallRequirementsMet();
            
            // Trigger monologue when transitioning from not-met to met
            if (currentMet && !_requirementsWereMet)
            {
                TriggerMonologue();
            }
            
            _requirementsWereMet = currentMet;
        }
    }

    private bool AreBallRequirementsMet()
    {
        if (ballRequirements == null || ballRequirements.Length == 0) return false;

        foreach (var req in ballRequirements)
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
        // Query the BallPoolManager
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

    // --- CRAFT COMPLETED LOGIC ---
    private void OnCraftExecuted(CraftRecipeSO recipe)
    {
        if (targetRecipe == null || targetRecipe == recipe)
        {
            TriggerMonologue();
        }
    }

    // --- RANDOM PLAYTIME LOGIC ---
    private IEnumerator Co_RandomPlaytimeCheck()
    {
        var wait = new WaitForSeconds(timeInterval);
        while (true)
        {
            yield return wait;

            if (Random.value <= triggerChance)
            {
                TriggerMonologue();
            }
        }
    }
}
