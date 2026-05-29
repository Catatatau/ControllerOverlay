using System;
using System.Collections.Generic;
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
            var pressed = new List<int>();
            for (int virtualKey = 1; virtualKey <= 254; virtualKey++)
            {
                if (IsKeyDown(virtualKey))
                {
                    pressed.Add(virtualKey);
                }
            }

            var state = new KeyboardMouseState();
            state.SetPressedKeys(pressed);
            CurrentState = state;
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
