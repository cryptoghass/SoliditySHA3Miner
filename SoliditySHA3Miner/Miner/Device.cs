using SoliditySHA3Miner.Structs;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SoliditySHA3Miner.Miner
{
    public class Device : IDisposable
    {
        public const uint MAX_SOLUTION_COUNT = 4;
        public const uint DEFAULT_INTENSITY = 14;

        public bool AllowDevice;
        public string Type;
        public string Platform;
        public int DeviceID;
        public uint PciBusID;
        public string Name;
        public float Intensity;

        [JsonIgnore]
        public bool IsAssigned;
        [JsonIgnore]
        public bool IsInitialized;
        [JsonIgnore]
        public bool IsMining;
        [JsonIgnore]
        public bool IsPause;
        [JsonIgnore]
        public bool HasNewTarget;
        [JsonIgnore]
        public bool HasNewChallenge;

        [JsonIgnore]
        public ulong[] High64Target;
        [JsonIgnore]
        public byte[] Target;
        [JsonIgnore]
        public byte[] Challenge;
        [JsonIgnore]
        public byte[] MidState;
        [JsonIgnore]
        public byte[] Message;

        [JsonIgnore]
        public ulong HashCount;
        [JsonIgnore]
        public DateTime HashStartTime;

        [JsonIgnore]
        public DevicePointers Pointers;

        private float m_lastIntensity;
        private uint m_lastThreads;
        private uint m_lastCompute;
        private Dim3 m_lastBlock;
        private Dim3 m_newBlock;
        private Dim3 m_gridSize;

        [JsonIgnore]
        public uint ConputeVersion { get; set; }

        [JsonIgnore]
        public uint Threads
        {
            get
            {
                if (ConputeVersion <= 500)
                    Intensity = Intensity <= 40.55f ? Intensity : 40.55f;

                if (Intensity != m_lastIntensity)
                {
                    m_lastThreads = (uint)Math.Pow(2, Intensity);
                    m_lastIntensity = Intensity;
                    m_lastBlock.X = 0;
                }
                return m_lastThreads;
            }
        }

        [JsonIgnore]
        public Dim3 Block
        {
            get
            {
                if (m_lastCompute != ConputeVersion)
                {
                    m_lastCompute = ConputeVersion;

                    switch (ConputeVersion)
                    {
                        case 520:
                        case 610:
                        case 700:
                        case 720:
                        case 750:
                            m_newBlock.X = 1024u;
                            break;
                        case 300:
                        case 320:
                        case 350:
                        case 370:
                        case 500:
                        case 530:
                        case 600:
                        case 620:
                        default:
                            m_newBlock.X = (ConputeVersion >= 800) ? 1024u : 384u;
                            break;
                    }
                }
                return m_newBlock;
            }
        }

        [JsonIgnore]
        public Dim3 Grid
        {
            get
            {
                if (m_lastBlock.X != Block.X)
                {
                    m_gridSize.X = (Threads + Block.X - 1) / Block.X;
                    m_lastBlock.X = Block.X;
                }
                return m_gridSize;
            }
        }

        private readonly List<GCHandle> m_handles;

        public Device()
        {
            m_handles = new List<GCHandle>();

            High64Target = (ulong[])Array.CreateInstance(typeof(ulong), 1);
            Target = (byte[])Array.CreateInstance(typeof(byte), MinerBase.UINT256_LENGTH);
            Challenge = (byte[])Array.CreateInstance(typeof(byte), MinerBase.UINT256_LENGTH);
            MidState = (byte[])Array.CreateInstance(typeof(byte), MinerBase.SPONGE_LENGTH);
            Message = (byte[])Array.CreateInstance(typeof(byte), MinerBase.MESSAGE_LENGTH);

            Pointers.High64Target = AllocHandleAndGetPointer(High64Target);
            Pointers.Target = AllocHandleAndGetPointer(Target);
            Pointers.Challenge = AllocHandleAndGetPointer(Challenge);
            Pointers.MidState = AllocHandleAndGetPointer(MidState);
            Pointers.Message = AllocHandleAndGetPointer(Message);

            m_lastBlock.X = 1;
            m_lastBlock.Y = 1;
            m_lastBlock.Z = 1;

            m_newBlock.X = 1;
            m_newBlock.Y = 1;
            m_newBlock.Z = 1;

            m_gridSize.X = 1;
            m_gridSize.Y = 1;
            m_gridSize.Z = 1;
        }

        public void Dispose()
        {
            m_handles.Clear();
            m_handles.TrimExcess();
        }

        private IntPtr AllocHandleAndGetPointer(Array array)
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            m_handles.Add(handle);
            return handle.AddrOfPinnedObject();
        }
    }
}