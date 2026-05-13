using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/CraftRecipe")]
public class CraftRecipeSO : ScriptableObject
{
    [System.Serializable]
    public struct BallRequirement
    {
        public BallDataSO ballData;
        public int count;
    }

    [Title("Requirements")]
    public List<BallRequirement> requirements;

    [Title("Results")]
    public GameObject resultPrefab;
    public GameObject shadowPrefab;

    public bool Matches(List<BallEntity> selectedBalls)
    {
        if (selectedBalls.Count == 0) return false;

        // Group by ID to check counts
        var currentCounts = new Dictionary<string, int>();
        foreach (var ball in selectedBalls)
        {
            string id = ball.Data.id;
            if (currentCounts.ContainsKey(id))
                currentCounts[id]++;
            else
                currentCounts[id] = 1;
        }

        // Compare with requirements
        if (currentCounts.Count != requirements.Count) return false;

        foreach (var req in requirements)
        {
            if (!currentCounts.ContainsKey(req.ballData.id) || currentCounts[req.ballData.id] != req.count)
            {
                return false;
            }
        }

        // Total count must also match to avoid extra balls
        int totalRequired = 0;
        foreach (var req in requirements) totalRequired += req.count;
        if (selectedBalls.Count != totalRequired) return false;

        return true;
    }
}
