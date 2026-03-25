using System.Collections.Generic;
using System.Text;
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

    public IEnumerable<IEnergyNode> Nodes => _nodes;

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

    public void Tick(float deltaTime)
    {
        // Tracking for debugging
        float totalProduced = 0f;
        float totalProvided = 0f;
        float totalRequested = 0f;

        // 1. Production phase
        foreach (IEnergyProducer producer in _producers)
        {
            totalProduced += producer.ProduceEnergy(deltaTime);
        }

        if (_nodes.Count < 2 || _consumers.Count == 0) return;

        // 3. Distribution phase
        foreach (IEnergyConsumer consumer in _consumers)
        {
            if (!consumer.NeedsEnergy) continue;

            float flowCap = consumer.MaxFlowRate * deltaTime;
            float request = Mathf.Min(consumer.EnergyRequest, flowCap);
            totalRequested += request;

            if (request <= 0) continue;

            // A. Take from Generators first
            foreach (IEnergyProducer producer in _producers)
            {
                if (request <= 0) break;
                if (producer is IEnergyStorage storage)
                {
                    float extracted = storage.ExtractEnergy(request);
                    if (extracted > 0)
                    {
                        // FIX: On donne l'énergie extraite au consommateur !
                        consumer.ProvideEnergy(extracted);
                        totalProvided += extracted;
                        request -= extracted;
                    }
                }
            }

            // B. Take from Storages (Yellow Balls / Other buffers)
            if (request > 0)
            {
                foreach (IEnergyStorage storage in _storages)
                {
                    if (request <= 0) break;
                    if (ReferenceEquals(consumer, storage)) continue;

                    float extracted = storage.ExtractEnergy(request);
                    if (extracted > 0)
                    {
                        consumer.ProvideEnergy(extracted);
                        totalProvided += extracted;
                        request -= extracted;
                    }
                }
            }
        }

        // Log summary only if something happened
        if (totalProduced > 0 || totalProvided > 0)
        {
            LogNetworkSummary(totalProduced, totalProvided, totalRequested);
        }
    }

    private void LogNetworkSummary(float produced, float provided, float requested)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"[Net {GetHashCode().ToString("X")}] "); // ID court en Hexa
        sb.Append($"Nodes: {_nodes.Count} | ");
        sb.Append($"Prod: {produced:F3} | ");
        sb.Append($"Flow: {provided:F3} / {requested:F3} (Supply/Demand)");

        Debug.Log(sb.ToString());
    }
}