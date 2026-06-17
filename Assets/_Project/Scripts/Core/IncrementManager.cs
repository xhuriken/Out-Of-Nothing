using TMPro;
using UnityEngine;
using Febucci.UI;
using Febucci.UI.Core;

/// <summary>
/// Manages global points/scoring system, updates UI text, and handles individual digit animations.
/// </summary>
public class IncrementManager : MonoBehaviour
{
    public static IncrementManager Instance { get; private set; }

    /// <summary>
    /// The TextMeshPro text component displaying the score.
    /// Supports both 3D text and UI text.
    /// </summary>
    [SerializeField] private TMP_Text _textPoints;

    /// <summary>
    /// The TextAnimator component linked to the points text.
    /// </summary>
    [SerializeField] private TextAnimator_TMP _textAnimator;

    /// <summary>
    /// The TextAnimator Typewriter component linked to the score text.
    /// </summary>
    [SerializeField] private TypewriterCore _typewriter;

    [Header("Score Data")]
    [SerializeField]
    [Tooltip("The current points score.")]
    private double _points = 0;
    
    private bool _isInitialized = false;
    
    /// <summary>
    /// Gets or sets the current points value.
    /// </summary>
    public double Points
    {
        get => _points;
        set { _points = value; }
    }

    /// <summary>
    /// Add points in global points scoring (update the Ui too)
    /// </summary>
    /// <param name="points"></param>
    public void AddPoints(double points)
    {
        _points += points;
        UpdatePointsUI();
    }

    /// <summary>
    /// Remove points in global points scoring (update the Ui too)
    /// </summary>
    /// <param name="points"></param>
    public void RemovePoints(double points)
    {
        _points -= points;
        UpdatePointsUI();
    }

    /// <summary>
    /// Set the points scoring (update the Ui too)
    /// </summary>
    /// <param name="points"></param>
    public void SetPoints(double points)
    {
        _points = points;
        UpdatePointsUI();
    }

    /// <summary>
    /// Update the score text in UI.
    /// </summary>
    private void UpdatePointsUI()
    {
        string scoreStr = _points.ToString("F0");

        if (_typewriter != null && _textAnimator != null)
        {
            if (scoreStr.Length > 0)
            {
                //il est devenu fou, mais la j'ai la flemme de l'arreter
                // Split the score string into the preceding text and the last character
                string precedingText = scoreStr.Substring(0, scoreStr.Length - 1);
                string lastChar = scoreStr.Substring(scoreStr.Length - 1);

                // Set the preceding text instantly (without playing appearance animations)
                _textAnimator.SetText(precedingText, false);

                // Append the last character, keeping it hidden initially for the typewriter
                _textAnimator.AppendText(lastChar, true);

                // Start the typewriter to reveal and animate the last character
                _typewriter.StartShowingText(false);
            }
            else
            {
                _textAnimator.SetText(string.Empty, false);
            }
        }
        else if (_textAnimator != null)
        {
            _textAnimator.SetText(scoreStr);
        }
        else if (_textPoints != null)
        {
            _textPoints.text = scoreStr;
        }
    }

    /// <summary>
    /// Gets the singleton instance of the Increment Manager.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Initializes the score UI once the game starts.
    /// </summary>
    private void Start()
    {
        _isInitialized = true;
        UpdatePointsUI();
    }

    /// <summary>
    /// Updates the UI when values are modified in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _isInitialized)
        {
            UpdatePointsUI();
        }
    }
}
