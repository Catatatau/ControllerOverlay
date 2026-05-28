using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ControllerOverlay.Overlay;

namespace ControllerOverlay.Input
{
    public class KeyboardMouseManager : IDisposable
    {
        public KeyboardMouseState CurrentState { get; private set; } = new KeyboardMouseState();
        public event Action? StateUpdated;

        private bool _isRunning;

        // Virtual Keys (VK_*)
        private const int VK_W = 0x57;
        private const int VK_A = 0x41;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;
        private const int VK_Q = 0x51;
        private const int VK_E = 0x45;
        private const int VK_R = 0x52;
        private const int VK_F = 0x46;
        private const int VK_Z = 0x5A;
        private const int VK_X = 0x58;
        private const int VK_C = 0x43;
        private const int VK_V = 0x56;
        private const int VK_B = 0x42;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_SPACE = 0x20;
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_XBUTTON1 = 0x05;
        private const int VK_XBUTTON2 = 0x06;

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(PollLoop);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void PollLoop()
        {
            while (_isRunning)
            {
                try
                {
                    Poll();
                    StateUpdated?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("KBM polling failed: " + ex.Message);
                }

                Thread.Sleep(8); // Roughly 125Hz
            }
        }

        private void Poll()
        {
            CurrentState.W = IsKeyDown(VK_W);
            CurrentState.A = IsKeyDown(VK_A);
            CurrentState.S = IsKeyDown(VK_S);
            CurrentState.D = IsKeyDown(VK_D);
            CurrentState.Q = IsKeyDown(VK_Q);
            CurrentState.E = IsKeyDown(VK_E);
            CurrentState.R = IsKeyDown(VK_R);
            CurrentState.F = IsKeyDown(VK_F);
            CurrentState.Z = IsKeyDown(VK_Z);
            CurrentState.X = IsKeyDown(VK_X);
            CurrentState.C = IsKeyDown(VK_C);
            CurrentState.V = IsKeyDown(VK_V);
            CurrentState.B = IsKeyDown(VK_B);
            CurrentState.Shift = IsKeyDown(VK_SHIFT);
            CurrentState.Ctrl = IsKeyDown(VK_CONTROL);
            CurrentState.Space = IsKeyDown(VK_SPACE);
            CurrentState.LClick = IsKeyDown(VK_LBUTTON);
            CurrentState.RClick = IsKeyDown(VK_RBUTTON);
            CurrentState.Mouse4 = IsKeyDown(VK_XBUTTON1);
            CurrentState.Mouse5 = IsKeyDown(VK_XBUTTON2);
        }

        private static bool IsKeyDown(int vKey)
        {
            return (Win32Interop.GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
