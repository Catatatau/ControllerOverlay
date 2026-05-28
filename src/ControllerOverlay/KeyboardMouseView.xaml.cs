using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControllerOverlay.Input;
using ControllerOverlay.Settings;

namespace ControllerOverlay
{
    public partial class KeyboardMouseView : UserControl
    {
        private SolidColorBrush _activeBrush = new SolidColorBrush(Color.FromRgb(0, 255, 204));
        private SolidColorBrush _inactiveBrush = new SolidColorBrush(Colors.Transparent);
        private SolidColorBrush _outlineBrush = new SolidColorBrush(Color.FromRgb(232, 238, 247));

        public KeyboardMouseView()
        {
            InitializeComponent();
        }

        public void ApplySettings(AppSettings settings)
        {
            if (ColorConverter.ConvertFromString(settings.AccentColor) is Color c)
            {
                _activeBrush = new SolidColorBrush(c);
            }

            if (settings.Theme == "Preto")
            {
                _outlineBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                RootBorder.Background = settings.IsBackgroundTransparent ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(180, 10, 10, 10));
            }
            else
            {
                _outlineBrush = new SolidColorBrush(Color.FromRgb(232, 238, 247));
                RootBorder.Background = settings.IsBackgroundTransparent ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(100, 20, 25, 35));
            }

            RootBorder.CornerRadius = new CornerRadius(16);
            if (!settings.IsBackgroundTransparent)
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                RootBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                RootBorder.BorderThickness = new Thickness(0);
            }

            // Apply outline
            KeyQ.BorderBrush = _outlineBrush;
            KeyW.BorderBrush = _outlineBrush;
            KeyE.BorderBrush = _outlineBrush;
            KeyR.BorderBrush = _outlineBrush;
            KeyA.BorderBrush = _outlineBrush;
            KeyS.BorderBrush = _outlineBrush;
            KeyD.BorderBrush = _outlineBrush;
            KeyF.BorderBrush = _outlineBrush;
            KeyZ.BorderBrush = _outlineBrush;
            KeyX.BorderBrush = _outlineBrush;
            KeyC.BorderBrush = _outlineBrush;
            KeyV.BorderBrush = _outlineBrush;
            KeyShift.BorderBrush = _outlineBrush;
            KeyCtrl.BorderBrush = _outlineBrush;
            KeySpace.BorderBrush = _outlineBrush;

            LClick.BorderBrush = _outlineBrush;
            RClick.BorderBrush = _outlineBrush;
            Mouse4.BorderBrush = _outlineBrush;
            Mouse5.BorderBrush = _outlineBrush;
            
            // Note: Canvas mouse body border is hardcoded in XAML for simplicity, but we could name it and update it.
        }

        public void UpdateState(KeyboardMouseState state)
        {
            KeyQ.Background = state.Q ? _activeBrush : _inactiveBrush;
            KeyW.Background = state.W ? _activeBrush : _inactiveBrush;
            KeyE.Background = state.E ? _activeBrush : _inactiveBrush;
            KeyR.Background = state.R ? _activeBrush : _inactiveBrush;
            KeyA.Background = state.A ? _activeBrush : _inactiveBrush;
            KeyS.Background = state.S ? _activeBrush : _inactiveBrush;
            KeyD.Background = state.D ? _activeBrush : _inactiveBrush;
            KeyF.Background = state.F ? _activeBrush : _inactiveBrush;
            KeyZ.Background = state.Z ? _activeBrush : _inactiveBrush;
            KeyX.Background = state.X ? _activeBrush : _inactiveBrush;
            KeyC.Background = state.C ? _activeBrush : _inactiveBrush;
            KeyV.Background = state.V ? _activeBrush : _inactiveBrush;
            KeyShift.Background = state.Shift ? _activeBrush : _inactiveBrush;
            KeyCtrl.Background = state.Ctrl ? _activeBrush : _inactiveBrush;
            KeySpace.Background = state.Space ? _activeBrush : _inactiveBrush;

            LClick.Background = state.LClick ? _activeBrush : _inactiveBrush;
            RClick.Background = state.RClick ? _activeBrush : _inactiveBrush;
            Mouse4.Background = state.Mouse4 ? _activeBrush : _inactiveBrush;
            Mouse5.Background = state.Mouse5 ? _activeBrush : _inactiveBrush;
        }
    }
}
