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
    private readonly List<YellowBallBehavior> _cables = new List<YellowBallBehavior>();
    private float _networkEfficiency = 1f;

    /// <summary>
    /// Gets the collection of nodes currently in this network.
    /// </summary>
    public IEnumerable<IEnergyNode> Nodes => _nodes;

    /// <summary>
    /// Returns true if the network contains at least one generator or a storage node with energy.
    /// </summary>
    public bool HasProducers
    {
        get
        {
            foreach (var prod in _producers)
            {
                if (!(prod is YellowBallBehavior)) return true; // It's a generator
                if (prod.CurrentEnergy > 0.001f) return true; // It's a cable with energy
            }
            return false;
        }
    }

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
            if (node is YellowBallBehavior cable) _cables.Add(cable);
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
            if (node is YellowBallBehavior cable) _cables.Remove(cable);
        }
    }
    /// <summary>
    /// Sorts the cables (YellowBalls) by their topological distance to the nearest producer.
    /// Should be called after a network rebuild is complete.
    /// </summary>
    public void SortCables()
    {
        _cables.Sort((a, b) => a.DistanceToSource.CompareTo(b.DistanceToSource));
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
        // 1. Reset allocations
        foreach (var node in _nodes)
        {
            if (node == null || (node is UnityEngine.Object obj && obj == null)) continue;
            node.EnergyAllocationRate = 0f;
        }

        // 2. Identify "Source" Producers (Generators) vs Consumers (Machines)
        float sourceSupply = 0f;
        List<IEnergyProducer> generators = new List<IEnergyProducer>();
        foreach (var prod in _producers)
        {
            if (prod == null || (prod is UnityEngine.Object obj && obj == null)) continue;
            if (prod is YellowBallBehavior) continue; // Cables are processed separately
            generators.Add(prod);
            sourceSupply += Quantize(Mathf.Min(prod.OutputTransferSpeed, prod.CurrentEnergy));
        }

        float machineDemand = 0f;
        List<IEnergyConsumer> machines = new List<IEnergyConsumer>();
        foreach (var cons in _consumers)
        {
            if (cons == null || (cons is UnityEngine.Object obj && obj == null)) continue;
            if (cons is YellowBallBehavior) continue; // Cables are processed separately
            float missing = cons.MaxStorage - cons.CurrentEnergy;
            if (missing > 0f)
            {
                float pull = Mathf.Min(cons.InputTransferSpeed, missing);
                machineDemand += pull;
                machines.Add(cons);
            }
        }

        // 3. Phase A: Fill Cables from Source (Near -> Far)
        float remainingSource = sourceSupply;
        foreach (var cable in _cables)
        {
            if (cable == null || (cable is UnityEngine.Object obj && obj == null)) continue;
            float missing = cable.MaxStorage - cable.CurrentEnergy;
            if (missing > 0f && remainingSource > 0f)
            {
                float pull = Mathf.Min(cable.InputTransferSpeed, missing, remainingSource);
                cable.EnergyAllocationRate += pull / tickRate;
                remainingSource -= pull;
            }
        }

        // 4. Phase B: Supply Machines from remaining Source
        float providedBySourceToMachines = Mathf.Min(machineDemand, remainingSource);
        remainingSource -= providedBySourceToMachines;
        float remainingMachineDemand = machineDemand - providedBySourceToMachines;

        // 5. Phase C: Draw from Cables for Machines (Far -> Near)
        float providedByCablesToMachines = 0f;
        if (remainingMachineDemand > 0f)
        {
            // Reverse iteration: farthest first
            for (int i = _cables.Count - 1; i >= 0; i--)
            {
                var cable = _cables[i];
                if (cable == null || (cable is UnityEngine.Object obj && obj == null)) continue;
                float available = Mathf.Min(cable.OutputTransferSpeed, cable.CurrentEnergy);
                if (available > 0f)
                {
                    float take = Mathf.Min(available, remainingMachineDemand);
                    cable.EnergyAllocationRate -= take / tickRate;
                    providedByCablesToMachines += take;
                    remainingMachineDemand -= take;
                    if (remainingMachineDemand <= 0f) break;
                }
            }
        }

        // 6. Finalize Machine Allocations (Pro-rata distribution when under-fed)
        float totalProvidedToMachines = providedBySourceToMachines + providedByCablesToMachines;
        if (totalProvidedToMachines < machineDemand - 0.0001f)
        {
            float satisfactionRatio = machineDemand > 0f ? (totalProvidedToMachines / machineDemand) : 0f;
            
            // Custom Curve matching user's exact specifications:
            // - S = 1.0 -> E = 1.0 (Full speed)
            // - S = 0.666 (3 machines) -> E = 0.5 (50% speed)
            // - S = 0.5 (4 machines) -> E = 0.05 (5% speed / extremely slow)
            // - S < 0.5 -> decays exponentially
            float efficiency = 1f;
            if (satisfactionRatio >= 0.666f)
            {
                float t = (satisfactionRatio - 0.666f) / (1f - 0.666f);
                efficiency = Mathf.Lerp(0.5f, 1f, t);
            }
            else if (satisfactionRatio >= 0.5f)
            {
                float t = (satisfactionRatio - 0.5f) / (0.666f - 0.5f);
                efficiency = Mathf.Lerp(0.05f, 0.5f, t);
            }
            else
            {
                float ratioOfHalf = satisfactionRatio / 0.5f;
                efficiency = 0.05f * ratioOfHalf * ratioOfHalf * ratioOfHalf;
            }
            _networkEfficiency = Mathf.Max(0.01f, efficiency);

            if (PowerTickManager.Instance != null && PowerTickManager.Instance.CurrentTickCount % 10 == 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append($"[EnergyNetwork Deficit] Efficiency: {_networkEfficiency:F2} | Supply: {totalProvidedToMachines:F2} / {machineDemand:F2} | Demanding: ");
                foreach (var m in machines)
                {
                    if (m is MonoBehaviour mb)
                    {
                        float missing = m.MaxStorage - m.CurrentEnergy;
                        float pull = Mathf.Min(m.InputTransferSpeed, missing);
                        sb.Append($"{mb.gameObject.name} ({pull:F2}), ");
                    }
                }
                Debug.Log(sb.ToString());
            }

            foreach (var machine in machines)
            {
                if (machine == null || (machine is UnityEngine.Object obj && obj == null)) continue;
                float missing = machine.MaxStorage - machine.CurrentEnergy;
                float pull = Mathf.Min(machine.InputTransferSpeed, missing);
                float allocated = pull * satisfactionRatio;
                machine.EnergyAllocationRate += allocated / tickRate;
            }
        }
        else
        {
            _networkEfficiency = 1f;
            // Allocate full demand
            foreach (var machine in machines)
            {
                if (machine == null || (machine is UnityEngine.Object obj && obj == null)) continue;
                float missing = machine.MaxStorage - machine.CurrentEnergy;
                float pull = Mathf.Min(machine.InputTransferSpeed, missing);
                machine.EnergyAllocationRate += pull / tickRate;
            }
        }

        // 7. Finalize Generator Allocations (Pro-rata based on what was actually drawn from them)
        float totalSourceConsumed = sourceSupply - remainingSource;
        float generatorRatio = sourceSupply > 0 ? totalSourceConsumed / sourceSupply : 0f;
        foreach (var gen in generators)
        {
            if (gen == null || (gen is UnityEngine.Object obj && obj == null)) continue;
            float pushable = Mathf.Min(gen.OutputTransferSpeed, gen.CurrentEnergy);
            gen.EnergyAllocationRate -= (pushable * generatorRatio) / tickRate;
        }

        // LogNetworkSummary(sourceSupply, machineDemand, machineRatio);
    }

    /// <summary>
    /// Applies the calculated allocation fluidly over the frame.
    /// Executed in FixedUpdate by EnergyManager.
    /// </summary>
    public void ProcessFluidTransfer(float deltaTime)
    {
        foreach (IEnergyNode node in _nodes)
        {
            if (node == null || (node as UnityEngine.Object) == null) continue;

            if (Mathf.Abs(node.EnergyAllocationRate) > 0.0001f)
            {
                float deltaEnergy = node.EnergyAllocationRate * deltaTime;
                node.CurrentEnergy = Quantize(Mathf.Clamp(node.CurrentEnergy + deltaEnergy, 0f, node.MaxStorage));
            }
        }
    }

    public float GetNetworkEfficiency()
    {
        return _networkEfficiency;
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
