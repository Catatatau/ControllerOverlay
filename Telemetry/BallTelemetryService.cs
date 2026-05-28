using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ControllerOverlay.Telemetry
{
    public class BallTelemetryService : IDisposable
    {
        private UdpClient? _client;
        private CancellationTokenSource? _cts;

        public bool IsRunning { get; private set; }
        public double? BallSpeedKph { get; private set; }
        public DateTime LastUpdateUtc { get; private set; } = DateTime.MinValue;
        public string LastError { get; private set; } = string.Empty;

        public void Start(int port)
        {
            Stop();

            try
            {
                _cts = new CancellationTokenSource();
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                IsRunning = true;
                LastError = string.Empty;
                _ = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsRunning = false;
                _client?.Dispose();
                _client = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void Stop()
        {
            IsRunning = false;
            _cts?.Cancel();
            _client?.Dispose();
            _cts?.Dispose();
            _client = null;
            _cts = null;
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _client != null)
            {
                try
                {
                    UdpReceiveResult result = await _client.ReceiveAsync(token);
                    string payload = Encoding.UTF8.GetString(result.Buffer).Trim();

                    if (TryReadSpeed(payload, out double speedKph))
                    {
                        BallSpeedKph = speedKph;
                        LastUpdateUtc = DateTime.UtcNow;
                        LastError = string.Empty;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }
        }

        private static bool TryReadSpeed(string payload, out double speedKph)
        {
            if (double.TryParse(payload, NumberStyles.Float, CultureInfo.InvariantCulture, out speedKph))
            {
                return true;
            }

            try
            {
                JObject json = JObject.Parse(payload);
                JToken? token = json["ballSpeedKph"] ??
                                json["ball_speed_kph"] ??
                                json["ballSpeed"] ??
                                json["speed"];

                if (token == null)
                {
                    speedKph = 0;
                    return false;
                }

                return double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out speedKph);
            }
            catch
            {
                speedKph = 0;
                return false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
