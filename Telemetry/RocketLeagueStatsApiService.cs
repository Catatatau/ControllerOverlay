using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ControllerOverlay.Telemetry
{
    public class RocketLeagueStatsApiService : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _runnerTask;

        private long? _lastFrame;
        private double? _lastElapsed;
        private DateTime? _lastPacketUtc;

        public bool IsRunning { get; private set; }
        public double? GameFps { get; private set; }
        public double? BallSpeedUus { get; private set; }
        public DateTime LastUpdateUtc { get; private set; } = DateTime.MinValue;
        public DateTime FpsLastUpdateUtc { get; private set; } = DateTime.MinValue;
        public DateTime BallSpeedLastUpdateUtc { get; private set; } = DateTime.MinValue;
        public string LastError { get; private set; } = string.Empty;

        public void Start(int port)
        {
            Stop();
            _cts = new CancellationTokenSource();
            IsRunning = true;
            LastError = string.Empty;
            _lastFrame = null;
            _lastElapsed = null;
            _lastPacketUtc = null;
            _runnerTask = Task.Run(() => RunLoop(port, _cts.Token));
        }

        public void Stop()
        {
            IsRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _runnerTask = null;
            _lastFrame = null;
            _lastElapsed = null;
            _lastPacketUtc = null;
            GameFps = null;
            BallSpeedUus = null;
            FpsLastUpdateUtc = DateTime.MinValue;
            BallSpeedLastUpdateUtc = DateTime.MinValue;
        }

        private async Task RunLoop(int port, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool connected = await TryRunTcp(port, token);
                    if (!connected)
                    {
                        await TryRunWebSocket(port, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }

                try
                {
                    await Task.Delay(700, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task<bool> TryRunWebSocket(int port, CancellationToken token)
        {
            using var ws = new ClientWebSocket();
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(900);
                await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), timeout.Token);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }

            byte[] buffer = new byte[8192];
            using var ms = new MemoryStream();
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                ms.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                string payload = Encoding.UTF8.GetString(ms.ToArray());
                ms.SetLength(0);
                ProcessIncomingPayload(payload);
            }

            return true;
        }

        private async Task<bool> TryRunTcp(int port, CancellationToken token)
        {
            using var client = new TcpClient();
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(900);
                await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }

            using NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[8192];
            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, token);
                if (bytesRead <= 0)
                {
                    break;
                }

                string payload = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessIncomingPayload(payload);
            }

            return true;
        }

        private void ProcessIncomingPayload(string payload)
        {
            foreach (string json in ExtractJsonObjects(payload))
            {
                ProcessMessage(json);
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                JObject msg = JObject.Parse(json);
                string? eventName = msg["Event"]?.ToString();
                if (!string.Equals(eventName, "UpdateState", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                JToken? data = NormalizeDataToken(msg["Data"]);
                JToken? game = data?["Game"] ?? data;
                if (game == null)
                {
                    return;
                }

                DateTime now = DateTime.UtcNow;

                double? speedUus =
                    GetNumber(game["BallSpeed"]) ??
                    GetNumber(data?["BallSpeed"]) ??
                    GetNumber(game["Ball"]?["Speed"]) ??
                    GetNumber(game["Ball"]?["Physics"]?["Speed"]);

                if (!speedUus.HasValue)
                {
                    speedUus = TryVectorSpeed(game["Ball"]?["Velocity"]) ??
                               TryVectorSpeed(game["Ball"]?["LinearVelocity"]) ??
                               TryVectorSpeed(game["Ball"]?["Physics"]?["LinearVelocity"]);
                }

                if (speedUus.HasValue)
                {
                    BallSpeedUus = speedUus.Value;
                    BallSpeedLastUpdateUtc = now;
                    LastUpdateUtc = now;
                }

                double? fps =
                    GetNumber(game["FPS"]) ??
                    GetNumber(game["Fps"]) ??
                    GetNumber(game["FrameRate"]) ??
                    GetNumber(game["Framerate"]) ??
                    GetNumber(game["Performance"]?["FPS"]) ??
                    GetNumber(game["Performance"]?["FrameRate"]);

                long? frame = GetLong(game["Frame"]);
                double? elapsed = GetNumber(game["Elapsed"]) ?? GetNumber(game["TimeSeconds"]);
                if (!fps.HasValue && frame.HasValue && elapsed.HasValue && _lastFrame.HasValue && _lastElapsed.HasValue)
                {
                    long deltaFrame = frame.Value - _lastFrame.Value;
                    double deltaElapsed = elapsed.Value - _lastElapsed.Value;
                    if (deltaFrame > 0 && deltaElapsed > 0.0001)
                    {
                        fps = deltaFrame / deltaElapsed;
                    }
                }

                if (fps.HasValue && fps.Value > 0 && !double.IsNaN(fps.Value) && !double.IsInfinity(fps.Value))
                {
                    GameFps = fps.Value;
                    FpsLastUpdateUtc = now;
                    LastUpdateUtc = now;
                }

                _lastFrame = frame;
                _lastElapsed = elapsed;
                _lastPacketUtc = now;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        private static IEnumerable<string> ExtractJsonObjects(string input)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
            {
                return results;
            }

            int depth = 0;
            int start = -1;
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                    }

                    depth++;
                    continue;
                }

                if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        results.Add(input.Substring(start, i - start + 1));
                        start = -1;
                    }
                }
            }

            return results;
        }

        private static JToken? NormalizeDataToken(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.String)
            {
                string? text = token.Value<string>();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                try
                {
                    return JToken.Parse(text);
                }
                catch
                {
                    return null;
                }
            }

            return token;
        }

        private static double? GetNumber(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            return token.Type switch
            {
                JTokenType.Integer => token.Value<double>(),
                JTokenType.Float => token.Value<double>(),
                _ => double.TryParse(token.ToString(), out double parsed) ? parsed : null
            };
        }

        private static long? GetLong(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            return token.Type switch
            {
                JTokenType.Integer => token.Value<long>(),
                JTokenType.Float => (long)token.Value<double>(),
                _ => long.TryParse(token.ToString(), out long parsed) ? parsed : null
            };
        }

        private static double? TryVectorSpeed(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            double? x = GetNumber(token["X"] ?? token["x"]);
            double? y = GetNumber(token["Y"] ?? token["y"]);
            double? z = GetNumber(token["Z"] ?? token["z"]);
            if (!x.HasValue || !y.HasValue || !z.HasValue)
            {
                return null;
            }

            return Math.Sqrt((x.Value * x.Value) + (y.Value * y.Value) + (z.Value * z.Value));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
