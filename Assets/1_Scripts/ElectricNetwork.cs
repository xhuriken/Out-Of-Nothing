using System.Collections.Generic;

public class ElectricNetwork
{
    public List<IEnergyStorage> Storages = new();
    public List<IEnergyConsumer> Consumers = new();
    public List<IEnergyRelay> Relays = new();
}