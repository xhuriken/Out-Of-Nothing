using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents a group of connected energy nodes.
/// Handles synchronized energy distribution between producers and consumers using proportional load balancing.
/// </summary>
public class EnergyNetwork
{
    private readonly HashSet<IEnergyNode> _nodes = new HashSet<IEnergyNode>();
    private readonly List<IEnergyConsumer> _consumers = new List<IEnergyConsumer>();
    private readonly List<IEnergyProducer> _producers = new List<IEnergyProducer>();

    /// <summary>
    /// Gets the collection of nodes currently in this network.
    /// </summary>
    public IEnumerable<IEnergyNode> Nodes => _nodes;

    /// <summary>
    /// Returns true if the network contains at least one energy producer.
    /// </summary>
    public bool HasProducers => _producers.Count > 0;

    /// <summary>
    /// Adds a node to the network and registers its energy interfaces.
    /// </summary>
    public void AddNode(IEnergyNode node)
    {
        if (_nodes.Add(node))
        {
            node.CurrentNetwork = this;
            if (node is IEnergyConsumer consumer) _consumers.Add(consumer);
            if (node is IEnergyProducer producer) _producers.Add(producer);
        }
    }

    /// <summary>
    /// Removes a node from the network (used during drag).
    /// </summary>
    public void RemoveNode(IEnergyNode node)
    {
        if (_nodes.Remove(node))
        {
            if (node.CurrentNetwork == this) node.CurrentNetwork = null;
            if (node is IEnergyConsumer consumer) _consumers.Remove(consumer);
            if (node is IEnergyProducer producer) _producers.Remove(producer);
        }
    }

    /// <summary>
    /// Quantizes energy values to 4 decimal places to prevent floating point drift
    /// and ensure "clean" energy packets.
    /// </summary>
    public static float Quantize(float value) => Mathf.Round(value * 10000f) / 10000f;

    /// <summary>
    /// Phase 5: Calculate energy allocation for the upcoming tick.
    /// Executed synchronously by the EnergyManager when a PowerTick occurs.
    /// </summary>
    /// <param name="tickRate">The duration of the tick in seconds.</param>
    public void CalculateAllocation(float tickRate)
    {
        float totalAvailableSupply = 0f;
        float totalDemand = 0f;

        // Reset allocations
        foreach (var node in _nodes)
        {
            node.EnergyAllocationRate = 0f;
        }

        // 1. Calculate Demand (Consumers)
        foreach (IEnergyConsumer consumer in _consumers)
        {
            float missing = consumer.MaxStorage - consumer.CurrentEnergy;
            if (missing > 0f)
            {
                // Demand is capped by their TransferSpeed per tick
                float pullable = Mathf.Min(consumer.InputTransferSpeed, missing);
                totalDemand += pullable;
            }
        }

        // 2. Calculate Supply (Producers / Batteries)
        foreach (IEnergyProducer producer in _producers)
        {
            // Note: production logic itself (creation of energy from nowhere) is handled 
            // fluidly or independently. Here we just count what can be pushed to the network.
            float pushable = Mathf.Min(producer.OutputTransferSpeed, producer.CurrentEnergy);
            totalAvailableSupply += pushable;
        }

        // Quantize totals
        totalAvailableSupply = Quantize(totalAvailableSupply);
        totalDemand = Quantize(totalDemand);

        if (totalAvailableSupply <= 0f || totalDemand <= 0f || _nodes.Count < 2)
        {
            return;
        }

        // 3. Load Balancing Ratios (Pure Pro-Rata)
        float consumerRatio = Mathf.Min(1f, totalAvailableSupply / totalDemand);
        float producerRatio = Mathf.Min(1f, totalDemand / totalAvailableSupply);

        // 4. Allocate Rates (Energy per Second)
        foreach (IEnergyConsumer consumer in _consumers)
        {
            float missing = consumer.MaxStorage - consumer.CurrentEnergy;
            if (missing > 0f)
            {
                float pullable = Mathf.Min(consumer.InputTransferSpeed, missing);
                float allocatedForTick = Quantize(pullable * consumerRatio);
                consumer.EnergyAllocationRate += allocatedForTick / tickRate;
            }
        }

        foreach (IEnergyProducer producer in _producers)
        {
            float pushable = Mathf.Min(producer.OutputTransferSpeed, producer.CurrentEnergy);
            if (pushable > 0f)
            {
                float debitForTick = Quantize(pushable * producerRatio);
                // Subtracted from the node fluidly
                producer.EnergyAllocationRate -= debitForTick / tickRate;
            }
        }

        // LogNetworkSummary(totalAvailableSupply, totalDemand, consumerRatio);
    }

    /// <summary>
    /// Applies the calculated allocation fluidly over the frame.
    /// Executed in FixedUpdate by EnergyManager.
    /// </summary>
    public void ProcessFluidTransfer(float deltaTime)
    {
        foreach (IEnergyNode node in _nodes)
        {
            if (Mathf.Abs(node.EnergyAllocationRate) > 0.0001f)
            {
                float deltaEnergy = node.EnergyAllocationRate * deltaTime;
                node.CurrentEnergy = Quantize(Mathf.Clamp(node.CurrentEnergy + deltaEnergy, 0f, node.MaxStorage));
            }
        }
    }

    private void LogNetworkSummary(float supply, float demand, float ratio)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"[Net {GetHashCode().ToString("X")}] ");
        sb.Append($"Nodes: {_nodes.Count} | ");
        sb.Append($"Supply: {supply:F3} | ");
        sb.Append($"Demand: {demand:F3} | Ratio: {ratio:F2}");

        Debug.Log(sb.ToString());
    }
}
