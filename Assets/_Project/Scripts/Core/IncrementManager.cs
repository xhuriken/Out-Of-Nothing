using TMPro;
using UnityEngine;

public class IncrementManager : MonoBehaviour
{
    public static IncrementManager Instance { get; private set; }

    [SerializeField] private TextMeshPro _textPoints;

    private double _points = 0;
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
        // update Ui ?
        UpdatePointsUI();
    }

    /// <summary>
    /// Remove points in global points scoring (update the Ui too)
    /// </summary>
    /// <param name="points"></param>
    public void RemovePoints(double points)
    {
        _points -= points;
        // update UI ?
        UpdatePointsUI();
    }

    /// <summary>
    /// Set the points scoring (update the Ui too)
    /// </summary>
    /// <param name="points"></param>
    public void SetPoints(double points)
    {
        _points = points;
        // update UI ?
        UpdatePointsUI();
    }

    /// <summary>
    /// Update the score text in UI
    /// </summary>
    private void UpdatePointsUI()
    {
        // here update the textmeshpro with animation like TextAnimator
        if(_textPoints) _textPoints.text = _points.ToString(); // temps
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

}
