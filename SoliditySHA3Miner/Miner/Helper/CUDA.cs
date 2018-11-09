using SoliditySHA3Miner.Structs;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SoliditySHA3Miner.Miner.Helper
{
    public static class CUDA
    {
        #region P/Invoke interface

        public static class Solver
        {
            public const string SOLVER_NAME = "CudaSoliditySHA3Solver";

            public delegate void MessageCallback([In]int deviceID, [In]StringBuilder type, [In]StringBuilder message);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern MessageCallback SetOnMessageHandler(IntPtr instance, MessageCallback messageCallback);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void FoundNvAPI64(ref bool hasNvAPI64);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCount(ref int deviceCount, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceName(int deviceID, StringBuilder deviceName, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr GetInstance();

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void DisposeInstance(IntPtr instance);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceProperties(IntPtr instance, int deviceID,
                                                          ref uint pciBusID, StringBuilder deviceName,
                                                          ref int computeMajor, ref int computeMinor,
                                                          StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void InitializeDevice(IntPtr instance, int deviceID, uint pciBusID, uint maxSolutionCount,
							                            ref IntPtr solutions, ref IntPtr solutionCount,
							                            ref IntPtr solutionsDevice, ref IntPtr solutionCountDevice,
							                            StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void SetDevice(IntPtr instance, int deviceID, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void ResetDevice(IntPtr instance, int deviceID, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void FreeObject(IntPtr instance, int deviceID, IntPtr objectPointer, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void PushHigh64Target(IntPtr instance, IntPtr high64Target, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void PushMidState(IntPtr instance, IntPtr midState, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void PushTarget(IntPtr instance, IntPtr target, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void PushMessage(IntPtr instance, IntPtr message, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void HashMidState(IntPtr instance, ref Dim3 grid, ref Dim3 block,
                                                   IntPtr solutionsDevice, IntPtr solutionCountDevice,
                                                   ref uint maxSolutionCount, ref ulong workPosition, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void HashMessage(IntPtr instance, ref Dim3 grid, ref Dim3 block,
                                                  IntPtr solutionsDevice, IntPtr solutionCountDevice,
                                                  ref uint maxSolutionCount, ref ulong workPosition, StringBuilder errorMessage);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceSettingMaxCoreClock(IntPtr instance, int deviceID, ref int coreClock);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceSettingMaxMemoryClock(IntPtr instance, int deviceID, ref int memoryClock);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceSettingPowerLimit(IntPtr instance, int deviceID, ref int powerLimit);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceSettingThermalLimit(IntPtr instance, int deviceID, ref int thermalLimit);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceSettingFanLevelPercent(IntPtr instance, int deviceID, ref int fanLevel);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentFanTachometerRPM(IntPtr instance, int deviceID, ref int tachometerRPM);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentTemperature(IntPtr instance, int deviceID, ref int temperature);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentCoreClock(IntPtr instance, int deviceID, ref int coreClock);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentMemoryClock(IntPtr instance, int deviceID, ref int memoryClock);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentUtilizationPercent(IntPtr instance, int deviceID, ref int utilization);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentPstate(IntPtr instance, int deviceID, ref int pState);

            [DllImport(SOLVER_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern void GetDeviceCurrentThrottleReasons(IntPtr instance, int deviceID, StringBuilder reasons, ref ulong reasonSize);
        }

        #endregion
    }
}