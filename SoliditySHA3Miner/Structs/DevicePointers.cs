using System;
using System.Runtime.InteropServices;

namespace SoliditySHA3Miner.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DevicePointers
    {
        public IntPtr High64Target;

        public IntPtr Target;

        public IntPtr Challenge;

        public IntPtr MidState;

        public IntPtr Message;
        
        public IntPtr SolutionCount;

        public IntPtr SolutionCountDevice;

        public IntPtr Solutions;

        public IntPtr SolutionsDevice;
    }
}