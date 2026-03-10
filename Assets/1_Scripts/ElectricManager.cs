
using System.Collections.Generic;
using UnityEngine;

public class ElectricManager : MonoBehaviour
{
    public static ElectricManager Instance;

    private List<IEnergyStorage> _storages = new();
    private List<IEnergyConsumer> _consumers = new();
    private List<IEnergyRelay> _relays = new();

    private List<ElectricNetwork> _networks = new();

    private bool _dirty;

    void Awake()
    {
        Instance = this;
    }

    public void Register(object obj)
    {
        if (obj is IEnergyStorage storage)
            _storages.Add(storage);
       

        if (obj is IEnergyConsumer consumer)
            _consumers.Add(consumer);
      

        if (obj is IEnergyRelay relay)
            _relays.Add(relay);
        

        _dirty = true;
        Debug.Log($"Registered electric object : {obj}");

    }

    void Update()
    {
        if (_dirty)
        {
            RebuildNetworks();
            _dirty = false;
        }

        DistributeEnergy();
    }

    void RebuildNetworks()
    {
        _networks.Clear();

        HashSet<IEnergyRelay> visited = new();

        foreach (var relay in _relays)
        {
            if (visited.Contains(relay))
                continue;

            ElectricNetwork network = new ElectricNetwork();

            Queue<IEnergyRelay> queue = new();
            queue.Enqueue(relay);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                network.Relays.Add(current);

                foreach (var other in _relays)
                {
                    if (Vector2.Distance(current.Position, other.Position) < current.Range)
                    {
                        queue.Enqueue(other);
                    }
                }
            }

            _networks.Add(network);

            Debug.Log($"Electric networks rebuilt : {_networks.Count}");

            Debug.Log(
                    $"Network created → Relays:{network.Relays.Count} | Storages:{network.Storages.Count} | Consumers:{network.Consumers.Count}"
                      );
        }
    }

    void DistributeEnergy()
    {
        foreach (var network in _networks)
        {
            float availableEnergy = 0;

            foreach (var storage in network.Storages)
                availableEnergy += storage.CurrentEnergy;

            foreach (var consumer in network.Consumers)
            {
                if (availableEnergy <= 0)
                    break;

                float need = consumer.EnergyNeeded;

                float given = Mathf.Min(need, availableEnergy);

                consumer.ReceiveEnergy(given);

                availableEnergy -= given;
                Debug.Log($"Consumer needs {need} energy");

            }
        }

    }

    public void MarkDirty()
    {
        _dirty = true;
    }
}