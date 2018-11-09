using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SoliditySHA3Miner.Miner
{
    public class CUDA : MinerBase
    {
        private Helper.CUDA.Solver.MessageCallback m_messageCallback;

        public CUDA(NetworkInterface.INetworkInterface networkInterface, Device[] cudaDevices, bool isSubmitStale, int pauseOnFailedScans)
            : base(networkInterface, cudaDevices, isSubmitStale, pauseOnFailedScans)
        {
            try
            {
                var hasNvAPI64 = false;
                Helper.CUDA.Solver.FoundNvAPI64(ref hasNvAPI64);

                if (!hasNvAPI64)
                    PrintMessage("CUDA", -1, "Warn", "NvAPI64 library not found.");

                var foundNvSMI = API.NvSMI.FoundNvSMI();

                if (!foundNvSMI)
                    PrintMessage("CUDA", -1, "Warn", "NvSMI library not found.");

                UseNvSMI = !hasNvAPI64 && foundNvSMI;

                HasMonitoringAPI = hasNvAPI64 | UseNvSMI;

                if (!HasMonitoringAPI)
                    PrintMessage("CUDA", -1, "Warn", "GPU monitoring not available.");

                UnmanagedInstance = Helper.CUDA.Solver.GetInstance();
                m_messageCallback = Helper.CUDA.Solver.SetOnMessageHandler(UnmanagedInstance, m_instance_OnMessage);

                if (!Program.AllowCUDA || cudaDevices.All(d => !d.AllowDevice))
                {
                    PrintMessage("CUDA", -1, "Info", "Device not set.");
                    return;
                }                
                AssignDevices();
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        #region IMiner

        public override void Dispose()
        {
            var disposeTask = Task.Factory.StartNew(() => base.Dispose());
            try
            {
                if (UnmanagedInstance != IntPtr.Zero)
                    Helper.CUDA.Solver.DisposeInstance(UnmanagedInstance);

                m_messageCallback = null;
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
            disposeTask.Wait();
        }

        #endregion IMiner

        #region MinerBase abstracts

        protected override void HashPrintTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var hashString = new StringBuilder();
            hashString.Append("Hashrates:");

            foreach (var device in Devices.Where(d => d.AllowDevice))
                hashString.AppendFormat(" {0} MH/s", GetHashRateByDevice(device) / 1000000.0f);

            PrintMessage("CUDA", -1, "Info", hashString.ToString());

            if (HasMonitoringAPI)
            {
                var coreClock = 0;
                var temperature = 0;
                var tachometerRPM = 0;
                var coreClockString = new StringBuilder();
                var temperatureString = new StringBuilder();
                var fanTachometerRpmString = new StringBuilder();

                coreClockString.Append("Core clocks:");
                foreach (var device in Devices)
                    if (device.AllowDevice)
                    {
                        if (UseNvSMI)
                            coreClock = API.NvSMI.GetDeviceCurrentCoreClock(device.PciBusID);
                        else
                            Helper.CUDA.Solver.GetDeviceCurrentCoreClock(UnmanagedInstance, device.DeviceID, ref coreClock);
                        coreClockString.AppendFormat(" {0}MHz", coreClock);
                    }
                PrintMessage("CUDA", -1, "Info", coreClockString.ToString());

                temperatureString.Append("Temperatures:");
                foreach (var device in Devices)
                    if (device.AllowDevice)
                    {
                        if (UseNvSMI)
                            temperature = API.NvSMI.GetDeviceCurrentTemperature(device.PciBusID);
                        else
                            Helper.CUDA.Solver.GetDeviceCurrentTemperature(UnmanagedInstance, device.DeviceID, ref temperature);
                        temperatureString.AppendFormat(" {0}C", temperature);
                    }
                PrintMessage("CUDA", -1, "Info", temperatureString.ToString());

                if (!UseNvSMI)
                {
                    fanTachometerRpmString.Append("Fan tachometers:");
                    foreach (var device in Devices)
                        if (device.AllowDevice)
                        {
                            Helper.CUDA.Solver.GetDeviceCurrentFanTachometerRPM(UnmanagedInstance, device.DeviceID, ref tachometerRPM);
                            fanTachometerRpmString.AppendFormat(" {0}RPM", tachometerRPM);
                        }
                    PrintMessage("CUDA", -1, "Info", fanTachometerRpmString.ToString());
                }
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
        }

        protected override void AssignDevices()
        {
            foreach (var device in Devices.Where(d => d.AllowDevice))
            {
                PrintMessage("CUDA", device.DeviceID, "Info", "Assigning device...");

                var isKingMaking = !string.IsNullOrWhiteSpace(Work.GetKingAddressString());

                var deviceName = new StringBuilder(device.Name);
                int computeMajor = 0, computeMinor = 0;
                var errorMessage = new StringBuilder(1024);

                Helper.CUDA.Solver.GetDeviceProperties(UnmanagedInstance,
                                                       device.DeviceID,
                                                       ref device.PciBusID,
                                                       deviceName,
                                                       ref computeMajor,
                                                       ref computeMinor,
                                                       errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    return;
                }

                var deviceNameString = deviceName.ToString();
                device.ConputeVersion = (uint)((computeMajor * 100) + (computeMinor * 10));

                if (device.ConputeVersion < 500)
                    device.Intensity = (device.Intensity < 1.000f) ? Device.DEFAULT_INTENSITY : device.Intensity; // For older GPUs
                else
                {
                    float defaultIntensity = Device.DEFAULT_INTENSITY;

                    if (isKingMaking)
                    {
                        if (new string[] { "2080", "2070", "1080" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 27.54f;

                        else if (new string[] { "2060", "1070 TI", "1070TI" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 27.46f;

                        else if (new string[] { "2050", "1070", "980" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 27.01f;

                        else if (new string[] { "1060", "970" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 26.01f;

                        else if (new string[] { "1050", "960" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 25.01f;
                    }
                    else
                    {
                        if (new string[] { "2080", "2070 TI", "2070TI", "1080 TI", "1080TI" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 27.00f;

                        else if (new string[] { "1080", "2070", "1070 TI", "1070TI" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 26.33f;

                        else if (new string[] { "2060", "1070", "980" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 26.00f;

                        else if (new string[] { "2050", "1060", "970" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 25.50f;

                        else if (new string[] { "1050", "960" }.Any(m => deviceNameString.IndexOf(m) > -1))
                            defaultIntensity = 25.00f;
                    }
                    device.Intensity = (device.Intensity < 1.000f) ? defaultIntensity : device.Intensity;
                }

                device.Name = deviceNameString;
                device.ConputeVersion = (uint)(Math.Abs((computeMajor) * 100) + Math.Abs(computeMinor * 10));
                device.IsAssigned = true;

                PrintMessage("CUDA", device.DeviceID, "Info", string.Format("Assigned CUDA device ({0})...", deviceName));
                PrintMessage("CUDA", device.DeviceID, "Info", string.Format("Compute capability: {0}.{1}", computeMajor, computeMinor));
                PrintMessage("CUDA", device.DeviceID, "Info", string.Format("Intensity: {0}", device.Intensity));

                if (!device.IsInitialized)
                {
                    PrintMessage("CUDA", device.DeviceID, "Info", "Initializing device...");
                    errorMessage.Clear();

                    Helper.CUDA.Solver.InitializeDevice(UnmanagedInstance,
                                                        device.DeviceID,
                                                        device.PciBusID,
                                                        Device.MAX_SOLUTION_COUNT,
                                                        ref device.Pointers.Solutions,
                                                        ref device.Pointers.SolutionCount,
                                                        ref device.Pointers.SolutionsDevice,
                                                        ref device.Pointers.SolutionCountDevice,
                                                        errorMessage);
                    if (errorMessage.Length > 0)
                        PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    else
                        device.IsInitialized = true;
                }
            }
        }

        protected override void PushHigh64Target(Device device)
        {
            var errorMessage = new StringBuilder(1024);
            Helper.CUDA.Solver.PushHigh64Target(UnmanagedInstance, device.Pointers.High64Target, errorMessage);

            if (errorMessage.Length > 0)
                PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
        }

        protected override void PushTarget(Device device)
        {
            var errorMessage = new StringBuilder(1024);
            Helper.CUDA.Solver.PushTarget(UnmanagedInstance, device.Pointers.Target, errorMessage);

            if (errorMessage.Length > 0)
                PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
        }

        protected override void PushMidState(Device device)
        {
            var errorMessage = new StringBuilder(1024);
            Helper.CUDA.Solver.PushMidState(UnmanagedInstance, device.Pointers.MidState, errorMessage);

            if (errorMessage.Length > 0)
                PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
        }

        protected override void PushMessage(Device device)
        {
            var errorMessage = new StringBuilder(1024);
            Helper.CUDA.Solver.PushMessage(UnmanagedInstance, device.Pointers.Message, errorMessage);

            if (errorMessage.Length > 0)
                PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
        }

        protected async override void StartFinding(Device device)
        {
            try
            {
                if (!device.IsInitialized) return;

                while (!device.HasNewTarget || !device.HasNewChallenge)
                    await Task.Delay(500);

                PrintMessage("CUDA", device.DeviceID, "Info", "Start mining...");

                PrintMessage("CUDA", device.DeviceID, "Debug",
                             string.Format("Threads: {0} Grid size: {1} Block size: {2}",
                                           device.Threads, device.Grid.X, device.Block.X));

                var grid = device.Grid;
                var block = device.Block;
                var maxSolutionCount = Device.MAX_SOLUTION_COUNT;
                var workPosition = 0ul;
                var errorMessage = new StringBuilder(1024);
                var currentChallenge = (byte[])Array.CreateInstance(typeof(byte), UINT256_LENGTH);

                Helper.CUDA.Solver.SetDevice(UnmanagedInstance, device.DeviceID, errorMessage);

                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    return;
                }

                // reduce excessive high hashrate reporting
                device.HashStartTime = DateTime.Now.AddMilliseconds(-500);
                device.HashCount = 0;
                device.IsMining = true;

                unsafe
                {
                    ulong* solutions = (ulong*)device.Pointers.Solutions.ToPointer();
                    uint* solutionCount = (uint*)device.Pointers.SolutionCount.ToPointer();
                    do
                    {
                        while (device.IsPause)
                        {
                            Task.Delay(500).Wait();
                            // reduce excessive high hashrate reporting
                            device.HashStartTime = DateTime.Now.AddMilliseconds(-500);
                            device.HashCount = 0;
                        }

                        CheckInputs(device, false, ref currentChallenge);

                        Work.IncrementPosition(ref workPosition, device.Threads);
                        device.HashCount += device.Threads;

                        Helper.CUDA.Solver.HashMidState(UnmanagedInstance,
                                                        ref grid,
                                                        ref block,
                                                        device.Pointers.SolutionsDevice,
                                                        device.Pointers.SolutionCountDevice,
                                                        ref maxSolutionCount,
                                                        ref workPosition,
                                                        errorMessage);
                        if (errorMessage.Length > 0)
                        {
                            PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                            device.IsMining = false;
                        }

                        if (*solutionCount > 0u)
                        {
                            var solutionArray = (ulong[])Array.CreateInstance(typeof(ulong), *solutionCount);

                            for (var i = 0; i < *solutionCount; i++)
                                solutionArray[i] = solutions[i];

                            SubmitSolutions(solutionArray, currentChallenge, "CUDA", device.DeviceID, *solutionCount, false);

                            *solutionCount = 0;
                        }
                    } while (device.IsMining);
                }

                PrintMessage("CUDA", device.DeviceID, "Info", "Stop mining...");

                device.HashCount = 0;

                Helper.CUDA.Solver.FreeObject(UnmanagedInstance, device.DeviceID, device.Pointers.Solutions, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                Helper.CUDA.Solver.FreeObject(UnmanagedInstance, device.DeviceID, device.Pointers.SolutionCount, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                Helper.CUDA.Solver.ResetDevice(UnmanagedInstance, device.DeviceID, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                device.IsInitialized = false;
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
            PrintMessage("CUDA", device.DeviceID, "Info", "Mining stopped.");
        }

        protected override async void StartFindingKing(Device device)
        {
            try
            {
                if (!device.IsInitialized) return;

                while (!device.HasNewTarget || !device.HasNewChallenge)
                    await Task.Delay(500);

                PrintMessage("CUDA", device.DeviceID, "Info", "Start mining...");

                PrintMessage("CUDA", device.DeviceID, "Debug",
                             string.Format("Threads: {0} Grid size: {1} Block size: {2}",
                                           device.Threads, device.Grid.X, device.Block.X));

                var grid = device.Grid;
                var block = device.Block;
                var maxSolutionCount = Device.MAX_SOLUTION_COUNT;
                var workPosition = 0ul;
                var errorMessage = new StringBuilder(1024);
                var currentChallenge = (byte[])Array.CreateInstance(typeof(byte), UINT256_LENGTH);

                Helper.CUDA.Solver.SetDevice(UnmanagedInstance, device.DeviceID, errorMessage);

                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    return;
                }

                // reduce excessive high hashrate reporting
                device.HashStartTime = DateTime.Now.AddMilliseconds(-500);
                device.HashCount = 0;
                device.IsMining = true;

                unsafe
                {
                    ulong* solutions = (ulong*)device.Pointers.Solutions.ToPointer();
                    uint* solutionCount = (uint*)device.Pointers.SolutionCount.ToPointer();
                    do
                    {
                        while (device.IsPause)
                        {
                            Task.Delay(500).Wait();
                            // reduce excessive high hashrate reporting
                            device.HashStartTime = DateTime.Now.AddMilliseconds(-500);
                            device.HashCount = 0;
                        }

                        CheckInputs(device, true, ref currentChallenge);

                        Work.IncrementPosition(ref workPosition, device.Threads);
                        device.HashCount += device.Threads;

                        Helper.CUDA.Solver.HashMessage(UnmanagedInstance,
                                                       ref grid,
                                                       ref block,
                                                       device.Pointers.SolutionsDevice,
                                                       device.Pointers.SolutionCountDevice,
                                                       ref maxSolutionCount,
                                                       ref workPosition,
                                                       errorMessage);
                        if (errorMessage.Length > 0)
                        {
                            PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                            device.IsMining = false;
                        }

                        if (*solutionCount > 0u)
                        {
                            var solutionArray = (ulong[])Array.CreateInstance(typeof(ulong), *solutionCount);

                            for (var i = 0; i < *solutionCount; i++)
                                solutionArray[i] = solutions[i];

                            SubmitSolutions(solutionArray, currentChallenge, "CUDA", device.DeviceID, *solutionCount, true);

                            *solutionCount = 0;
                        }
                    } while (device.IsMining);
                }

                PrintMessage("CUDA", device.DeviceID, "Info", "Stop mining...");

                device.HashCount = 0;

                Helper.CUDA.Solver.FreeObject(UnmanagedInstance, device.DeviceID, device.Pointers.Solutions, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                Helper.CUDA.Solver.FreeObject(UnmanagedInstance, device.DeviceID, device.Pointers.SolutionCount, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                Helper.CUDA.Solver.ResetDevice(UnmanagedInstance, device.DeviceID, errorMessage);
                if (errorMessage.Length > 0)
                {
                    PrintMessage("CUDA", device.DeviceID, "Error", errorMessage.ToString());
                    errorMessage.Clear();
                }

                device.IsInitialized = false;
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
            PrintMessage("CUDA", device.DeviceID, "Info", "Mining stopped.");
        }

        #endregion

        private void m_instance_OnMessage(int deviceID, StringBuilder type, StringBuilder message)
        {
            PrintMessage("CUDA", deviceID, type.ToString(), message.ToString());
        }

        protected override ulong GetHashRateByDevice(Device device)
        {
            var hashRate = 0ul;

            if (!IsPause)
                hashRate = (ulong)(device.HashCount / (DateTime.Now - device.HashStartTime).TotalSeconds);

            return hashRate;
        }
    }
}