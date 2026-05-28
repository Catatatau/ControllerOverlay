using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ControllerOverlay.Hotkeys;
using ControllerOverlay.Input;
using ControllerOverlay.Overlay;
using ControllerOverlay.Settings;
using ControllerOverlay.Telemetry;

namespace ControllerOverlay
{
    public partial class MainWindow : Window
    {
        private SettingsManager _settingsManager = null!;
        private ControllerManager _controllerManager = null!;
        private GlobalHotkeyService _hotkeyService = null!;
        private OverlayBehavior _overlayBehavior = null!;
        private OverlayBehavior? _fpsOverlayBehavior;
        private SettingsWindow? _settingsWindow;
        private FpsHudWindow? _fpsWindow;
        private RocketLeagueStatsApiService _gameStatsService = null!;
        private EtwGameFpsReader _etwFpsReader = null!;
        private KeyboardMouseManager _kbmManager = null!;

        private DateTime _lastStatsUpdate = DateTime.MinValue;
        private int _activeStatsApiPort;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _settingsManager = new SettingsManager();
            _settingsManager.Load();

            _overlayBehavior = new OverlayBehavior(this);
            _gameStatsService = new RocketLeagueStatsApiService();
            _etwFpsReader = new EtwGameFpsReader();

            EnsureFpsWindow();

            _hotkeyService = new GlobalHotkeyService(this);
            _hotkeyService.RegisterHotkey(6, 0x4F, ToggleOverlay); // Ctrl+Shift+O
            _hotkeyService.RegisterHotkey(6, 0x43, ToggleSettings); // Ctrl+Shift+C

            _controllerManager = new ControllerManager();
            _controllerManager.SetWindowHandle(new WindowInteropHelper(this).Handle);
            _controllerManager.SetDeadzone(_settingsManager.CurrentSettings.Deadzone);
            _controllerManager.StateUpdated += ControllerManager_StateUpdated;
            _controllerManager.Start();

            _kbmManager = new KeyboardMouseManager();
            _kbmManager.StateUpdated += KeyboardMouseManager_StateUpdated;
            _kbmManager.Start();

            ApplySettings();
        }

        private void EnsureFpsWindow()
        {
            if (_fpsWindow != null && _fpsWindow.IsLoaded)
            {
                return;
            }

            var settings = _settingsManager.CurrentSettings;
            _fpsWindow = new FpsHudWindow
            {
                Left = settings.StatsPanelLeft,
                Top = settings.StatsPanelTop
            };
            _fpsWindow.LocationChanged += FpsWindow_LocationChanged;
            _fpsWindow.Closed += FpsWindow_Closed;
            _fpsWindow.Show();

            _fpsOverlayBehavior = new OverlayBehavior(_fpsWindow);
        }

        private void FpsWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (_fpsWindow == null || _settingsManager == null)
            {
                return;
            }

