using System.Runtime.InteropServices;

namespace SoliditySHA3Miner.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Dim3
    {
        public uint X;
        public uint Y;
        public uint Z;
    }
}