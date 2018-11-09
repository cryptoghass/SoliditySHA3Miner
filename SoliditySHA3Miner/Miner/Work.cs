using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SoliditySHA3Miner.Miner
{
    public static class Work
    {
        private static ulong m_Position = 0;

        private static readonly object m_positionLock = new object();

        public static byte[] KingAddress { get; set; }

        public static byte[] SolutionTemplate { get; set; }

        public static string GetKingAddressString()
        {
            if (KingAddress == null) return string.Empty;

            return HexByteConvertorExtensions.ToHex(KingAddress.ToArray(), prefix: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void GetKingAddress(byte* kingAddress)
        {
            if (KingAddress != null)
            {
                fixed (byte* tempKingAddress = KingAddress)
                    Buffer.MemoryCopy(tempKingAddress, kingAddress, MinerBase.ADDRESS_LENGTH, MinerBase.ADDRESS_LENGTH);
            }
            else
                for (int i = 0; i < MinerBase.ADDRESS_LENGTH; ++i)
                    kingAddress[i] = 0;
        }

        public static void SetKingAddress(string kingAddress)
        {
            if (string.IsNullOrWhiteSpace(kingAddress))
                KingAddress = null;
            else
                KingAddress = new HexBigInteger(kingAddress).ToHexByteArray().ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void GetSolutionTemplate(byte* solutionTemplate)
        {
            if (SolutionTemplate == null) return;

            fixed (byte* tempSolutionTemplate = SolutionTemplate)
                Buffer.MemoryCopy(tempSolutionTemplate, solutionTemplate, MinerBase.UINT256_LENGTH, MinerBase.UINT256_LENGTH);
        }

        public static void SetSolutionTemplate(string solutionTemplate) => SolutionTemplate = new HexBigInteger(solutionTemplate).ToHexByteArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetPosition(ref ulong workPosition)
        {
            lock (m_positionLock) { workPosition = m_Position; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetPosition(ref ulong lastPosition)
        {
            lock (m_positionLock)
            {
                lastPosition = m_Position;
                m_Position = 0u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IncrementPosition(ref ulong lastPosition, ulong increment)
        {
            lock (m_positionLock)
            {
                lastPosition = m_Position;
                m_Position += increment;
            }
        }
    }
}
