using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace ControllerOverlay.Telemetry
{
    public static class RtssFpsReader
    {
        private const string SharedMemoryName = "RTSSSharedMemoryV2";
        private const uint RtssSignature = 0x53535452;
        private const uint RtssSignatureAlt = 0x52545353;
        private const uint MinimumVersion = 0x00020000;
        private const int MaxPath = 260;
        private const int ProcessIdOffset = 0;
        private const int NameOffset = 4;
        private const int Time0Offset = 268;
        private const int Time1Offset = 272;
        private const int FramesOffset = 276;
        private const int FrameTimeOffset = 280;

        public static bool TryGetGameFps(string processName, out double fps, out string matchedProcess)
        {
            fps = 0;
            matchedProcess = string.Empty;

            string normalizedTarget = NormalizeProcessName(processName);
            if (string.IsNullOrWhiteSpace(normalizedTarget))
            {
                normalizedTarget = "rocketleague.exe";
            }

            uint latestTick = 0;
            int currentPid = Process.GetCurrentProcess().Id;

            try
            {
                using MemoryMappedFile memory = MemoryMappedFile.OpenExisting(SharedMemoryName, MemoryMappedFileRights.Read);
                using MemoryMappedViewAccessor accessor = memory.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                uint signature = accessor.ReadUInt32(0);
                uint version = accessor.ReadUInt32(4);
                if ((signature != RtssSignature && signature != RtssSignatureAlt) || version < MinimumVersion)
                {
                    return false;
                }

                uint entrySize = accessor.ReadUInt32(8);
                uint appArrayOffset = accessor.ReadUInt32(12);
                uint appArraySize = accessor.ReadUInt32(16);

                for (uint i = 0; i < appArraySize; i++)
                {
                    long entryOffset = appArrayOffset + (i * entrySize);
                    uint entryPid = accessor.ReadUInt32(entryOffset + ProcessIdOffset);
                    if (entryPid == 0 || entryPid == currentPid)
                    {
                        continue;
                    }

                    string entryName = ReadAnsiString(accessor, entryOffset + NameOffset, MaxPath);
                    string fileName = Path.GetFileName(entryName).ToLowerInvariant();
                    if (!IsSameProcess(fileName, normalizedTarget))
                    {
                        continue;
                    }

                    if (!TryReadEntryFps(accessor, entryOffset, out double entryFps, out uint time1))
                    {
                        continue;
                    }

                    if (time1 >= latestTick)
                    {
                        latestTick = time1;
                        fps = entryFps;
                        matchedProcess = fileName;
                    }
                }
            }
            catch
            {
                return false;
            }

            return fps > 0;
        }

        private static bool TryReadEntryFps(MemoryMappedViewAccessor accessor, long entryOffset, out double fps, out uint time1)
        {
            fps = 0;
            time1 = accessor.ReadUInt32(entryOffset + Time1Offset);
            uint time0 = accessor.ReadUInt32(entryOffset + Time0Offset);
            uint frames = accessor.ReadUInt32(entryOffset + FramesOffset);
            uint frameTime = accessor.ReadUInt32(entryOffset + FrameTimeOffset);

            if (frameTime > 0)
            {
                fps = 1_000_000.0 / frameTime;
                return fps > 0 && !double.IsNaN(fps) && !double.IsInfinity(fps);
            }

            if (time0 > 0 && time1 > time0 && frames > 0)
            {
                fps = 1000.0 * frames / (time1 - time0);
                return fps > 0 && !double.IsNaN(fps) && !double.IsInfinity(fps);
            }

            return false;
        }

        private static bool IsSameProcess(string fileName, string target)
        {
            if (string.Equals(fileName, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(fileName, target + ".exe", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string NormalizeProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return string.Empty;
            }

            return Path.GetFileName(processName.Trim()).ToLowerInvariant();
        }

        private static string ReadAnsiString(MemoryMappedViewAccessor accessor, long offset, int maxLength)
        {
            byte[] bytes = new byte[maxLength];
            accessor.ReadArray(offset, bytes, 0, bytes.Length);

            int length = Array.IndexOf(bytes, (byte)0);
            if (length < 0)
            {
                length = bytes.Length;
            }

            return Encoding.Default.GetString(bytes, 0, length);
        }
    }
}
