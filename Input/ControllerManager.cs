using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.DirectInput;
using Vortice.XInput;

namespace ControllerOverlay.Input
{
    public class ControllerManager : IDisposable
    {
        public ControllerState CurrentState { get; private set; } = new ControllerState();
        public event Action? StateUpdated;

        private IDirectInput8? _directInput;
        private readonly List<DirectInputDeviceEntry> _directInputDevices = new();
        private DirectInputDeviceEntry? _activeDirectInputDevice;
        private IntPtr _windowHandle;
        private int _directInputProbeCooldown;
        private bool _isRunning;
        private double _deadzone = 0.08;

        public ControllerManager()
        {
            try
            {
                _directInput = DInput.DirectInput8Create();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to initialize DirectInput: " + ex.Message);
            }
        }

        public void SetDeadzone(double deadzone)
        {
            _deadzone = deadzone;
        }

        public void SetWindowHandle(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
        }

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
                    Debug.WriteLine("Controller polling failed: " + ex.Message);
                    MarkDisconnected();
                }

                Thread.Sleep(8); // Roughly 125Hz
            }
        }

        private void Poll()
        {
            for (uint index = 0; index < 4; index++)
            {
                if (Vortice.XInput.XInput.GetState(index, out Vortice.XInput.State xState))
                {
                    ApplyXInputState(index, xState);
                    return;
                }
            }

            PollDirectInput();
        }

        private void ApplyXInputState(uint index, Vortice.XInput.State xState)
        {
            CurrentState.IsConnected = true;
            CurrentState.ControllerName = $"XInput Controller {index + 1}";
            CurrentState.Layout = "Xbox";

            var gp = xState.Gamepad;
            CurrentState.A = (gp.Buttons & GamepadButtons.A) != 0;
            CurrentState.B = (gp.Buttons & GamepadButtons.B) != 0;
            CurrentState.X = (gp.Buttons & GamepadButtons.X) != 0;
            CurrentState.Y = (gp.Buttons & GamepadButtons.Y) != 0;
            
            CurrentState.DPadUp = (gp.Buttons & GamepadButtons.DPadUp) != 0;
            CurrentState.DPadDown = (gp.Buttons & GamepadButtons.DPadDown) != 0;
            CurrentState.DPadLeft = (gp.Buttons & GamepadButtons.DPadLeft) != 0;
            CurrentState.DPadRight = (gp.Buttons & GamepadButtons.DPadRight) != 0;

            CurrentState.L1 = (gp.Buttons & GamepadButtons.LeftShoulder) != 0;
            CurrentState.R1 = (gp.Buttons & GamepadButtons.RightShoulder) != 0;
            CurrentState.L3 = (gp.Buttons & GamepadButtons.LeftThumb) != 0;
            CurrentState.R3 = (gp.Buttons & GamepadButtons.RightThumb) != 0;

            CurrentState.Start = (gp.Buttons & GamepadButtons.Start) != 0;
            CurrentState.Select = (gp.Buttons & GamepadButtons.Back) != 0;
            CurrentState.Home = false;
            
            CurrentState.L2 = gp.LeftTrigger / 255.0;
            CurrentState.R2 = gp.RightTrigger / 255.0;

            CurrentState.LeftStickX = gp.LeftThumbX / 32768.0;
            CurrentState.LeftStickY = -gp.LeftThumbY / 32768.0;
            CurrentState.RightStickX = gp.RightThumbX / 32768.0;
            CurrentState.RightStickY = -gp.RightThumbY / 32768.0;

            CurrentState.ApplyDeadzone(_deadzone);
        }

        private void PollDirectInput()
        {
            if (_directInput == null)
            {
                MarkDisconnected();
                return;
            }

            if (_directInputDevices.Count == 0 && !TryOpenDirectInputDevices())
            {
                MarkDisconnected();
                return;
            }

            if (_directInputDevices.Count == 0)
            {
                MarkDisconnected();
                return;
            }

            DirectInputDeviceEntry? bestDevice = null;
            JoystickState bestState = new();
            double bestActivity = -1;

            foreach (var entry in _directInputDevices.ToArray())
            {
                if (!TryReadDirectInputDevice(entry.Device, out var state))
                {
                    entry.FailedReads++;
                    if (entry.FailedReads >= 4)
                    {
                        RemoveDirectInputDevice(entry);
                    }

                    continue;
                }

                entry.FailedReads = 0;
                var activity = MeasureActivity(state);

                if (entry == _activeDirectInputDevice)
                {
                    activity += 0.01;
                }

                if (activity > bestActivity)
                {
                    bestActivity = activity;
                    bestDevice = entry;
                    bestState = state;
                }
            }

            if (bestDevice != null)
            {
                _activeDirectInputDevice = bestDevice;
                CurrentState.ControllerName = bestDevice.ProductName;
                CurrentState.Layout = bestDevice.Layout;
                ApplyDirectInputState(bestState);
                return;
            }

            MarkDisconnected();
        }

        private bool TryOpenDirectInputDevices()
        {
            if (_directInput == null)
            {
                return false;
            }

            if (_directInputProbeCooldown > 0)
            {
                _directInputProbeCooldown--;
                return false;
            }

            DeviceInstance[] devices;
            try
            {
                devices = _directInput
                    .GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
                    .OrderByDescending(device => IsPreferredDirectInputDevice(device.ProductName))
                    .ThenBy(device => device.ProductName.Contains("XInput", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to enumerate DirectInput devices: " + ex.Message);
                _directInputProbeCooldown = 60;
                return false;
            }

            if (devices.Length == 0)
            {
                _directInputProbeCooldown = 60;
                return false;
            }

            CloseDirectInputDevices();

            foreach (var deviceInstance in devices)
            {
                IDirectInputDevice8? device = null;
                try
                {
                    device = _directInput.CreateDevice(deviceInstance.InstanceGuid);
                    ApplyCooperativeLevel(device, deviceInstance.ProductName);
                    device.SetDataFormat<RawJoystickState>();
                    device.Acquire();

                    if (!TryReadDirectInputDevice(device, out _))
                    {
                        device.Dispose();
                        continue;
                    }

                    _directInputDevices.Add(new DirectInputDeviceEntry(
                        device,
                        deviceInstance.ProductName,
                        DetectLayout(deviceInstance.ProductName)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open DirectInput device '{deviceInstance.ProductName}': {ex.Message}");
                    device?.Dispose();
                }
            }

            _directInputProbeCooldown = _directInputDevices.Count > 0 ? 240 : 60;
            return _directInputDevices.Count > 0;
        }

        private void ApplyCooperativeLevel(IDirectInputDevice8 device, string productName)
        {
            if (_windowHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                device.SetCooperativeLevel(_windowHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set DirectInput background mode for '{productName}': {ex.Message}");
            }
        }

        private static bool TryReadDirectInputDevice(IDirectInputDevice8 device, out JoystickState state)
        {
            state = new JoystickState();

            try
            {
                try
                {
                    device.Poll();
                }
                catch
                {
                    device.Acquire();
                    device.Poll();
                }

                state = device.GetCurrentJoystickState();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to read DirectInput device: " + ex.Message);
                return false;
            }
        }

        private static double MeasureActivity(JoystickState state)
        {
            var buttons = state.Buttons ?? Array.Empty<bool>();
            var povs = state.PointOfViewControllers ?? Array.Empty<int>();
            double score = buttons.Any(button => button) ? 100 : 0;

            if (povs.Any(pov => pov >= 0 && pov < 36000))
            {
                score += 50;
            }

            score += Math.Abs(NormalizeAxis(state.X));
            score += Math.Abs(NormalizeAxis(state.Y));
            score += Math.Abs(NormalizeAxis(state.Z));
            score += Math.Abs(NormalizeAxis(state.RotationX));
            score += Math.Abs(NormalizeAxis(state.RotationY));
            score += Math.Abs(NormalizeAxis(state.RotationZ));

            return score;
        }

        private void ApplyDirectInputState(JoystickState state)
        {
            var buttons = state.Buttons ?? Array.Empty<bool>();
            var povs = state.PointOfViewControllers ?? Array.Empty<int>();
            var pov = povs.Length > 0 ? povs[0] : -1;

            CurrentState.IsConnected = true;

            // Common DirectInput mapping for PlayStation controllers and DS4Windows HID mode.
            CurrentState.X = GetButton(buttons, 0);
            CurrentState.A = GetButton(buttons, 1);
            CurrentState.B = GetButton(buttons, 2);
            CurrentState.Y = GetButton(buttons, 3);

            CurrentState.L1 = GetButton(buttons, 4);
            CurrentState.R1 = GetButton(buttons, 5);
            CurrentState.L2 = GetButton(buttons, 6) ? 1.0 : NormalizeTriggerAxis(state.RotationX);
            CurrentState.R2 = GetButton(buttons, 7) ? 1.0 : NormalizeTriggerAxis(state.RotationY);

            CurrentState.Select = GetButton(buttons, 8);
            CurrentState.Start = GetButton(buttons, 9);
            CurrentState.L3 = GetButton(buttons, 10);
            CurrentState.R3 = GetButton(buttons, 11);
            CurrentState.Home = GetButton(buttons, 12);

            CurrentState.DPadUp = pov == 0 || pov == 4500 || pov == 31500;
            CurrentState.DPadRight = pov == 4500 || pov == 9000 || pov == 13500;
            CurrentState.DPadDown = pov == 13500 || pov == 18000 || pov == 22500;
            CurrentState.DPadLeft = pov == 22500 || pov == 27000 || pov == 31500;

            CurrentState.LeftStickX = NormalizeAxis(state.X);
            CurrentState.LeftStickY = NormalizeAxis(state.Y);
            CurrentState.RightStickX = NormalizeAxis(state.Z);
            CurrentState.RightStickY = NormalizeAxis(state.RotationZ);

            CurrentState.ApplyDeadzone(_deadzone);
        }

        private static bool GetButton(bool[] buttons, int index)
        {
            return index >= 0 && index < buttons.Length && buttons[index];
        }

        private static double NormalizeAxis(int value)
        {
            return Math.Clamp((value - 32767.0) / 32767.0, -1.0, 1.0);
        }

        private static double NormalizeTriggerAxis(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return Math.Clamp(value / 65535.0, 0.0, 1.0);
        }

        private static bool IsPreferredDirectInputDevice(string productName)
        {
            return productName.Contains("Wireless Controller", StringComparison.OrdinalIgnoreCase) ||
                   productName.Contains("DualShock", StringComparison.OrdinalIgnoreCase) ||
                   productName.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
                   productName.Contains("Sony", StringComparison.OrdinalIgnoreCase);
        }

        private static string DetectLayout(string productName)
        {
            if (productName.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
                productName.Contains("DualShock", StringComparison.OrdinalIgnoreCase) ||
                productName.Contains("Wireless Controller", StringComparison.OrdinalIgnoreCase) ||
                productName.Contains("Sony", StringComparison.OrdinalIgnoreCase))
            {
                return "PlayStation";
            }

            if (productName.Contains("Xbox", StringComparison.OrdinalIgnoreCase) ||
                productName.Contains("XInput", StringComparison.OrdinalIgnoreCase))
            {
                return "Xbox";
            }

            return "Generic";
        }

        private void MarkDisconnected()
        {
            CurrentState.IsConnected = false;
            CurrentState.A = false;
            CurrentState.B = false;
            CurrentState.X = false;
            CurrentState.Y = false;
            CurrentState.DPadUp = false;
            CurrentState.DPadDown = false;
            CurrentState.DPadLeft = false;
            CurrentState.DPadRight = false;
            CurrentState.L1 = false;
            CurrentState.R1 = false;
            CurrentState.L3 = false;
            CurrentState.R3 = false;
            CurrentState.Start = false;
            CurrentState.Select = false;
            CurrentState.Home = false;
            CurrentState.LeftStickX = 0;
            CurrentState.LeftStickY = 0;
            CurrentState.RightStickX = 0;
            CurrentState.RightStickY = 0;
            CurrentState.L2 = 0;
            CurrentState.R2 = 0;
        }

        private void RemoveDirectInputDevice(DirectInputDeviceEntry entry)
        {
            if (_activeDirectInputDevice == entry)
            {
                _activeDirectInputDevice = null;
            }

            _directInputDevices.Remove(entry);
            entry.Dispose();
        }

        private void CloseDirectInputDevices()
        {
            foreach (var entry in _directInputDevices.ToArray())
            {
                entry.Dispose();
            }

            _directInputDevices.Clear();
            _activeDirectInputDevice = null;
        }

        public void Dispose()
        {
            Stop();
            CloseDirectInputDevices();
            _directInput?.Dispose();
        }

        private sealed class DirectInputDeviceEntry : IDisposable
        {
            public DirectInputDeviceEntry(IDirectInputDevice8 device, string productName, string layout)
            {
                Device = device;
                ProductName = productName;
                Layout = layout;
            }

            public IDirectInputDevice8 Device { get; }
            public string ProductName { get; }
            public string Layout { get; }
            public int FailedReads { get; set; }

            public void Dispose()
            {
                try
                {
                    Device.Unacquire();
                }
                catch
                {
                }

                Device.Dispose();
            }
        }
    }
}
