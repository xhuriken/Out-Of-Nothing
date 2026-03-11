using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the creation and destruction of energy networks based on physical proximity.
/// Uses a Flood Fill (BFS) algorithm to detect isolated graphs.
/// </summary>
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
                    // Look for the interface in parent (for machine proxies) or the object itself
                    IEnergyNode neighbor = _neighborBuffer[i].GetComponentInParent<IEnergyNode>();

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
}