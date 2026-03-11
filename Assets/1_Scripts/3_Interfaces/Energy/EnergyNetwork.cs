using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a group of connected energy nodes.
/// Handles energy distribution between producers and consumers.
/// </summary>
public class EnergyNetwork
{
    private readonly HashSet<IEnergyNode>  _nodes     = new HashSet<IEnergyNode>();
    private readonly List<IEnergyConsumer> _consumers = new List<IEnergyConsumer>();
    private readonly List<IEnergyProducer> _producers = new List<IEnergyProducer>();
    private readonly List<IEnergyStorage>  _storages  = new List<IEnergyStorage>();

    /// <summary>
    /// Exposes the nodes for Gizmo drawing in the EnergyManager.
    /// </summary>
    public IEnumerable<IEnergyNode> Nodes
    {
        get { return _nodes; }
    }

    /// <summary>
    /// Registers a node into this specific network and categorizes it based on its interfaces.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(IEnergyNode node)
    {
        if (_nodes.Add(node))
        {
            node.CurrentNetwork = this;

            if (node is IEnergyConsumer consumer) _consumers.Add(consumer);
            if (node is IEnergyProducer producer) _producers.Add(producer);
            if (node is IEnergyStorage storage)   _storages.Add(storage);
        }
    }
    /// <summary>
    /// Distributes energy prioritizing live production over stored energy.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick.</param>
    public void Tick(float deltaTime)
    {
        if (_nodes.Count == 0)
        {
            return;
        }

        float totalInstantProduction = 0f;

        foreach (IEnergyProducer producer in _producers)
        {
            totalInstantProduction += producer.ProduceEnergy(deltaTime);
        }

        if (totalInstantProduction > 0f)
        {
            Debug.Log($"[EnergyNetwork] Generated {totalInstantProduction:F1} energy this tick from {_producers.Count} producer(s).");
        }

        foreach (IEnergyConsumer consumer in _consumers)
        {
            if (!consumer.NeedsEnergy)
            {
                continue;
            }

            float request = consumer.EnergyRequest;

            float fromProduction = Mathf.Min(request, totalInstantProduction);
            if (fromProduction > 0f)
            {
                consumer.ProvideEnergy(fromProduction);
                totalInstantProduction -= fromProduction;
                request -= fromProduction;
                Debug.Log($"[EnergyNetwork] Provided {fromProduction:F1} energy directly from producers.");
            }

            if (request > 0f)
            {
                foreach (IEnergyStorage storage in _storages)
                {
                    if (request <= 0f) break;

                    float fromStorage = storage.ExtractEnergy(request);
                    if (fromStorage > 0f)
                    {
                        consumer.ProvideEnergy(fromStorage);
                        request -= fromStorage;
                        Debug.Log($"[EnergyNetwork] Extracted {fromStorage:F1} energy from storage.");
                    }
                }
            }
        }
    }
}