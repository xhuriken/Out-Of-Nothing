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

    [Header("Debug")]
    [SerializeField] private bool _enableLogs = false;
    public bool EnableLogs => _enableLogs;

    [Header("Arc Settings")]
    [SerializeField] private ElectricArc _arcPrefab;
    [SerializeField] private int _neighborMaxCount = 32;

    private struct Edge : System.IEquatable<Edge>
    {
        public IEnergyNode A;
        public IEnergyNode B;
        public Edge(IEnergyNode a, IEnergyNode b)
        {
            if (a.GetHashCode() < b.GetHashCode()) { A = a; B = b; }
            else { A = b; B = a; }
        }
        public bool Equals(Edge other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is Edge other && Equals(other);
        public override int GetHashCode() => (A.GetHashCode() * 397) ^ B.GetHashCode();
    }

    private HashSet<Edge> _previousEdges = new HashSet<Edge>();
    private HashSet<Edge> _currentEdges = new HashSet<Edge>();
    private HashSet<Edge> _edgeBuffer = new HashSet<Edge>(); // To avoid duplicate arcs

    private List<IEnergyNode> _allNodes = new List<IEnergyNode>();
    private readonly List<EnergyNetwork> _networks = new List<EnergyNetwork>();
    private readonly Collider2D[] _neighborBuffer = new Collider2D[16];
    private readonly List<ElectricArc> _arcPool = new List<ElectricArc>();

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
            MarkTopologyDirty();
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
            MarkTopologyDirty();
            RebuildNetworks(); // Rebuild synchronously to avoid MissingReferenceException in fluid processing
        }
    }
    /// <summary>
    /// Marks the current topology as outdated. Rebuild will happen on the next PowerTick.
    /// </summary>
    public void MarkTopologyDirty()
    {
        _isDirty = true;
    }

    private void OnEnable()
    {
        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPostPowerTick += HandlePowerTick;
        }
    }

    private void OnDisable()
    {
        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPostPowerTick -= HandlePowerTick;
        }
    }

    private void Start()
    {
        if (PowerTickManager.Instance != null)
        {
            PowerTickManager.Instance.OnPostPowerTick -= HandlePowerTick;
            PowerTickManager.Instance.OnPostPowerTick += HandlePowerTick;
        }
    }

    private int _activeArcCount = 0;

    private void HandlePowerTick()
    {
        if (_isDirty)
        {
            RebuildNetworks();
        }

        float tickRate = PowerTickManager.Instance.TickRate;
        foreach (EnergyNetwork network in _networks)
        {
            network.CalculateAllocation(tickRate);
        }
    }

    private void Update()
    {
        HandlePreviewArcs();
    }

    private void HandlePreviewArcs()
    {
        // 1. Find the currently dragged node
        IEnergyNode draggedNode = GetDraggedNode();

        if (draggedNode == null)
        {
            // Just hide any remaining arcs in the pool beyond the active ones
            for (int i = _activeArcCount; i < _arcPool.Count; i++)
            {
                if (_arcPool[i] == null) continue;
                if (_arcPool[i].gameObject.activeSelf)
                    _arcPool[i].gameObject.SetActive(false);
            }
            return;
        }

        // 2. Hide any existing network arcs connected to the dragged node 
        // to avoid visual overlap (Blue vs Gray).
        // EXCEPTION: Yellow balls stay connected, so we keep their real arcs visible.
        bool canStayConnected = draggedNode is YellowBallBehavior;
        if (!canStayConnected)
        {
            for (int i = 0; i < _activeArcCount; i++)
            {
                if (_arcPool[i] == null) continue;
                if (_arcPool[i].IsConnectedTo(draggedNode))
                {
                    _arcPool[i].gameObject.SetActive(false);
                }
            }
        }

        // 3. Find potential neighbors for the dragged node
        int currentPreviewIndex = _activeArcCount;
        _edgeBuffer.Clear();

        int neighborCount = Physics2D.OverlapCircleNonAlloc(
            draggedNode.Position,
            draggedNode.ConnectionRadius,
            _neighborBuffer
        );

        for (int i = 0; i < neighborCount; i++)
        {
            IEnergyNode neighbor = GetNodeFromCollider(_neighborBuffer[i]);
            if (neighbor == null || neighbor == draggedNode) continue;

            // For preview, we ignore the "IsBeingDragged" flag for the check itself
            if (CanConnectInternal(draggedNode, neighbor, true))
            {
                Edge edge = new Edge(draggedNode, neighbor);
                if (!_edgeBuffer.Contains(edge))
                {
                    ShowArc(draggedNode, neighbor, ref currentPreviewIndex, true);
                    _edgeBuffer.Add(edge);
                }
            }
        }

        // 4. Deactivate remaining arcs in the pool
        for (int i = currentPreviewIndex; i < _arcPool.Count; i++)
        {
            if (_arcPool[i] == null) continue;
            if (_arcPool[i].gameObject.activeSelf)
                _arcPool[i].gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        // Fluid processing: interpolates the allocated energy every frame
        foreach (EnergyNetwork network in _networks)
        {
            network.ProcessFluidTransfer(Time.fixedDeltaTime);
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
        _currentEdges.Clear();
        _edgeBuffer.Clear();

        // reset all arcs before rebuilding
        for (int i = _arcPool.Count - 1; i >= 0; i--)
        {
            ElectricArc arc = _arcPool[i];
            if (arc == null)
            {
                _arcPool.RemoveAt(i);
                continue;
            }
            arc.gameObject.SetActive(false);
        }
        int currentArcIndex = 0;

        //HashSet for O(1) lookup during traversal
        HashSet<IEnergyNode> unvisited = new HashSet<IEnergyNode>(_allNodes);

        while (unvisited.Count > 0)
        {
            // Start a new isolated network
            EnergyNetwork newNetwork = new EnergyNetwork();
            _networks.Add(newNetwork);

            // Pass 1: Discover all nodes in this isolated component
            List<IEnergyNode> componentNodes = new List<IEnergyNode>();
            List<IEnergyNode> producersInNetwork = new List<IEnergyNode>();
            
            IEnumerator<IEnergyNode> enumerator = unvisited.GetEnumerator();
            enumerator.MoveNext();
            IEnergyNode root = enumerator.Current;

            Queue<IEnergyNode> discoveryQueue = new Queue<IEnergyNode>();
            discoveryQueue.Enqueue(root);
            unvisited.Remove(root);

            while (discoveryQueue.Count > 0)
            {
                IEnergyNode currentNode = discoveryQueue.Dequeue();
                newNetwork.AddNode(currentNode);
                componentNodes.Add(currentNode);
                
                if (currentNode is IEnergyProducer)
                {
                    producersInNetwork.Add(currentNode);
                }

                int neighborCount = Physics2D.OverlapCircleNonAlloc(
                    currentNode.Position,
                    currentNode.ConnectionRadius,
                    _neighborBuffer
                );

                for (int i = 0; i < neighborCount; i++)
                {
                    IEnergyNode neighbor = GetNodeFromCollider(_neighborBuffer[i]);
                    if (neighbor == null || neighbor == currentNode) continue;

                    if (CanConnectInternal(currentNode, neighbor))
                    {
                        Edge edge = new Edge(currentNode, neighbor);
                        _currentEdges.Add(edge);

                        if (unvisited.Contains(neighbor))
                        {
                            discoveryQueue.Enqueue(neighbor);
                            unvisited.Remove(neighbor);
                        }

                        // Prevent duplicate visual arcs
                        if (!_edgeBuffer.Contains(edge))
                        {
                            ShowArc(currentNode, neighbor, ref currentArcIndex);
                            _edgeBuffer.Add(edge);
                        }
                    }
                }
            }

            // Pass 2: Calculate DistanceToSource (BFS from all producers simultaneously)
            if (producersInNetwork.Count > 0)
            {
                foreach (var node in componentNodes) node.DistanceToSource = int.MaxValue;

                Queue<IEnergyNode> distQueue = new Queue<IEnergyNode>();
                foreach (var prod in producersInNetwork)
                {
                    prod.DistanceToSource = 0;
                    distQueue.Enqueue(prod);
                }

                while (distQueue.Count > 0)
                {
                    IEnergyNode curr = distQueue.Dequeue();
                    
                    int count = Physics2D.OverlapCircleNonAlloc(curr.Position, curr.ConnectionRadius, _neighborBuffer);
                    for (int i = 0; i < count; i++)
                    {
                        IEnergyNode neighbor = GetNodeFromCollider(_neighborBuffer[i]);
                        if (neighbor == null || neighbor == curr) continue;

                        if (neighbor.DistanceToSource == int.MaxValue && CanConnectInternal(curr, neighbor))
                        {
                            neighbor.DistanceToSource = curr.DistanceToSource + 1;
                            distQueue.Enqueue(neighbor);
                        }
                    }
                }
            }
            else
            {
                // No generator in this network
                foreach (var node in componentNodes) node.DistanceToSource = 999;
            }
        }

        foreach (var network in _networks)
        {
            network.SortCables();
        }

        _activeArcCount = currentArcIndex;
        _previousEdges = new HashSet<Edge>(_currentEdges);
        Debug.Log($"[EnergyManager] Rebuild complete. Found {_networks.Count} networks. Arcs: {_activeArcCount}");
    }

    /// <summary>
    /// Helper to extract IEnergyNode from a collider.
    /// </summary>
    private IEnergyNode GetNodeFromCollider(Collider2D col)
    {
        if (col.TryGetComponent(out MachineEntity machine)) return machine;
        if (col.TryGetComponent(out BallEntity ball)) return ball.Behavior as IEnergyNode;
        return null;
    }

    /// <summary>
    /// Activates an arc from the pool and initializes it.
    /// </summary>
    private void ShowArc(IEnergyNode a, IEnergyNode b, ref int index, bool isPreview = false)
    {
        ElectricArc arc = null;
        while (index < _arcPool.Count)
        {
            arc = _arcPool[index];
            if (arc != null) break;
            _arcPool.RemoveAt(index);
        }

        if (arc == null)
        {
            arc = Instantiate(_arcPrefab, transform);
            _arcPool.Add(arc);
        }

        arc.gameObject.SetActive(true);
        arc.Initialize(a, b, isPreview);
        index++;
    }

    /// <summary>
    /// Draws visual lines between connected nodes in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_networks == null || _networks.Count == 0) return;

        Random.State oldState = Random.state;

        foreach (EnergyNetwork network in _networks)
        {
            Random.InitState(network.GetHashCode());
            Gizmos.color = new Color(Random.value, Random.value, Random.value, 1f);

            List<IEnergyNode> nodesList = new List<IEnergyNode>(network.Nodes);

            for (int i = 0; i < nodesList.Count; i++)
            {
                Gizmos.DrawWireSphere(nodesList[i].Position, nodesList[i].PhysicalRadius);

                for (int j = i + 1; j < nodesList.Count; j++)
                {
                    if (CanConnectInternal(nodesList[i], nodesList[j]))
                    {
                        Gizmos.DrawLine(nodesList[i].Position, nodesList[j].Position);
                    }
                }
            }
        }

        Random.state = oldState;
    }

    private IEnergyNode GetDraggedNode()
    {
        foreach (var node in _allNodes)
        {
            if (node.IsBeingDragged) return node;
        }
        return null;
    }

    /// <summary>
    /// Determine if 2 EnergyNode can connect each other based on type and physical overlap.
    /// </summary>
    private bool CanConnectInternal(IEnergyNode a, IEnergyNode b, bool ignoreDrag = false)
    {
        // 1. Isolate dragged nodes (unless we are in preview mode)
        // EXCEPTION: Yellow balls stay connected even during drag.
        if (!ignoreDrag)
        {
            bool aIsYellow = a is YellowBallBehavior;
            bool bIsYellow = b is YellowBallBehavior;

            if (a.IsBeingDragged && !aIsYellow) return false;
            if (b.IsBeingDragged && !bIsYellow) return false;
        }

        // 2. Physical Check (Hysteresis logic)
        Edge edge = new Edge(a, b);
        bool wasConnected = _previousEdges.Contains(edge);
        
        bool isPhysicallyConnected;
        if (ignoreDrag)
        {
            // PREVIEW: Always use Radius-to-Radius (no sticky preview during drag)
            isPhysicallyConnected = EnergyCollisionUtility.AreConnected(a, b);
        }
        else if (wasConnected)
        {
            // MAINTENANCE: Stay connected as long as the connection radius touches the collider
            isPhysicallyConnected = EnergyCollisionUtility.IsConnectionMaintained(a, b);
        }
        else
        {
            // CONNECTION: Initial connection requires attraction radius to touch physical radius
            isPhysicallyConnected = EnergyCollisionUtility.AreConnected(a, b);
        }

        if (!isPhysicallyConnected) return false;

        // 3. Type Check
        // Yellow balls can connect to anything
        if (a is YellowBallBehavior || b is YellowBallBehavior)
        {
            return true;
        }

        // Machines of same type cannot connect directly
        bool aIsProducer = a is IEnergyProducer;
        bool bIsProducer = b is IEnergyProducer;
        bool aIsConsumer = a is IEnergyConsumer;
        bool bIsConsumer = b is IEnergyConsumer;

        return (aIsProducer && bIsConsumer) || (aIsConsumer && bIsProducer);
    }
}