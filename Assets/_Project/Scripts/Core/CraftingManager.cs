using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Shapes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the crafting system, centralizing the crafting state and visuals.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxRadius = 4f;
    [SerializeField] private float _radiusGrowthDuration = 0.3f;
    [SerializeField] private Color _failColor = Color.red;
    [SerializeField] private LayerMask _ballLayerMask;
    [SerializeField] private List<CraftRecipeSO> _recipes;

    [Header("Visuals")]
    [SerializeField] private Disc _selectionDisc;
    [SerializeField] private CraftArc _craftArcPrefab;
    [SerializeField] private GameObject _additionalPreviewObject;
    [SerializeField] private GameObject _ballSelectionFeedbackPrefab;

    [Header("Animations")]
    [SerializeField] private float _craftAnimationDuration = 0.8f;
    [SerializeField] private Ease _craftEase = Ease.InElastic;
    [SerializeField] private float _shadowAnimationDuration = 0.3f;
    [SerializeField] private float _resultSpawnDuration = 0.5f;
    [SerializeField] private Ease _resultSpawnEase = Ease.OutBack;
    [SerializeField] private float _selectionFeedbackAnimationDuration = 0.15f;

    [Header("Line Settings")]
    [SerializeField] private int _lineSegments = 8;
    [SerializeField] private float _lineJitter = 0.03f;
    [SerializeField] private float _lineUpdateFrequency = 0.05f;

    private List<BallEntity> _selectedBalls = new List<BallEntity>();
    private List<CraftArc> _activeLines = new List<CraftArc>();
    private bool _isCrafting;
    private GameObject _currentPreview;
    private CraftRecipeSO _currentMatchingRecipe;
    private Color _initialDiscColor;
    private Camera _mainCamera;
    private GameObject _additionalPreviewInstance;
    private Dictionary<BallEntity, GameObject> _selectionFeedbacks = new Dictionary<BallEntity, GameObject>();

    public bool IsCrafting => _isCrafting;
    public bool IsBallSelected(BallEntity ball) => _selectedBalls.Contains(ball);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _mainCamera = Camera.main;
        _initialDiscColor = _selectionDisc.Color;
        _selectionDisc.gameObject.SetActive(false);

        if (_additionalPreviewObject != null)
        {
            // If it's a prefab, instantiate it. If it's a scene object, this will duplicate it, which is usually fine if it's meant to be a template.
            _additionalPreviewInstance = Instantiate(_additionalPreviewObject, transform);
            _additionalPreviewInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Handles the input toggle for crafting mode.
    /// </summary>
    public void OnCraftInput(bool isPressed)
    {
        if (isPressed) StartCrafting();
        else StopCrafting();
    }

    private void StartCrafting()
    {
        if (_isCrafting) return;
        _isCrafting = true;

        // Safety: unlock any balls that might be left over (e.g. from an interrupted FailCraft tween)
        foreach (var ball in _selectedBalls)
        {
            if (ball != null) ball.IsProcessing = false;
        }

        _selectedBalls.Clear();
        _currentMatchingRecipe = null;
        GameCursor.Instance?.SetMode(CursorMode.Craft);
    }

    private void StopCrafting()
    {
        if (!_isCrafting) return;
        _isCrafting = false;

        if (_currentMatchingRecipe != null)
        {
            ExecuteCraft();
        }
        else if (_selectedBalls.Count > 0)
        {
            FailCraft();
        }
        else
        {
            ResetVisuals(false);
        }

        GameCursor.Instance?.SetMode(CursorMode.Normal);
    }

    private void Update()
    {
        if (!_isCrafting) return;

        UpdateDynamicCenter();
        UpdateLine();
        UpdatePreview();
        ValidateSelectedBalls();
        UpdateFeedbackPositions();
    }

    private void UpdateFeedbackPositions()
    {
        foreach (var kvp in _selectionFeedbacks)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.transform.position = kvp.Key.transform.position;
            }
        }
    }

    private void UpdateDynamicCenter()
    {
        if (_selectedBalls.Count == 0) return;

        Vector2 centroid = Vector2.zero;
        foreach (var ball in _selectedBalls)
        {
            centroid += (Vector2)ball.transform.position;
        }
        centroid /= _selectedBalls.Count;

        // Smoothly move the disc to the centroid
        _selectionDisc.transform.position = Vector3.Lerp(_selectionDisc.transform.position, (Vector3)centroid, Time.deltaTime * 10f);
    }

    private void ValidateSelectedBalls()
    {
        if (_selectedBalls.Count == 0) return;

        for (int i = _selectedBalls.Count - 1; i >= 0; i--)
        {
            BallEntity ball = _selectedBalls[i];
            if (ball == null || Vector2.Distance(ball.transform.position, _selectionDisc.transform.position) > _maxRadius)
            {
                DeselectBall(ball);
            }
        }
    }

    private void DeselectBall(BallEntity ball)
    {
        if (ball != null)
        {
            ball.transform.DOKill();
            ball.transform.localScale = Vector3.one;
            ball.transform.DOPunchScale(Vector3.one * -0.1f, 0.2f);
            ball.IsProcessing = false;
        }
        _selectedBalls.Remove(ball);

        // Deselection feedback animation & destruction
        if (ball != null && _selectionFeedbacks.TryGetValue(ball, out GameObject feedback))
        {
            _selectionFeedbacks.Remove(ball);
            if (feedback != null)
            {
                feedback.transform.DOKill();
                feedback.transform.DOScale(Vector3.zero, _selectionFeedbackAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (feedback != null) Destroy(feedback);
                });
            }
        }

        CheckRecipes();

        if (_selectedBalls.Count == 0)
        {
            ResetVisuals(false);
        }
    }

    /// <summary>
    /// Called from GameInputManager when a click occurs during crafting.
    /// Handles toggling balls in/out of the crafting pool.
    /// </summary>
    public void OnClickSelection()
    {
        BallEntity ball = RaycastBall();
        if (ball == null) return;

        if (_selectedBalls.Contains(ball))
        {
            DeselectBall(ball);
        }
        else
        {
            SelectBall(ball);
        }
    }

    private void SelectBall(BallEntity ball)
    {
        if (_selectedBalls.Count == 0)
        {
            // First ball: anchor the crafting circle
            _selectionDisc.transform.position = ball.transform.position;
            _selectionDisc.gameObject.SetActive(true);
            _selectionDisc.Radius = 0;
            _selectionDisc.Color = _initialDiscColor;

            DOTween.To(() => _selectionDisc.Radius, x => _selectionDisc.Radius = x, _maxRadius, _radiusGrowthDuration).SetEase(Ease.OutBack);
        }
        else
        {
            // Check if within current dynamic radius
            float dist = Vector2.Distance(_selectionDisc.transform.position, ball.transform.position);
            if (dist > _maxRadius)
            {
                FlashRedAndShake();
                return;
            }
        }

        _selectedBalls.Add(ball);
        ball.IsProcessing = true; // Prevents behaviors/clicks during craft

        ball.transform.DOKill();
        ball.transform.localScale = Vector3.one;
        ball.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

        // Instantiate selection feedback prefab at ball position with scale animation
        if (_ballSelectionFeedbackPrefab != null && !_selectionFeedbacks.ContainsKey(ball))
        {
            GameObject feedback = Instantiate(_ballSelectionFeedbackPrefab, ball.transform.position, Quaternion.identity, transform);
            feedback.transform.localScale = Vector3.zero;
            feedback.transform.DOScale(Vector3.one, _selectionFeedbackAnimationDuration).SetEase(Ease.OutBack);
            _selectionFeedbacks.Add(ball, feedback);
        }

        CheckRecipes();
    }

    private void FlashRedAndShake()
    {
        // Cancel existing color/pos tweens on the disc if any
        DOTween.Kill(_selectionDisc);
        DOTween.Kill(_selectionDisc.transform);

        // Flash Red once
        DOTween.Sequence()
            .Append(DOTween.To(() => _selectionDisc.Color, x => _selectionDisc.Color = x, _failColor, 0.1f))
            .Append(DOTween.To(() => _selectionDisc.Color, x => _selectionDisc.Color = x, _initialDiscColor, 0.15f))
            .SetTarget(_selectionDisc);

        // Random slight shake
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.15f;
        _selectionDisc.transform.DOPunchPosition((Vector3)randomDir, 0.25f, 10, 1f);
    }

    private BallEntity RaycastBall()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        if (float.IsNaN(mouseScreenPos.x) || float.IsNaN(mouseScreenPos.y)) return null;

        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, _ballLayerMask);
        if (hit.collider != null)
        {
            return hit.collider.GetComponentInParent<BallEntity>();
        }
        return null;
    }

    private void CheckRecipes()
    {
        _currentMatchingRecipe = _recipes.FirstOrDefault(r => r.Matches(_selectedBalls));
    }

    private void UpdatePreview()
    {
        if (_currentMatchingRecipe != null)
        {
            // Instantiate preview if it doesn't exist or is different
            if (_currentPreview == null || _currentPreview.name != _currentMatchingRecipe.shadowPrefab.name + "(Preview)")
            {
                if (_currentPreview != null) DestroyPreview();

                _currentPreview = Instantiate(_currentMatchingRecipe.shadowPrefab);
                _currentPreview.name = _currentMatchingRecipe.shadowPrefab.name + "(Preview)";

                // Appear animation
                _currentPreview.transform.DOScale(1.0f, _shadowAnimationDuration).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    if (_currentPreview != null)
                    {
                        // Subtle hover animation for the preview after appearing
                        _currentPreview.transform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    }
                });

                if (_additionalPreviewInstance != null)
                {
                    _additionalPreviewInstance.SetActive(true);
                    _additionalPreviewInstance.transform.DOKill();
                    _additionalPreviewInstance.transform.localScale = Vector3.zero;
                    _additionalPreviewInstance.transform.DOScale(0.5f, _shadowAnimationDuration).SetEase(Ease.OutBack);
                }
            }
            // Follow the selection disc (which is at the centroid)
            _currentPreview.transform.position = _selectionDisc.transform.position;
            if (_additionalPreviewInstance != null)
            {
                _additionalPreviewInstance.transform.position = _selectionDisc.transform.position;
            }
        }
        else
        {
            if (_currentPreview != null) DestroyPreview();
        }
    }

    private void DestroyPreview()
    {
        if (_currentPreview == null) return;

        GameObject previewToDestroy = _currentPreview;
        _currentPreview = null;

        previewToDestroy.transform.DOKill();
        previewToDestroy.transform.DOScale(0, _shadowAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (previewToDestroy != null) Destroy(previewToDestroy);
        });

        if (_additionalPreviewInstance != null)
        {
            _additionalPreviewInstance.transform.DOKill();
            _additionalPreviewInstance.transform.DOScale(0, _shadowAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                _additionalPreviewInstance.SetActive(false);
            });
        }
    }

    private void UpdateLine()
    {
        int count = _selectedBalls.Count;
        List<(BallEntity, BallEntity)> desiredPairs = new List<(BallEntity, BallEntity)>();

        if (count >= 2)
        {
            for (int i = 0; i < count; i++)
            {
                desiredPairs.Add((_selectedBalls[i], _selectedBalls[(i + 1) % count]));
            }
        }

        List<CraftArc> linesToKeep = new List<CraftArc>();
        List<CraftArc> linesToDespawn = new List<CraftArc>();

        foreach (var arc in _activeLines)
        {
            if (arc == null) continue;

            bool stillDesired = false;
            for (int i = desiredPairs.Count - 1; i >= 0; i--)
            {
                var pair = desiredPairs[i];
                if (arc.StartBall == pair.Item1 && arc.EndBall == pair.Item2)
                {
                    stillDesired = true;
                    linesToKeep.Add(arc);
                    desiredPairs.RemoveAt(i);
                    break;
                }
            }

            if (!stillDesired)
            {
                linesToDespawn.Add(arc);
            }
        }

        // Animate out and destroy all lines that are no longer needed
        foreach (var arc in linesToDespawn)
        {
            if (arc != null)
            {
                arc.Despawn();
            }
        }

        // Instantiate and spawn any new lines
        foreach (var pair in desiredPairs)
        {
            CraftArc arc = Instantiate(_craftArcPrefab, transform);
            arc.Setup(pair.Item1, pair.Item2, _lineSegments, _lineJitter, _lineUpdateFrequency);
            linesToKeep.Add(arc);
        }

        _activeLines = linesToKeep;
    }

    private void ExecuteCraft()
    {
        _isCrafting = false; // Stop updates immediately
        Vector3 center = _selectionDisc.transform.position;
        CraftRecipeSO recipe = _currentMatchingRecipe;

        // Visual cleanup before animation
        ClearLines();
        DestroyPreview();

        // Animate and destroy active selection feedback objects immediately
        foreach (var kvp in _selectionFeedbacks)
        {
            GameObject feedback = kvp.Value;
            if (feedback != null)
            {
                feedback.transform.DOKill();
                feedback.transform.DOScale(Vector3.zero, _selectionFeedbackAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (feedback != null) Destroy(feedback);
                });
            }
        }
        _selectionFeedbacks.Clear();

        // Synchronized shrink animation
        DOTween.To(() => _selectionDisc.Radius, x => _selectionDisc.Radius = x, 0, _craftAnimationDuration).SetEase(_craftEase);

        foreach (var ball in _selectedBalls)
        {
            ball.transform.DOMove(center, _craftAnimationDuration).SetEase(_craftEase);
            ball.transform.DOScale(0, _craftAnimationDuration).SetEase(_craftEase);
        }

        // Final result spawn
        DOVirtual.DelayedCall(_craftAnimationDuration, () =>
        {
            GameObject result = Instantiate(recipe.resultPrefab, center, Quaternion.identity);

            // Bouncy spawn animation
            result.transform.localScale = Vector3.zero;
            result.transform.DOScale(1f, _resultSpawnDuration).SetEase(_resultSpawnEase);

            foreach (var ball in _selectedBalls)
            {
                if (ball != null) BallPoolManager.Instance.ReleaseBall(ball);
            }

            ResetVisuals(true);
        });
    }

    private void FailCraft()
    {
        // Failure: Flash red and unlock balls
        DOTween.To(() => _selectionDisc.Color, x => _selectionDisc.Color = x, _failColor, 0.1f)
            .SetLoops(4, LoopType.Yoyo)
            .OnComplete(() =>
            {
                foreach (var ball in _selectedBalls) ball.IsProcessing = false;
                ResetVisuals(false);
            });
    }

    private void ResetVisuals(bool success)
    {
        if (success)
        {
            // Already handled by ExecuteCraft's internal animation, but safety:
            _selectionDisc.Radius = 0;
            _selectionDisc.gameObject.SetActive(false);
        }
        else
        {
            DOTween.To(() => _selectionDisc.Radius, x => _selectionDisc.Radius = x, 0, 0.3f)
                .SetEase(Ease.InSine)
                .OnComplete(() => _selectionDisc.gameObject.SetActive(false));
        }

        // Safety: Ensure all balls are unlocked
        foreach (var ball in _selectedBalls)
        {
            if (ball != null) ball.IsProcessing = false;
        }

        // Animate and clear active selection feedback objects
        foreach (var kvp in _selectionFeedbacks)
        {
            GameObject feedback = kvp.Value;
            if (feedback != null)
            {
                feedback.transform.DOKill();
                feedback.transform.DOScale(Vector3.zero, _selectionFeedbackAnimationDuration).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (feedback != null) Destroy(feedback);
                });
            }
        }
        _selectionFeedbacks.Clear();

        ClearLines();
        DestroyPreview();

        _selectedBalls.Clear();
        _currentMatchingRecipe = null;
    }

    private void ClearLines()
    {
        foreach (var arc in _activeLines)
        {
            if (arc != null) arc.Despawn();
        }
        _activeLines.Clear();
    }
}
