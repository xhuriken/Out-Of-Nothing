using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the creation and destruction of energy networks based on physical proximity.
/// Uses a Flood Fill (BFS) algorithm to detect isolated graphs.
/// </summary>
[DefaultExecutionOrder(-100)]
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    private readonly List<IEnergyNode> _allNodes = new List<IEnergyNode>();
    private readonly List<EnergyNetwork> _networks = new List<EnergyNetwork>();
    private readonly Collider2D[] _neighborBuffer = new Collider2D[16];

    private bool _isDirty;

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
    /// Registers a new node (Ball or Machine) into the energy system.
    /// </summary>
    public void RegisterNode(IEnergyNode node)
    {
        if (!_allNodes.Contains(node))
        {
            _allNodes.Add(node);
            Debug.Log($"[EnergyManager] Registered new node. Total nodes: {_allNodes.Count}");
            RequestRebuild();
        }
    }

    /// <summary>
    /// Unregisters a node and triggers a network recalculation.
    /// </summary>
    public void UnregisterNode(IEnergyNode node)
    {
        if (_allNodes.Remove(node))
        {
            Debug.Log($"[EnergyManager] Unregistered node. Total nodes: {_allNodes.Count}");
            RequestRebuild();
        }
    }
    /// <summary>
    /// Marks the current topology as outdated. Rebuild will happen on the next FixedUpdate.
    /// </summary>
    public void RequestRebuild()
    {
        _isDirty = true;
    }

    private void FixedUpdate()
    {
        if (_isDirty)
        {
            RebuildNetworks();
        }

        foreach (EnergyNetwork network in _networks)
        {
            network.Tick(Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Core Algorithm: Reconstructs all isolated EnergyNetworks from scratch.
    /// This handles Splitting and Merging automatically.
    /// </summary>
    private void RebuildNetworks()
    {
        _isDirty = false;
        _networks.Clear();

        //HashSet for O(1) lookup during traversal
        HashSet < IEnergyNode > unvisited = new HashSet<IEnergyNode>(_allNodes);

        while (unvisited.Count > 0)
        {
            // Start a new isolated network
            EnergyNetwork newNetwork = new EnergyNetwork();
            _networks.Add(newNetwork);

            // Get any starting node
            IEnumerator<IEnergyNode> enumerator = unvisited.GetEnumerator();
            enumerator.MoveNext();
            IEnergyNode root = enumerator.Current;

            // BFS Queue to find all connected neighbors
            Queue<IEnergyNode> queue = new Queue<IEnergyNode>();
            queue.Enqueue(root);
            unvisited.Remove(root);

            while (queue.Count > 0)
            {
                IEnergyNode currentNode = queue.Dequeue();
                newNetwork.AddNode(currentNode);

                // Find physical neighbors using OverlapCircle
                int neighborCount = Physics2D.OverlapCircleNonAlloc(
                    currentNode.Position,
                    currentNode.ConnectionRadius,
                    _neighborBuffer
                );

                for (int i = 0; i < neighborCount; i++)
                {
                    // Check for MachineEntity (MonoBehaviour) OR BallEntity (via Behavior)
                    IEnergyNode neighbor = null;
                    GameObject hitObj = _neighborBuffer[i].gameObject;

                    if (hitObj.TryGetComponent(out MachineEntity machine))
                    {
                        neighbor = machine;
                    }
                    else if (hitObj.TryGetComponent(out BallEntity ball))
                    {
                        neighbor = ball.Behavior as IEnergyNode;
                    }

                    if (neighbor != null && unvisited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        unvisited.Remove(neighbor);
                    }
                }
            }
        }

        Debug.Log($"[EnergyManager] Rebuild complete. Found {_networks.Count} independent networks.");
    }

    /// <summary>
    /// Draws visual lines between connected nodes in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_networks == null || _networks.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        foreach (EnergyNetwork network in _networks)
        {
            List<IEnergyNode> nodesList = new List<IEnergyNode>(network.Nodes);

            for (int i = 0; i < nodesList.Count; i++)
            {
                for (int j = i + 1; j < nodesList.Count; j++)
                {
                    float distance = Vector2.Distance(nodesList[i].Position, nodesList[j].Position);
                    float maxAllowedDistance = Mathf.Max(nodesList[i].ConnectionRadius, nodesList[j].ConnectionRadius);

                    if (distance <= maxAllowedDistance)
                    {
                        Gizmos.DrawLine(nodesList[i].Position, nodesList[j].Position);
                    }
                }
            }
        }
    }
}