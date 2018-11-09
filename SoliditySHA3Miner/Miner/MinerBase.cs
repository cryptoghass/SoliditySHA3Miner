using Nethereum.Hex.HexTypes;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SoliditySHA3Miner.Miner
{
    public abstract class MinerBase : IMiner
    {
        public const int UINT32_LENGTH = 4;
        public const int UINT64_LENGTH = 8;
        public const int SPONGE_LENGTH = 200;
        public const int ADDRESS_LENGTH = 20;
        public const int UINT256_LENGTH = 32;
        public const int MESSAGE_LENGTH = UINT256_LENGTH + ADDRESS_LENGTH + UINT256_LENGTH;

        private static readonly object m_submissionQueueLock = new object();

        protected Timer m_hashPrintTimer;
        protected int m_pauseOnFailedScan;
        protected int m_failedScanCount;
        protected bool m_isCurrentChallengeStopSolving;
        protected bool m_isSubmitStale;

        protected string m_TargetString;
        protected string m_ChallengeString;
        protected string m_AddressString;

        protected HexBigInteger m_TargetBigInteger;
        protected ulong m_High64Target;

        protected byte[] m_ChallengeBytes;
        protected byte[] m_AddressBytes;
        protected byte[] m_SolutionTemplateBytes;
        protected byte[] m_MidStateBytes;

        protected IntPtr m_ChallengeBytesPointer;
        protected IntPtr m_AddressBytesPointer;
        protected IntPtr m_SolutionTemplateBytesPointer;
        protected IntPtr m_MidStateBytesPointer;

        protected string Target
        {
            get => m_TargetString;
            set
            {
                m_TargetString = value;
                m_TargetBigInteger = new HexBigInteger(value);
                m_High64Target = ulong.Parse(value.Replace("0x", string.Empty).Substring(0, UINT64_LENGTH * 2), NumberStyles.HexNumber);
            }
        }

        protected string Challenge
        {
            get => m_ChallengeString;
            set
            {
                m_ChallengeString = value;
                Utils.Numerics.HexStringToByte32Array(value, ref m_ChallengeBytes);
            }
        }

        protected string Address
        {
            get => m_AddressString;
            set
            {
                m_AddressString = value;
                Utils.Numerics.AddressStringToByte20Array(value, ref m_AddressBytes);
            }
        }

        public IntPtr UnmanagedInstance { get; protected set; }
        
        #region IMiner

        public NetworkInterface.INetworkInterface NetworkInterface { get; protected set; }

        public Device[] Devices { get; }

        public bool HasAssignedDevices => Devices?.Any(d => d.IsAssigned) ?? false;

        public bool HasMonitoringAPI { get; protected set; }

        public bool UseNvSMI { get; protected set; }

        public bool IsAnyInitialised => Devices?.Any(d => d.IsInitialized) ?? false;

        public bool IsMining => Devices?.Any(d => d.IsMining) ?? false;

        public bool IsPause => Devices?.Any(d => d.IsPause) ?? false;

        public void StartMining(int networkUpdateInterval, int hashratePrintInterval)
        {
            try
            {
                NetworkInterface.ResetEffectiveHashrate();
                NetworkInterface.UpdateMiningParameters();

                m_hashPrintTimer = new Timer(hashratePrintInterval);
                m_hashPrintTimer.Elapsed += HashPrintTimer_Elapsed;
                m_hashPrintTimer.Start();

                var isKingMaking = !string.IsNullOrWhiteSpace(Work.GetKingAddressString());
                StartFindingAll(isKingMaking);
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
                StopMining();
            }
        }

        public void StopMining()
        {
            try
            {
                m_hashPrintTimer.Stop();

                NetworkInterface.ResetEffectiveHashrate();

                foreach (var device in Devices)
                    device.IsMining =  false;
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        public ulong GetHashrateByDevice(string platformName, int deviceID)
        {
            return GetHashRateByDevice(Devices.First(d => d.DeviceID == deviceID));
        }

        protected void NetworkInterface_OnGetTotalHashrate(NetworkInterface.INetworkInterface sender, ref ulong totalHashrate)
        {
            totalHashrate = GetTotalHashrate();
        }

        public ulong GetTotalHashrate()
        {
            var totalHashrate = 0ul;
            try
            {
                foreach (var device in Devices)
                    totalHashrate += GetHashRateByDevice(device);
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
            return totalHashrate;
        }

        public virtual void Dispose()
        {
            NetworkInterface.OnGetTotalHashrate -= NetworkInterface_OnGetTotalHashrate;
            NetworkInterface.OnGetMiningParameterStatus -= NetworkInterface_OnGetMiningParameterStatus;
            NetworkInterface.OnNewChallenge -= NetworkInterface_OnNewChallenge;
            NetworkInterface.OnNewTarget -= NetworkInterface_OnNewTarget;
            NetworkInterface.OnStopSolvingCurrentChallenge -= NetworkInterface_OnStopSolvingCurrentChallenge;

            if (m_hashPrintTimer != null)
            {
                m_hashPrintTimer.Elapsed -= HashPrintTimer_Elapsed;
                m_hashPrintTimer.Dispose();
            }
            try
            {
                if (Devices != null)
                    Devices.AsParallel().
                            ForAll(d => d.Dispose());
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
            try
            {
                NetworkInterface.Dispose();
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        #endregion IMiner
        
        protected abstract void HashPrintTimer_Elapsed(object sender, ElapsedEventArgs e);
        protected abstract void AssignDevices();
        protected abstract void PushHigh64Target(Device device);
        protected abstract void PushTarget(Device device);
        protected abstract void PushMidState(Device device);
        protected abstract void PushMessage(Device device);
        protected abstract void StartFinding(Device device);
        protected abstract void StartFindingKing(Device device);
        protected abstract ulong GetHashRateByDevice(Device device);

        public MinerBase(NetworkInterface.INetworkInterface networkInterface, Device[] devices, bool isSubmitStale, int pauseOnFailedScans)
        {
            m_failedScanCount = 0;
            m_pauseOnFailedScan = pauseOnFailedScans;
            m_isSubmitStale = isSubmitStale;
            NetworkInterface = networkInterface;
            Devices = devices;

            m_ChallengeBytes = (byte[])Array.CreateInstance(typeof(byte), UINT256_LENGTH);
            m_ChallengeBytesPointer = GCHandle.Alloc(m_ChallengeBytes, GCHandleType.Pinned).AddrOfPinnedObject();

            m_AddressBytes = (byte[])Array.CreateInstance(typeof(byte), ADDRESS_LENGTH);
            m_AddressBytesPointer = GCHandle.Alloc(m_AddressBytes, GCHandleType.Pinned).AddrOfPinnedObject();

            NetworkInterface.OnGetTotalHashrate += NetworkInterface_OnGetTotalHashrate;
            NetworkInterface.OnGetMiningParameterStatus += NetworkInterface_OnGetMiningParameterStatus;
            NetworkInterface.OnNewChallenge += NetworkInterface_OnNewChallenge;
            NetworkInterface.OnNewTarget += NetworkInterface_OnNewTarget;
            NetworkInterface.OnStopSolvingCurrentChallenge += NetworkInterface_OnStopSolvingCurrentChallenge;
        }

        protected void PrintMessage(string platform, int deviceEnum, string type, string message)
        {
            var sFormat = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(platform)) sFormat.Append(platform + " ");
            if (deviceEnum > -1) sFormat.Append("ID: {0} ");

            switch (type.ToUpperInvariant())
            {
                case "INFO":
                    sFormat.Append(deviceEnum > -1 ? "[INFO] {1}" : "[INFO] {0}");
                    break;

                case "WARN":
                    sFormat.Append(deviceEnum > -1 ? "[WARN] {1}" : "[WARN] {0}");
                    break;

                case "ERROR":
                    sFormat.Append(deviceEnum > -1 ? "[ERROR] {1}" : "[ERROR] {0}");
                    break;

                case "DEBUG":
                default:
#if DEBUG
                    sFormat.Append(deviceEnum > -1 ? "[DEBUG] {1}" : "[DEBUG] {0}");
                    break;
#else
                    return;
#endif
            }
            Program.Print(deviceEnum > -1
                ? string.Format(sFormat.ToString(), deviceEnum, message)
                : string.Format(sFormat.ToString(), message));
        }

        private void NetworkInterface_OnGetMiningParameterStatus(NetworkInterface.INetworkInterface sender, bool success)
        {
            try
            {
                if (UnmanagedInstance != null && UnmanagedInstance.ToInt64() != 0)
                {
                    if (success)
                    {
                        var isPause = Devices.All(d => d.IsPause);

                        if (m_isCurrentChallengeStopSolving) { isPause = true; }
                        else if (isPause)
                        {
                            if (m_failedScanCount > m_pauseOnFailedScan)
                                m_failedScanCount = 0;

                            isPause = false;
                        }
                        foreach (var device in Devices)
                            device.IsPause = IsPause;
                    }
                    else
                    {
                        m_failedScanCount += 1;

                        var isMining = Devices.Any(d => d.IsMining);

                        if (m_failedScanCount > m_pauseOnFailedScan && IsMining)
                            foreach (var device in Devices)
                                device.IsPause = true;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        private void NetworkInterface_OnNewChallenge(NetworkInterface.INetworkInterface sender, string messagePrefix)
        {
            try
            {
                if (UnmanagedInstance != null && UnmanagedInstance.ToInt64() != 0)
                {
                    Challenge = messagePrefix.Replace("0x", string.Empty).Substring(0, UINT256_LENGTH * 2);
                    Address = messagePrefix.Replace("0x", string.Empty).Substring(UINT256_LENGTH * 2, ADDRESS_LENGTH * 2);

                    if (m_SolutionTemplateBytes == null)
                    {
                        m_SolutionTemplateBytes = Work.SolutionTemplate;
                        m_SolutionTemplateBytesPointer = GCHandle.Alloc(m_SolutionTemplateBytes, GCHandleType.Pinned).AddrOfPinnedObject();
                    }

                    if (m_MidStateBytes == null)
                    {
                        m_MidStateBytes = (byte[])Array.CreateInstance(typeof(byte), SPONGE_LENGTH);
                        m_MidStateBytesPointer = GCHandle.Alloc(m_MidStateBytes, GCHandleType.Pinned).AddrOfPinnedObject();
                    }

                    m_MidStateBytes = Helper.CPU.GetMidState(m_ChallengeBytes, m_AddressBytes, m_SolutionTemplateBytes);

                    foreach (var device in Devices)
                    {
                        Array.ConstrainedCopy(m_ChallengeBytes, 0, device.Message, 0, UINT256_LENGTH);
                        Array.ConstrainedCopy(m_AddressBytes, 0, device.Message, UINT256_LENGTH, ADDRESS_LENGTH);
                        Array.ConstrainedCopy(m_SolutionTemplateBytes, 0, device.Message, UINT256_LENGTH + ADDRESS_LENGTH, UINT256_LENGTH);

                        Array.Copy(m_ChallengeBytes, device.Challenge, UINT256_LENGTH);
                        Array.Copy(m_MidStateBytes, device.MidState, SPONGE_LENGTH);                        
                        device.HasNewChallenge = true;
                    }

                    if (m_isCurrentChallengeStopSolving)
                    {
                        foreach (var device in Devices)
                            device.IsPause = false;

                        m_isCurrentChallengeStopSolving = false;
                    }
                }
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        private void NetworkInterface_OnNewTarget(NetworkInterface.INetworkInterface sender, string target)
        {
            try
            {
                Target = target.ToString();
                var targetBytes = m_TargetBigInteger.ToHexByteArray();

                foreach (var device in Devices)
                {
                    Array.Copy(targetBytes, device.Target, UINT256_LENGTH);
                    device.High64Target[0] = m_High64Target;
                    device.HasNewTarget = true;
                }
            }
            catch (Exception ex)
            {
                PrintMessage("CUDA", -1, "Error", ex.Message);
            }
        }

        private void NetworkInterface_OnStopSolvingCurrentChallenge(NetworkInterface.INetworkInterface sender)
        {
            if (m_isCurrentChallengeStopSolving) return;
            
            m_isCurrentChallengeStopSolving = true;

            PrintMessage("CUDA", -1, "Info", "Mining temporary paused until new challenge receive...");

            foreach (var device in Devices)
                device.IsPause = true;
        }

        protected void m_instance_OnGetHigh64Target(ref ulong high64Target)
        {
            high64Target = m_High64Target;
        }

        protected void m_instance_OnGetCurrentChallenge(ref IntPtr currentChallenge)
        {
            currentChallenge = m_ChallengeBytesPointer;
        }

        protected void m_instance_OnGetCurrentMidState(ref IntPtr currentMidState)
        {
            currentMidState = m_MidStateBytesPointer;
        }

        private void StartFindingAll(bool isKingMaking)
        {
            foreach (var device in Devices)
            {
                if (isKingMaking) StartFindingKing(device);
                else StartFinding(device);
            }
        }

        protected void CheckInputs(Device device, bool isKingMaking, ref byte[] currentChallenge)
        {
            if (device.HasNewTarget || device.HasNewChallenge)
            {
                if (device.HasNewTarget)
                {
                    if (isKingMaking) PushTarget(device);
                    else PushHigh64Target(device);

                    device.HasNewTarget = false;
                }

                if (device.HasNewChallenge)
                {
                    if (isKingMaking) PushMessage(device);
                    else PushMidState(device);

                    Array.Copy(device.Challenge, currentChallenge, UINT256_LENGTH);
                    device.HasNewChallenge = false;
                }

                // reduce hashrate spike on new challenge
                device.HashStartTime = DateTime.Now.AddMilliseconds(-500);
                device.HashCount = 0;
            }
        }

        protected void SubmitSolutions(ulong[] solutions, byte[] challenge, string platform, int deviceID, uint solutionCount, bool isKingMaking)
        {
            Task.Factory.StartNew(() =>
            {
                lock (m_submissionQueueLock)
                {
                    foreach (var solution in solutions)
                    {
                        var challengeString = "0x" + Utils.Numerics.Byte32ArrayToHexString(challenge);

                        if (!NetworkInterface.IsPool)
                            if (((NetworkInterface.Web3Interface)NetworkInterface).IsChallengedSubmitted(challengeString))
                            {
                                NetworkInterface_OnStopSolvingCurrentChallenge(null);
                                return;
                            }

                        if ((!m_isSubmitStale && !challenge.SequenceEqual(m_ChallengeBytes)) || solution == 0)
                            continue;
                        else if (m_isSubmitStale && !challenge.SequenceEqual(m_ChallengeBytes))
                            PrintMessage(platform, deviceID, "Warn", "GPU found stale solution, verifying...");
                        else
                            PrintMessage(platform, deviceID, "Info", "GPU found solution, verifying...");

                        var solutionBytes = BitConverter.GetBytes(solution);
                        var nonceByte = m_SolutionTemplateBytes.ToArray();
                        var messageBytes = (byte[])Array.CreateInstance(typeof(byte), UINT256_LENGTH + ADDRESS_LENGTH + UINT256_LENGTH);
                        var digestBytes = (byte[])Array.CreateInstance(typeof(byte), UINT256_LENGTH);
                        var messageHandle = GCHandle.Alloc(messageBytes, GCHandleType.Pinned);
                        var messagePointer = messageHandle.AddrOfPinnedObject();
                        var digestHandle = GCHandle.Alloc(digestBytes, GCHandleType.Pinned);
                        var digestPointer = digestHandle.AddrOfPinnedObject();

                        if (isKingMaking)
                            Array.ConstrainedCopy(solutionBytes, 0, nonceByte, ADDRESS_LENGTH, UINT64_LENGTH);
                        else
                            Array.ConstrainedCopy(solutionBytes, 0, nonceByte, (UINT256_LENGTH / 2) - (UINT64_LENGTH / 2), UINT64_LENGTH);

                        Array.ConstrainedCopy(challenge, 0, messageBytes, 0, UINT256_LENGTH);
                        Array.ConstrainedCopy(m_AddressBytes, 0, messageBytes, UINT256_LENGTH, ADDRESS_LENGTH);
                        Array.ConstrainedCopy(nonceByte, 0, messageBytes, UINT256_LENGTH + ADDRESS_LENGTH, UINT256_LENGTH);

                        Helper.CPU.Solver.SHA3(messagePointer, digestPointer);
                        messageHandle.Free();
                        digestHandle.Free();

                        var digestString = Utils.Numerics.Byte32ArrayToHexString(digestBytes);
                        var digest = new HexBigInteger(digestString);
                        var nonceString = Utils.Numerics.Byte32ArrayToHexString(nonceByte);
                        var difficultyString = NetworkInterface.Difficulty.ToString("X64");
                        
                        if (digest.Value >= m_TargetBigInteger.Value)
                        {
                            PrintMessage(platform, deviceID, "Error",
                                         "CPU verification failed: invalid solution"
                                         + "\nChallenge: " + challengeString
                                         + "\nAddress: 0x" + Address
                                         + "\nSolution: " + nonceString
                                         + "\nDigest: " + digestString
                                         + "\nTarget: " + Target);
                        }
                        else
                        {
                            PrintMessage(platform, deviceID, "Info", "Solution verified by CPU, submitting nonce " + nonceString + "...");

                            PrintMessage(platform, deviceID, "Debug",
                                         "Solution details..."
                                         + "\nChallenge: " + challengeString
                                         + "\nAddress: 0x" + Address
                                         + "\nSolution: " + nonceString
                                         + "\nDigest: " + digestString
                                         + "\nTarget: " + Target);

                            NetworkInterface.SubmitSolution(digestString, "0x" + Address, challengeString, difficultyString, Target, nonceString, this);
                        }
                    }
                }
            });
        }
    }
}