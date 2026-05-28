using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ControllerOverlay.Overlay
{
    public class OverlayBehavior
    {
        private Window _window;
        private IntPtr _hwnd;
        private bool _isClickThrough;

        public OverlayBehavior(Window window)
        {
            _window = window;
            _window.SourceInitialized += Window_SourceInitialized;
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            // Hide from Alt-Tab
            int extendedStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
            Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, extendedStyle | Win32Interop.WS_EX_TOOLWINDOW);
        }

        public void SetClickThrough(bool clickThrough)
        {
            if (_hwnd == IntPtr.Zero) return;

            _isClickThrough = clickThrough;
            int extendedStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);

            if (clickThrough)
            {
                Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, extendedStyle | Win32Interop.WS_EX_TRANSPARENT | Win32Interop.WS_EX_LAYERED);
            }
            else
            {
                Win32Interop.SetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE, extendedStyle & ~Win32Interop.WS_EX_TRANSPARENT);
            }
        }
        
        public bool IsClickThrough => _isClickThrough;
    }
}
