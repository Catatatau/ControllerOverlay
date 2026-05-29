using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using ControllerOverlay.Settings;

namespace ControllerOverlay
{
    public partial class SettingsWindow : Window
    {
        private SettingsManager _manager;
        private MainWindow _mainWindow;
        private bool _isLoaded;

        public SettingsWindow(SettingsManager manager, MainWindow mainWindow)
        {
            InitializeComponent();
            _manager = manager;
            _mainWindow = mainWindow;

            LoadSettingsToUI();
            _isLoaded = true;
        }

        private void LoadSettingsToUI()
        {
            var s = _manager.CurrentSettings;
            
            SetComboText(CmbLayout, s.Layout);
            SetComboText(CmbKeyboardPreset, s.KeyboardPreset);
            SetComboText(CmbTheme, s.Theme);
            TxtAccentColor.Text = s.AccentColor;
            UpdateAccentPreview(s.AccentColor);
            SldScale.Value = s.Scale;
            SldOpacity.Value = s.Opacity;
            SldDeadzone.Value = s.Deadzone;
            ChkShowFps.IsChecked = s.ShowFps;
            ChkShowBallSpeed.IsChecked = s.ShowBallSpeed;
            TxtFpsPort.Text = s.FpsUdpPort.ToString();
            TxtStatsLeft.Text = s.StatsPanelLeft.ToString("0", CultureInfo.InvariantCulture);
            TxtStatsTop.Text = s.StatsPanelTop.ToString("0", CultureInfo.InvariantCulture);
            SldStatsScale.Value = s.StatsPanelScale;
            ChkStatsTransparentBg.IsChecked = s.IsStatsBackgroundTransparent;
            ChkStatsMovable.IsChecked = s.IsStatsPanelMovable;

            ChkClickThrough.IsChecked = s.ClickThrough;
            ChkAlwaysOnTop.IsChecked = s.AlwaysOnTop;
            ChkLockPosition.IsChecked = s.LockPosition;
            ChkTransparentBg.IsChecked = s.IsBackgroundTransparent;
            ChkDebugMode.IsChecked = s.DebugMode;
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var s = _manager.CurrentSettings;
            
            s.Layout = GetComboText(CmbLayout, s.Layout);
            s.KeyboardPreset = GetComboText(CmbKeyboardPreset, s.KeyboardPreset);
            s.Theme = GetComboText(CmbTheme, s.Theme);
            s.AccentColor = TxtAccentColor.Text;
            UpdateAccentPreview(s.AccentColor);
            s.Scale = SldScale.Value;
            s.Opacity = SldOpacity.Value;
            s.Deadzone = SldDeadzone.Value;
            s.ShowFps = ChkShowFps.IsChecked ?? true;
            s.ShowBallSpeed = ChkShowBallSpeed.IsChecked ?? true;
            s.StatsPanelScale = SldStatsScale.Value;
            s.IsStatsBackgroundTransparent = ChkStatsTransparentBg.IsChecked ?? false;
            s.IsStatsPanelMovable = ChkStatsMovable.IsChecked ?? false;

            if (int.TryParse(TxtFpsPort.Text, out int fpsPort) && fpsPort > 0 && fpsPort <= 65535)
            {
                s.FpsUdpPort = fpsPort;
            }

            if (TryParseDouble(TxtStatsLeft.Text, out double left))
            {
                s.StatsPanelLeft = left;
            }

            if (TryParseDouble(TxtStatsTop.Text, out double top))
            {
                s.StatsPanelTop = top;
            }

            s.ClickThrough = ChkClickThrough.IsChecked ?? false;
            s.AlwaysOnTop = ChkAlwaysOnTop.IsChecked ?? true;
            s.LockPosition = ChkLockPosition.IsChecked ?? false;
            s.IsBackgroundTransparent = ChkTransparentBg.IsChecked ?? true;
            s.DebugMode = ChkDebugMode.IsChecked ?? false;

            _manager.Save();
            _mainWindow.ReloadSettings();
        }

        private void AccentColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string hex)
            {
                TxtAccentColor.Text = hex;
                UpdateAccentPreview(hex);
            }
        }

        private void UpdateAccentPreview(string hex)
        {
            try
            {
                if (ColorConverter.ConvertFromString(hex) is Color color)
                {
                    AccentPreview.Background = new SolidColorBrush(color);
                    return;
                }
            }
            catch
            {
            }

            AccentPreview.Background = Brushes.Transparent;
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static string GetComboText(ComboBox comboBox, string fallback)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && item.Content is string content)
            {
                return content;
            }

            return string.IsNullOrWhiteSpace(comboBox.Text) ? fallback : comboBox.Text;
        }

        private static void SetComboText(ComboBox comboBox, string value)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    comboBoxItem.Content is string content &&
                    string.Equals(content, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            comboBox.Text = value;
        }

        private void GithubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
