using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace ControllerOverlay.Telemetry
{
    public sealed class EtwGameFpsReader : IDisposable
    {
        private const string DxgKrnlProviderName = "Microsoft-Windows-DxgKrnl";
        private readonly object _sync = new();
        private readonly Queue<DateTime> _presentTimes = new();

        private CancellationTokenSource? _cts;
        private Task? _runnerTask;
        private TraceEventSession? _session;
        private DateTime _lastPresentUtc = DateTime.MinValue;
        private DateTime _lastFpsUpdateUtc = DateTime.MinValue;
        private double? _lastFps;

        public bool IsRunning { get; private set; }
        public bool RequiresAdministrator { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public DateTime LastUpdateUtc => _lastFpsUpdateUtc;

        public double? GameFps
        {
            get
            {
                lock (_sync)
                {
                    return _lastFps;
                }
            }
        }

        public void Start(string processName)
        {
            Stop();

            RequiresAdministrator = false;
            LastError = string.Empty;
            lock (_sync)
            {
                _presentTimes.Clear();
                _lastPresentUtc = DateTime.MinValue;
                _lastFpsUpdateUtc = DateTime.MinValue;
                _lastFps = null;
            }

            _cts = new CancellationTokenSource();
            _runnerTask = Task.Run(() => Run(processName, _cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();

            try
            {
                _session?.Stop();
            }
            catch
            {
            }

            _cts?.Dispose();
            _cts = null;
            _runnerTask = null;
            _session = null;
            IsRunning = false;
        }

        private void Run(string processName, CancellationToken token)
        {
            string normalizedProcessName = NormalizeProcessName(processName);
            string sessionName = "ControllerOverlayGameFps-" + Environment.ProcessId;
            DateTime lastPidRefreshUtc = DateTime.MinValue;
            HashSet<int> targetProcessIds = new();

            try
            {
                if (TraceEventSession.IsElevated() != true)
                {
                    RequiresAdministrator = true;
                    LastError = "Administrador necessario para ler FPS real via ETW.";
                    return;
                }

                using var session = new TraceEventSession(sessionName);
                _session = session;
                session.StopOnDispose = true;
                session.EnableProvider(DxgKrnlProviderName, TraceEventLevel.Verbose, 0x0000000008000000);

                IsRunning = true;
                RefreshTargetProcessIds(normalizedProcessName, targetProcessIds);
                lastPidRefreshUtc = DateTime.UtcNow;

                session.Source.Dynamic.All += data =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    DateTime refreshNow = DateTime.UtcNow;
                    if ((refreshNow - lastPidRefreshUtc).TotalSeconds >= 1)
                    {
                        RefreshTargetProcessIds(normalizedProcessName, targetProcessIds);
                        lastPidRefreshUtc = refreshNow;
                    }

                    if (!IsTargetProcessEvent(data, targetProcessIds) || !IsPresentEvent(data) || !IsSuccessfulWindowPresent(data))
                    {
                        return;
                    }

                    DateTime eventUtc = data.TimeStamp.Kind == DateTimeKind.Utc
                        ? data.TimeStamp
                        : data.TimeStamp.ToUniversalTime();
                    RecordPresent(eventUtc);
                };

                session.Source.Process();
            }
            catch (UnauthorizedAccessException ex)
            {
                RequiresAdministrator = true;
                LastError = ex.Message;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            finally
            {
                IsRunning = false;
                _session = null;
            }
        }

        private static void RefreshTargetProcessIds(string normalizedProcessName, HashSet<int> targetProcessIds)
        {
            targetProcessIds.Clear();
            foreach (var process in Process.GetProcessesByName(normalizedProcessName))
            {
                try
                {
                    targetProcessIds.Add(process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private void RecordPresent(DateTime now)
        {
            lock (_sync)
            {
                if (_lastPresentUtc != DateTime.MinValue &&
                    (now - _lastPresentUtc).TotalMilliseconds < 1)
                {
                    return;
                }

                _presentTimes.Enqueue(now);
                _lastPresentUtc = now;
                PruneOldPresents(now);
                UpdateFpsSnapshot();
            }
        }

        private void UpdateFpsSnapshot()
        {
            if (_presentTimes.Count < 2)
            {
                return;
            }

            double seconds = (_lastPresentUtc - _presentTimes.Peek()).TotalSeconds;
            if (seconds <= 0.01)
            {
                return;
            }

            double fps = (_presentTimes.Count - 1) / seconds;
            if (fps <= 0 || double.IsNaN(fps) || double.IsInfinity(fps))
            {
                return;
            }

            _lastFps = fps;
            _lastFpsUpdateUtc = DateTime.UtcNow;
        }

        private void PruneOldPresents(DateTime now)
        {
            DateTime cutoff = now.AddSeconds(-1);
            while (_presentTimes.Count > 0 && _presentTimes.Peek() < cutoff)
            {
                _presentTimes.Dequeue();
            }
        }

        private static bool IsPresentEvent(TraceEvent data)
        {
            return (int)data.ID == 184 &&
                   string.Equals(data.TaskName, "Present", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTargetProcessEvent(TraceEvent data, HashSet<int> targetProcessIds)
        {
            return targetProcessIds.Contains(data.ProcessID);
        }

        private static bool IsSuccessfulWindowPresent(TraceEvent data)
        {
            long? hWindow = null;
            long? returnStatus = null;
            for (int i = 0; i < data.PayloadNames.Length; i++)
            {
                string name = data.PayloadNames[i];
                if (name.Equals("hWindow", StringComparison.OrdinalIgnoreCase) &&
                    TryGetLong(data.PayloadValue(i), out long parsedWindow))
                {
                    hWindow = parsedWindow;
                }

                if (name.Equals("ReturnStatus", StringComparison.OrdinalIgnoreCase) &&
                    TryGetLong(data.PayloadValue(i), out long parsedStatus))
                {
                    returnStatus = parsedStatus;
                }
            }

            return hWindow.GetValueOrDefault(1) != 0 && returnStatus.GetValueOrDefault(0) == 0;
        }

        private static bool TryGetLong(object? value, out long result)
        {
            switch (value)
            {
                case int intValue:
                    result = intValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case ulong ulongValue when ulongValue <= long.MaxValue:
                    result = (long)ulongValue;
                    return true;
                default:
                    return long.TryParse(value?.ToString(), out result);
            }
        }

        private static string NormalizeProcessName(string processName)
        {
            string fileName = Path.GetFileNameWithoutExtension(processName.Trim());
            return string.IsNullOrWhiteSpace(fileName) ? "RocketLeague" : fileName;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
