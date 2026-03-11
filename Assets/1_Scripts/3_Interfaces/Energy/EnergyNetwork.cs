using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a group of connected energy nodes.
/// Handles energy distribution between producers and consumers.
/// </summary>
public class EnergyNetwork
{
    private readonly HashSet<IEnergyNode> _nodes = new HashSet<IEnergyNode>();
    private readonly List<IEnergyConsumer> _consumers = new List<IEnergyConsumer>();
    private readonly List<IEnergyProducer> _producers = new List<IEnergyProducer>();
    private readonly List<IEnergyStorage> _storages = new List<IEnergyStorage>();

    /// <summary>
    /// Registers a node into this specific network and categorizes it based on its interfaces.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(IEnergyNode node)
    {
        if (_nodes.Add(node))
        {
            node.CurrentNetwork = this;

            // Pattern Matching to categorize the node capabilities
            if (node is IEnergyConsumer consumer)
            {
                _consumers.Add(consumer);
            }

            if (node is IEnergyProducer producer)
            {
                _producers.Add(producer);
            }

            if (node is IEnergyStorage storage)
            {
                _storages.Add(storage);
            }
        }
    }
    /// <summary>
    /// Distributes energy prioritizing live production over stored energy.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick.</param>
    public void Tick(float deltaTime)
    {
        float totalInstantProduction = 0f;

        // Step 1: Harvest all instantaneous production
        foreach (IEnergyProducer producer in _producers)
        {
            totalInstantProduction += producer.ProduceEnergy(deltaTime);
        }

        // Step 2: Distribute to consumers
        foreach (IEnergyConsumer consumer in _consumers)
        {
            if (!consumer.NeedsEnergy)
            {
                continue;
            }

            float request = consumer.EnergyRequest;

            // Try to fulfill request with free instant production first
            float fromProduction = Mathf.Min(request, totalInstantProduction);
            consumer.ProvideEnergy(fromProduction);
            totalInstantProduction -= fromProduction;
            request -= fromProduction;

            // If the consumer still needs power, drain the storages (Yellow Balls)
            if (request > 0f)
            {
                foreach (IEnergyStorage storage in _storages)
                {
                    if (request <= 0f)
                    {
                        break;
                    }

                    float fromStorage = storage.ExtractEnergy(request);
                    consumer.ProvideEnergy(fromStorage);
                    request -= fromStorage;
                }
            }
        }

        // Note: Any totalInstantProduction left over here is currently wasted.
        // To charge YellowBalls, loop through _storages here and AddEnergy.
    }
}