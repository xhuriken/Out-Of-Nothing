using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Represents a group of connected energy nodes.
/// Handles synchronized energy distribution between producers and consumers using proportional sharing.
/// </summary>
public class EnergyNetwork
{
    private readonly HashSet<IEnergyNode> _nodes = new HashSet<IEnergyNode>();
    private readonly List<IEnergyConsumer> _consumers = new List<IEnergyConsumer>();
    private readonly List<IEnergyProducer> _producers = new List<IEnergyProducer>();
    private readonly List<IEnergyStorage> _storages = new List<IEnergyStorage>();

    /// <summary>
    /// Gets the collection of nodes currently in this network.
    /// </summary>
    public IEnumerable<IEnergyNode> Nodes => _nodes;

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
            if (node is IEnergyStorage storage) _storages.Add(storage);
        }
    }

    /// <summary>
    /// Processes a single energy cycle. 
    /// Executed synchronously by the EnergyManager via PowerTickManager.
    /// </summary>
    public void ProcessTick(float tickDuration)
    {
        float totalProduced = 0f;
        float totalProvided = 0f;
        float totalRequested = 0f;

        // 1. Production Phase: All producers generate energy into their buffers
        foreach (IEnergyProducer producer in _producers)
        {
            totalProduced += producer.ProduceEnergy(tickDuration);
        }

        // 2. Identify supply: Sum all available energy in storage nodes
        float availableSupply = 0f;
        foreach (IEnergyStorage storage in _storages)
        {
            availableSupply += storage.CurrentEnergy;
        }

        // Exit early if no consumers or no energy
        if (_nodes.Count < 2 || _consumers.Count == 0 || availableSupply <= 0)
        {
            if (totalProduced > 0) LogNetworkSummary(totalProduced, 0, 0);
            return;
        }

        // 3. Demand Calculation: Gather valid requests from all consumers
        // Structure to hold temp request data for proportional sharing
        var requests = new List<(IEnergyConsumer Consumer, float Amount)>();

        foreach (IEnergyConsumer consumer in _consumers)
        {
            if (!consumer.NeedsEnergy) continue;

            float flowCap = consumer.MaxFlowRate * tickDuration;
            float amount = Mathf.Min(consumer.EnergyRequest, flowCap);

            if (amount > 0)
            {
                requests.Add((consumer, amount));
                totalRequested += amount;
            }
        }

        if (totalRequested <= 0)
        {
            if (totalProduced > 0) LogNetworkSummary(totalProduced, 0, 0);
            return;
        }

        // 4. Distribution Phase: Calculate satisfaction ratio (Fair-Share)
        // If supply < demand, everyone gets a proportional percentage (e.g., 50% of what they asked)
        float satisfactionRatio = Mathf.Min(1f, availableSupply / totalRequested);

        foreach (var req in requests)
        {
            float fairAmount = req.Amount * satisfactionRatio;
            float actuallyExtracted = ExtractFromPool(fairAmount, req.Consumer);

            if (actuallyExtracted > 0)
            {
                req.Consumer.ProvideEnergy(actuallyExtracted);
                totalProvided += actuallyExtracted;
            }
        }

        // 5. Logging
        if (totalProduced > 0 || totalProvided > 0)
        {
            LogNetworkSummary(totalProduced, totalProvided, totalRequested);
        }
    }

    /// <summary>
    /// Helper to pull energy from the available storages in the network.
    /// </summary>
    private float ExtractFromPool(float amount, IEnergyConsumer requester)
    {
        float remainingToExtract = amount;

        // A. Take from Generators (Producers that are also Storage) first
        foreach (IEnergyProducer producer in _producers)
        {
            if (remainingToExtract <= 0) break;
            if (producer is IEnergyStorage storage)
            {
                remainingToExtract -= storage.ExtractEnergy(remainingToExtract);
            }
        }

        // B. Take from other Storages (Yellow Balls, etc.)
        if (remainingToExtract > 0)
        {
            foreach (IEnergyStorage storage in _storages)
            {
                if (remainingToExtract <= 0) break;

                // Don't extract from yourself if you are both a consumer and storage
                if (ReferenceEquals(requester, storage)) continue;

                remainingToExtract -= storage.ExtractEnergy(remainingToExtract);
            }
        }

        return amount - remainingToExtract;
    }

    /// <summary>
    /// Logs a summary of the network's energy flow to the console.
    /// </summary>
    private void LogNetworkSummary(float produced, float provided, float requested)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"[Net {GetHashCode().ToString("X")}] ");
        sb.Append($"Nodes: {_nodes.Count} | ");
        sb.Append($"Prod: {produced:F3} | ");
        sb.Append($"Flow: {provided:F3} / {requested:F3} (Supply/Demand)");

        Debug.Log(sb.ToString());
    }
}