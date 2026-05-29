using System;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ControllerOverlay.Settings;

namespace ControllerOverlay
{
    public partial class SettingsWindow : Window
    {
        private SettingsManager _manager;
        private MainWindow _mainWindow;
        private bool _isLoaded;
        private bool _isRefreshingPresetList;
        private bool _isPickingAccent;
        private bool _isUpdatingColorPicker;
        private double _accentHue = 174;
        private double _accentSaturation = 1;
        private double _accentValue = 1;

        private const int ColorWheelSize = 180;

        public SettingsWindow(SettingsManager manager, MainWindow mainWindow)
        {
            InitializeComponent();
            _manager = manager;
            _mainWindow = mainWindow;

            BuildColorWheel();
            PopulateKeyboardPresets();
            LoadSettingsToUI();
            _isLoaded = true;
        }

        private void LoadSettingsToUI()
        {
            var s = _manager.CurrentSettings;
            
            SetComboText(CmbLayout, s.Layout);
            SetComboText(CmbKeyboardPreset, s.KeyboardPreset);
            SetComboText(CmbTheme, s.Theme);
            SetColorPickerFromHex(s.AccentColor);
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
            if (!_isLoaded || _isRefreshingPresetList) return;

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

        private void PopulateKeyboardPresets(string? selectedPreset = null)
        {
            selectedPreset ??= GetComboText(CmbKeyboardPreset, _manager.CurrentSettings.KeyboardPreset);

            _isRefreshingPresetList = true;
            CmbKeyboardPreset.Items.Clear();

            foreach (string preset in KeyboardMouseView.GetAvailablePresetNames())
            {
                CmbKeyboardPreset.Items.Add(preset);
            }

            SetComboText(CmbKeyboardPreset, selectedPreset);
            _isRefreshingPresetList = false;
        }

        private void CmbKeyboardPreset_DropDownOpened(object sender, EventArgs e)
        {
            PopulateKeyboardPresets();
        }

        private void OpenKeyboardFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = KeyboardMouseView.GetUserKeyboardFolder();
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"")
            {
                UseShellExecute = true
            });

            PopulateKeyboardPresets();
        }

        private void ColorWheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPickingAccent = true;
            ColorWheelImage.CaptureMouse();
            PickAccentColor(e.GetPosition(ColorWheelImage));
        }

        private void ColorWheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPickingAccent && e.LeftButton == MouseButtonState.Pressed)
            {
                PickAccentColor(e.GetPosition(ColorWheelImage));
            }
        }

        private void ColorWheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPickingAccent = false;
            ColorWheelImage.ReleaseMouseCapture();
        }

        private void ColorValue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingColorPicker || ColorWheelImage == null || TxtAccentColor == null)
            {
                return;
            }

            _accentValue = e.NewValue;
            BuildColorWheel();
            ApplyAccentFromHsv();
        }

        private void PickAccentColor(Point point)
        {
            double width = ColorWheelImage.ActualWidth > 0 ? ColorWheelImage.ActualWidth : ColorWheelSize;
            double height = ColorWheelImage.ActualHeight > 0 ? ColorWheelImage.ActualHeight : ColorWheelSize;
            double radius = Math.Min(width, height) / 2.0;
            double centerX = width / 2.0;
            double centerY = height / 2.0;
            double dx = point.X - centerX;
            double dy = point.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > radius && distance > 0)
            {
                double factor = radius / distance;
                dx *= factor;
                dy *= factor;
                distance = radius;
            }

            _accentHue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;
            _accentSaturation = Math.Clamp(distance / radius, 0.0, 1.0);
            ApplyAccentFromHsv();
        }

        private void ApplyAccentFromHsv()
        {
            Color color = HsvToRgb(_accentHue, _accentSaturation, _accentValue);
            string hex = ToHex(color);
            TxtAccentColor.Text = hex;
            UpdateAccentPreview(hex);
        }

        private void SetColorPickerFromHex(string hex)
        {
            try
            {
                if (ColorConverter.ConvertFromString(hex) is Color color)
                {
                    RgbToHsv(color, out _accentHue, out _accentSaturation, out _accentValue);

                    _isUpdatingColorPicker = true;
                    SldColorValue.Value = _accentValue;
                    _isUpdatingColorPicker = false;

                    string normalizedHex = ToHex(color);
                    TxtAccentColor.Text = normalizedHex;
                    UpdateAccentPreview(normalizedHex);
                    BuildColorWheel();
                    return;
                }
            }
            catch
            {
            }

            TxtAccentColor.Text = hex;
            UpdateAccentPreview(hex);
            BuildColorWheel();
        }

        private void BuildColorWheel()
        {
            if (ColorWheelImage == null)
            {
                return;
            }

            int stride = ColorWheelSize * 4;
            var pixels = new byte[ColorWheelSize * stride];
            double center = (ColorWheelSize - 1) / 2.0;
            double radius = center;

            for (int y = 0; y < ColorWheelSize; y++)
            {
                for (int x = 0; x < ColorWheelSize; x++)
                {
                    double dx = x - center;
                    double dy = y - center;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    int offset = y * stride + x * 4;

                    if (distance > radius)
                    {
                        pixels[offset + 3] = 0;
                        continue;
                    }

                    double hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;
                    double saturation = Math.Clamp(distance / radius, 0.0, 1.0);
                    Color color = HsvToRgb(hue, saturation, _accentValue);

                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = 255;
                }
            }

            ColorWheelImage.Source = BitmapSource.Create(
                ColorWheelSize,
                ColorWheelSize,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);
        }

        private void UpdateAccentPreview(string hex)
        {
            if (AccentPreview == null)
            {
                return;
            }

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

        private static Color HsvToRgb(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = value - c;

            (double r, double g, double b) = hue switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x)
            };

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        private static void RgbToHsv(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            if (delta == 0)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g)
            {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else
            {
                hue = 60 * (((r - g) / delta) + 4);
            }

            if (hue < 0)
            {
                hue += 360;
            }

            saturation = max == 0 ? 0 : delta / max;
            value = max;
        }

        private static string ToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseDouble(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
                   double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static string GetComboText(ComboBox comboBox, string fallback)
        {
            if (comboBox.SelectedItem is string selected)
            {
                return selected;
            }

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
                if (item is string stringItem &&
                    string.Equals(stringItem, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = stringItem;
                    return;
                }

                if (item is ComboBoxItem comboBoxItem &&
                    comboBoxItem.Content is string content &&
                    string.Equals(content, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                comboBox.Items.Add(value);
                comboBox.SelectedItem = value;
            }
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
