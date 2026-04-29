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

    private readonly List<IEnergyNode> _allNodes = new List<IEnergyNode>();
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

        // reset all arcs before rebuilding
        foreach (ElectricArc arc in _arcPool)
        {
            arc.gameObject.SetActive(false);
        }
        int currentArcIndex = 0;

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

                // Find neighbor candidate colliders intersecting our ConnectionRadius (Circle-vs-Collider)
                int neighborCount = Physics2D.OverlapCircleNonAlloc(
                    currentNode.Position,
                    currentNode.ConnectionRadius,
                    _neighborBuffer
                );

                for (int i = 0; i < neighborCount; i++)
                {
                    IEnergyNode neighbor = GetNodeFromCollider(_neighborBuffer[i]);

                    if (neighbor == null || neighbor == currentNode) continue;

                    // Strictly check connection logic AND distance (Sum of radii)
                    if (CanConnect(currentNode, neighbor))
                    {
                        if (unvisited.Contains(neighbor))
                        {
                            queue.Enqueue(neighbor);
                            unvisited.Remove(neighbor);
                        }

                        ShowArc(currentNode, neighbor, ref currentArcIndex);
                    }
                }
            }
        }

        Debug.Log($"[EnergyManager] Rebuild complete. Found {_networks.Count} independent networks.");
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
    private void ShowArc(IEnergyNode a, IEnergyNode b, ref int index)
    {
        ElectricArc arc;
        if (index < _arcPool.Count)
        {
            arc = _arcPool[index];
        }
        else
        {
            arc = Instantiate(_arcPrefab, transform);
            _arcPool.Add(arc);
        }

        arc.gameObject.SetActive(true);
        arc.Initialize(a, b);
        index++;
    }

    /// <summary>
    /// Draws visual lines between connected nodes in the Unity Editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_networks == null || _networks.Count == 0) return;

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
                    if (CanConnect(nodesList[i], nodesList[j]))
                    {
                        Gizmos.DrawLine(nodesList[i].Position, nodesList[j].Position);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determine if 2 EnergyNode can connect each other based on type and physical overlap.
    /// The actual distance check is handled by Physics2D.OverlapCircle in RebuildNetworks.
    /// </summary>
    private bool CanConnect(IEnergyNode a, IEnergyNode b)
    {
        // 1. Isolate dragged machines
        if (a is MachineEntity ma && ma.IsBeingDragged) return false;
        if (b is MachineEntity mb && mb.IsBeingDragged) return false;

        // 2. Type Check
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