            var settings = _settingsManager.CurrentSettings;
            settings.StatsPanelLeft = _fpsWindow.Left;
            settings.StatsPanelTop = _fpsWindow.Top;
            _settingsManager.Save();
        }

        private void FpsWindow_Closed(object? sender, EventArgs e)
        {
            _fpsWindow = null;
            _fpsOverlayBehavior = null;
        }

        private void ControllerManager_StateUpdated()
        {
            var state = _controllerManager.CurrentState.Snapshot();

            Dispatcher.InvokeAsync(() =>
            {
                if (state.IsConnected)
                {
                    CtrlView.AutoDetectLayout(state.ControllerName);
                }

                CtrlView.UpdateState(state);
                UpdateStatsHud();

                if (_settingsManager.CurrentSettings.DebugMode)
                {
                    DebugText.Text = $"Status: {(state.IsConnected ? "Connected" : "Disconnected")}\n" +
                                     $"Controller: {state.ControllerName}\n" +
                                     $"Layout: {state.Layout}\n" +
                                     $"Axes: L({state.LeftStickX:F2},{state.LeftStickY:F2}) R({state.RightStickX:F2},{state.RightStickY:F2})\n" +
                                     $"Triggers: L({state.L2:F2}) R({state.R2:F2})";
                }
            });
        }

        private void KeyboardMouseManager_StateUpdated()
        {
            var state = _kbmManager.CurrentState;
            Dispatcher.InvokeAsync(() =>
            {
                KbmView.UpdateState(state);
            });
        }

        private void ApplySettings()
        {
            var settings = _settingsManager.CurrentSettings;
            Topmost = settings.AlwaysOnTop;
            Opacity = settings.Opacity;
            CtrlView.RenderTransform = new System.Windows.Media.ScaleTransform(settings.Scale, settings.Scale);
            CtrlView.ApplySettings(settings);
            KbmView.RenderTransform = new System.Windows.Media.ScaleTransform(settings.Scale, settings.Scale);
            KbmView.ApplySettings(settings);
            if (settings.Layout == "Teclado/Mouse")
            {
                Width = 300 * settings.Scale;
                Height = 180 * settings.Scale;
                CtrlView.Visibility = Visibility.Collapsed;
                KbmView.Visibility = Visibility.Visible;
            }
            else
            {
                Width = 270 * settings.Scale;
                Height = 175 * settings.Scale;
                CtrlView.Visibility = Visibility.Visible;
                KbmView.Visibility = Visibility.Collapsed;
            }

            _overlayBehavior.SetClickThrough(settings.ClickThrough);
            DebugText.Visibility = settings.DebugMode ? Visibility.Visible : Visibility.Collapsed;

            EnsureFpsWindow();
            bool allowStatsDragging = !settings.LockPosition && settings.IsStatsPanelMovable;
            if (_fpsWindow != null)
            {
                _fpsWindow.Topmost = settings.AlwaysOnTop;
                _fpsWindow.Opacity = settings.Opacity;
                _fpsWindow.AllowDragging = allowStatsDragging || (!settings.ClickThrough && !settings.LockPosition);
                _fpsWindow.Left = settings.StatsPanelLeft;
                _fpsWindow.Top = settings.StatsPanelTop;
                _fpsWindow.ApplyAppearance(settings.IsStatsBackgroundTransparent, settings.StatsPanelScale);
                _fpsWindow.SetMetricVisibility(settings.ShowFps, settings.ShowBallSpeed);
            }

            _fpsOverlayBehavior?.SetClickThrough(settings.ClickThrough && !allowStatsDragging);

            ConfigureGameStatsApi(settings);
            UpdateStatsHud(force: true);
        }

        private void ConfigureGameStatsApi(AppSettings settings)
        {
            if (!settings.ShowFps && !settings.ShowBallSpeed)
            {
                _gameStatsService.Stop();
                return;
            }

            if (_activeStatsApiPort != settings.FpsUdpPort || !_gameStatsService.IsRunning)
            {
                _activeStatsApiPort = settings.FpsUdpPort;
                _gameStatsService.Start(settings.FpsUdpPort);
            }

            if (settings.ShowFps)
            {
                if (!_etwFpsReader.IsRunning && !_etwFpsReader.RequiresAdministrator)
                {
                    _etwFpsReader.Start("RocketLeague.exe");
                }
            }
            else
            {
                _etwFpsReader.Stop();
            }
        }

        private void UpdateStatsHud(bool force = false)
        {
            if (_fpsWindow == null || !_fpsWindow.IsLoaded)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (!force && (now - _lastStatsUpdate).TotalMilliseconds < 120)
            {
                return;
            }

            _lastStatsUpdate = now;
            Dispatcher.InvokeAsync(() =>
            {
                var settings = _settingsManager.CurrentSettings;
                string fpsText = settings.ShowFps ? FormatGameFps(settings) : "FPS: N/D";
                string ballText = settings.ShowBallSpeed ? FormatBallSpeed() : "Bola: N/D";

                _fpsWindow.SetMetricValues(fpsText, ballText);
                _fpsWindow.SetMetricVisibility(settings.ShowFps, settings.ShowBallSpeed);

                bool shouldShow = (settings.ShowFps || settings.ShowBallSpeed) && Visibility == Visibility.Visible;
                if (shouldShow)
                {
                    if (_fpsWindow.Visibility != Visibility.Visible)
                    {
                        _fpsWindow.Show();
                    }
                }
                else if (_fpsWindow.Visibility == Visibility.Visible)
                {
                    _fpsWindow.Hide();
                }
            });
        }

        private string FormatGameFps(AppSettings settings)
        {
            if (RtssFpsReader.TryGetGameFps("RocketLeague.exe", out double rtssFps, out _))
            {
                return $"FPS: {rtssFps:0}";
            }

            double? etwFps = _etwFpsReader.GameFps;
            if (etwFps.HasValue &&
                (DateTime.UtcNow - _etwFpsReader.LastUpdateUtc).TotalSeconds <= 6.0)
            {
                return $"FPS: {etwFps.Value:0}";
            }

            if (_gameStatsService.GameFps.HasValue &&
                (DateTime.UtcNow - _gameStatsService.FpsLastUpdateUtc).TotalSeconds <= 2.0)
            {
                return $"FPS: {_gameStatsService.GameFps.Value:0}";
            }

            if (_etwFpsReader.RequiresAdministrator)
            {
                return "FPS: admin";
            }

            return "FPS: N/D";
        }

        private string FormatBallSpeed()
        {
            if (_gameStatsService.BallSpeedUus.HasValue &&
                (DateTime.UtcNow - _gameStatsService.BallSpeedLastUpdateUtc).TotalSeconds <= 2.0)
            {
                return $"Bola: {_gameStatsService.BallSpeedUus.Value:0} km/h";
            }

            return "Bola: N/D";
        }

        private void ToggleOverlay()
        {
            if (Visibility == Visibility.Visible)
            {
                Visibility = Visibility.Hidden;
                _fpsWindow?.Hide();
            }
            else
            {
                Visibility = Visibility.Visible;
                UpdateStatsHud(force: true);
            }
        }

        private void ToggleClickThrough()
        {
            var s = _settingsManager.CurrentSettings;
            s.ClickThrough = !s.ClickThrough;
            _settingsManager.Save();
            ApplySettings();
        }

        private void ToggleSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(_settingsManager, this);
                _settingsWindow.Owner = this;
                _settingsWindow.Topmost = true;
                _settingsWindow.Show();
            }
            else
            {
                if (_settingsWindow.WindowState == WindowState.Minimized)
                {
                    _settingsWindow.WindowState = WindowState.Normal;
                }

                _settingsWindow.Activate();
            }
        }

        public void ReloadSettings()
        {
            _controllerManager.SetDeadzone(_settingsManager.CurrentSettings.Deadzone);
            ApplySettings();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !_settingsManager.CurrentSettings.LockPosition)
            {
                DragMove();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _controllerManager?.Dispose();
            _kbmManager?.Dispose();
            _hotkeyService?.Dispose();
            _gameStatsService?.Dispose();
            _etwFpsReader?.Dispose();
            _fpsWindow?.Close();
            Application.Current.Shutdown();
        }
    }
}
