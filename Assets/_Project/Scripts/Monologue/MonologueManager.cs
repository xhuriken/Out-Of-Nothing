using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

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

    [Header("Event Prefabs and Data")]
    [SerializeField]
    [Tooltip("The prefab of the Black Hole to spawn.")]
    private GameObject _blackHolePrefab;

    [SerializeField]
    [Tooltip("The prefab of the Shop to spawn.")]
    private GameObject _shopPrefab;

    [SerializeField]
    [Tooltip("The prefab of the First Ball to spawn.")]
    private GameObject _firstBallPrefab;

    [SerializeField]
    [Tooltip("The monologue event that triggers the Black Hole spawn.")]
    private MonologueEventSO _blackHoleStartEvent;

    [Header("Event Delays")]
    [SerializeField]
    [Tooltip("Delay in seconds between the welcome monologue trigger and the First Ball spawn.")]
    private float _firstBallSpawnDelay = 0.5f;

    [SerializeField]
    [Tooltip("Delay in seconds between the 10-balls monologue trigger and the Black Hole spawn.")]
    private float _blackHoleSpawnDelay = 0f;

    [SerializeField]
    [Tooltip("Delay in seconds between the 20-points monologue trigger and the Shop spawn.")]
    private float _shopSpawnDelay = 1f;

    [Header("Event Durations and Eases")]
    [SerializeField]
    [Tooltip("Duration for the First Ball spawn scale animation.")]
    private float _firstBallSpawnDuration = 0.6f;

    [SerializeField]
    [Tooltip("Duration for the Black Hole GRadius spawn animation.")]
    private float _blackHoleSpawnDuration = 1.5f;

    [SerializeField]
    [Tooltip("Duration for the Shop GRadius spawn animation (Xtemps).")]
    private float _shopSpawnDuration = 1f;

    [SerializeField]
    [Tooltip("The Ease type for the Shop GRadius spawn animation.")]
    private Ease _shopSpawnEase = Ease.InOutSine;

    [SerializeField]
    [Tooltip("The Ease type for the Black Hole GRadius spawn animation.")]
    private Ease _blackHoleSpawnEase = Ease.InOutElastic;

    [SerializeField]
    [Tooltip("Safety margin added to the Black Hole attraction range when spawning the Shop.")]
    private float _shopSpawnSafetyMargin = 1.5f;

    private bool _hasTriggered20PointsEvent = false;

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

            // Welcome event logic: spawn FirstBall
            if (mEvent.ConditionType == MonologueConditionType.GameStart)
            {
                if (_firstBallSpawnDelay > 0f)
                {
                    StartCoroutine(Co_SpawnFirstBallAfterDelay(_firstBallSpawnDelay));
                }
                else
                {
                    SpawnFirstBall();
                }
            }
        }
    }

    // --- BALL COUNT EVALUATION ---
    private IEnumerator Co_CheckBallCounts()
    {
        var wait = new WaitForSeconds(ballCountCheckInterval);
        while (true)
        {
            yield return wait;

            // Check if 20 points are reached to trigger monologue and shop spawn
            if (!_hasTriggered20PointsEvent && IncrementManager.Instance != null && IncrementManager.Instance.Points >= 20)
            {
                _hasTriggered20PointsEvent = true;
                Trigger20PointsEvent();
            }

            foreach (var mEvent in monologueEvents)
            {
                if (mEvent == null) continue;
                if (!ShouldEvaluate(mEvent)) continue;

                if (mEvent.ConditionType == MonologueConditionType.BallCount)
                {
                    bool currentlyMet = AreBallRequirementsMet(mEvent);
                    bool previouslyMet = _metBallRequirements.Contains(mEvent);

                    // Trigger on positive transition (not-met -> met)
                    if (currentlyMet && !previouslyMet)
                    {
                        TriggerMonologue(mEvent);
                        _triggeredEvents.Add(mEvent);

                        // 10 balls event (represented by BlackHoleStart) triggers the Black Hole spawn
                        if (mEvent == _blackHoleStartEvent || mEvent.name == "BlackHoleStart")
                        {
                            if (_blackHoleSpawnDelay > 0f)
                            {
                                StartCoroutine(Co_SpawnBlackHoleAfterDelay(_blackHoleSpawnDelay));
                            }
                            else
                            {
                                SpawnBlackHole();
                            }
                        }
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
                else if (mEvent.ConditionType == MonologueConditionType.PointsCount)
                {
                    if (IncrementManager.Instance != null && IncrementManager.Instance.Points >= mEvent.RequiredPoints)
                    {
                        TriggerMonologue(mEvent);
                        _triggeredEvents.Add(mEvent);
                    }
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

    /// <summary>
    /// Spawns the First Ball at (0,0,0) and animates its scale quickly from 0 to 1.
    /// </summary>
    private void SpawnFirstBall()
    {
        if (_firstBallPrefab == null)
        {
            Debug.LogError("MonologueManager: Cannot spawn First Ball because _firstBallPrefab is missing.");
            return;
        }

        GameObject firstBall = Instantiate(_firstBallPrefab, Vector3.zero, Quaternion.identity);
        if (firstBall != null)
        {
            firstBall.transform.localScale = Vector3.zero;
            firstBall.transform.DOScale(Vector3.one, _firstBallSpawnDuration).SetEase(Ease.OutElastic);
        }
    }

    /// <summary>
    /// Spawns the Black Hole at Vector3.zero, sets GRadius to 0, and animates it to StartRadius over YDuration using InOutElastic.
    /// </summary>
    private void SpawnBlackHole()
    {
        if (_blackHolePrefab == null)
        {
            Debug.LogError("MonologueManager: Black Hole prefab is not assigned.");
            return;
        }

        // Avoid spawning multiple Black Holes
        if (FindAnyObjectByType<BlackHole>() != null)
        {
            Debug.LogWarning("MonologueManager: A Black Hole already exists in the scene.");
            return;
        }

        GameObject bhObj = Instantiate(_blackHolePrefab, Vector3.zero, Quaternion.identity);
        BlackHole bh = bhObj.GetComponent<BlackHole>();
        if (bh != null)
        {
            float targetRadius = bh.StartRadius;

            // Start GRadius at 0
            bh.GRadius = 0f;

            // Animate GRadius from 0 to targetRadius using the specified Ease and duration
            DOTween.To(() => bh.GRadius, x => bh.GRadius = x, targetRadius, _blackHoleSpawnDuration)
                   .SetEase(_blackHoleSpawnEase);
        }
    }

    /// <summary>
    /// Triggers the 20 points monologue and schedules the Shop spawn.
    /// </summary>
    private void Trigger20PointsEvent()
    {
       
        StartCoroutine(Co_SpawnShopAfterDelay(_shopSpawnDelay));
    }

    /// <summary>
    /// Triggers the Shop spawn sequence with the configured delay and animation.
    /// Exposes a public interface for spawning the Shop after Black Hole implosions/explosions.
    /// </summary>
    public void RequestShopSpawn()
    {
        StartCoroutine(Co_SpawnShopAfterDelay(_shopSpawnDelay));
    }

    /// <summary>
    /// Coroutine to spawn the Shop after a delay, animating its GRadius from 0 to its base radius.
    /// </summary>
    private IEnumerator Co_SpawnShopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_shopPrefab == null)
        {
            Debug.LogError("MonologueManager: Shop prefab is not assigned.");
            yield break;
        }

        // Avoid spawning multiple Shops
        if (FindAnyObjectByType<Shop>() != null)
        {
            Debug.LogWarning("MonologueManager: A Shop already exists in the scene.");
            yield break;
        }

        // Choose a random position on a circle around Vector3.zero, ensuring it is outside the black hole attraction range
        float spawnRadius = 6f;
        BlackHole bh = FindAnyObjectByType<BlackHole>();
        if (bh != null)
        {
            float bhPhysicsRange = bh.GRadius;
            BlackHolePhysics physics = bh.GetComponent<BlackHolePhysics>();
            if (physics != null)
            {
                bhPhysicsRange += physics.AttractRadiusOffset;
            }
            else
            {
                bhPhysicsRange += 2f;
            }
            float safeMinRadius = bhPhysicsRange + _shopSpawnSafetyMargin;
            if (spawnRadius < safeMinRadius)
            {
                spawnRadius = safeMinRadius;
            }
        }

        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 spawnPosition = (Vector3)(randomDir * spawnRadius);

        // Ensure the spawn position is strictly within the GameZone boundaries
        if (GameZone.Instance != null)
        {
            float margin = 1.0f; // Safe margin for the Shop radius
            spawnPosition.x = Mathf.Clamp(spawnPosition.x, GameZone.Instance.MinX + margin, GameZone.Instance.MaxX - margin);
            spawnPosition.y = Mathf.Clamp(spawnPosition.y, GameZone.Instance.MinY + margin, GameZone.Instance.MaxY - margin);
        }

        GameObject shopObj = Instantiate(_shopPrefab, spawnPosition, Quaternion.identity);
        Shop shop = shopObj.GetComponentInChildren<Shop>();
        if (shop != null)
        {
            float targetRadius = shop.BaseGRadius;

            // Start GRadius at 0
            shop.GRadius = 0f;

            // Animate GRadius from 0 to targetRadius using the specified Ease and duration (Xtemps)
            DOTween.To(() => shop.GRadius, x => shop.GRadius = x, targetRadius, _shopSpawnDuration)
                   .SetEase(_shopSpawnEase);
        }
    }

    /// <summary>
    /// Coroutine to spawn the First Ball after a delay.
    /// </summary>
    private IEnumerator Co_SpawnFirstBallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnFirstBall();
    }

    /// <summary>
    /// Coroutine to spawn the Black Hole after a delay.
    /// </summary>
    private IEnumerator Co_SpawnBlackHoleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBlackHole();
    }
}
