using System;

namespace SoliditySHA3Miner.Miner
{
    public interface IMiner : IDisposable
    {
        NetworkInterface.INetworkInterface NetworkInterface { get; }
        Device[] Devices { get; }

        bool HasAssignedDevices { get; }
        bool HasMonitoringAPI { get; }
        bool IsAnyInitialised { get; }
        bool IsMining { get; }
        bool IsPause { get; }

        void StartMining(int networkUpdateInterval, int hashratePrintInterval);

        void StopMining();

        ulong GetTotalHashrate();

        ulong GetHashrateByDevice(string platformName, int deviceID);
    }
}