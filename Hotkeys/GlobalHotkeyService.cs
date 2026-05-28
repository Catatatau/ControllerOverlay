using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ControllerOverlay.Hotkeys
{
    public class GlobalHotkeyService : IDisposable
    {
        private Window _window;
        private IntPtr _hwnd;
        private HwndSource? _source;
        private int _currentId;
        private Dictionary<int, Action> _hotkeys = new Dictionary<int, Action>();

        public GlobalHotkeyService(Window window)
        {
            _window = window;
            _window.SourceInitialized += Window_SourceInitialized;
            _window.Closed += Window_Closed;
            InitializeWindowHook();
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            InitializeWindowHook();
        }

        private void InitializeWindowHook()
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            if (_hwnd == IntPtr.Zero || _source != null)
            {
                return;
            }

            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(HwndHook);
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            Dispose();
        }

        public void RegisterHotkey(uint modifiers, uint key, Action action)
        {
            InitializeWindowHook();
            _currentId++;
            if (Overlay.Win32Interop.RegisterHotKey(_hwnd, _currentId, modifiers, key))
            {
                _hotkeys.Add(_currentId, action);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Overlay.Win32Interop.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeys.ContainsKey(id))
                {
                    _hotkeys[id].Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
            foreach (var id in _hotkeys.Keys)
            {
                Overlay.Win32Interop.UnregisterHotKey(_hwnd, id);
            }
            _hotkeys.Clear();
        }
    }
}
