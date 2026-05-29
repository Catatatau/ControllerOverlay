using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControllerOverlay.Input;
using ControllerOverlay.Settings;

namespace ControllerOverlay
{
    public partial class KeyboardMouseView : UserControl
    {
        private readonly List<KeyVisual> _keyVisuals = new();

        private SolidColorBrush _activeBrush = new(Color.FromRgb(0, 255, 204));
        private SolidColorBrush _inactiveBrush = new(Color.FromArgb(12, 255, 255, 255));
        private SolidColorBrush _outlineBrush = new(Color.FromRgb(232, 238, 247));
        private SolidColorBrush _textBrush = new(Color.FromRgb(232, 238, 247));
        private string _currentPreset = string.Empty;

        public double OverlayWidth { get; private set; } = 270;
        public double OverlayHeight { get; private set; } = 150;

        public KeyboardMouseView()
        {
            InitializeComponent();
            BuildPreset("FPS Compacto");
        }

        public void ApplySettings(AppSettings settings)
        {
            _activeBrush = TryMakeBrush(settings.AccentColor, Color.FromRgb(0, 255, 204));

            bool dark = string.Equals(settings.Theme, "Preto", StringComparison.OrdinalIgnoreCase);
            bool clean = string.Equals(settings.Theme, "Limpo", StringComparison.OrdinalIgnoreCase);

            _outlineBrush = new SolidColorBrush(dark
                ? Color.FromRgb(255, 255, 255)
                : clean
                    ? Color.FromRgb(248, 250, 255)
                    : Color.FromRgb(232, 238, 247));
            _textBrush = new SolidColorBrush(dark ? Color.FromRgb(250, 250, 250) : Color.FromRgb(232, 238, 247));
            _inactiveBrush = new SolidColorBrush(Color.FromArgb((byte)(dark ? 30 : clean ? 8 : 16), 255, 255, 255));

            RootBorder.Background = settings.IsBackgroundTransparent
                ? Brushes.Transparent
                : new SolidColorBrush(Color.FromArgb((byte)(dark ? 190 : 120), 10, 12, 18));
            RootBorder.CornerRadius = settings.IsBackgroundTransparent ? new CornerRadius(0) : new CornerRadius(10);
            RootBorder.BorderBrush = settings.IsBackgroundTransparent
                ? Brushes.Transparent
                : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            RootBorder.BorderThickness = settings.IsBackgroundTransparent ? new Thickness(0) : new Thickness(1);
            RootBorder.Padding = settings.IsBackgroundTransparent ? new Thickness(0) : new Thickness(7);

            BuildPreset(settings.KeyboardPreset);
            ApplyIdleStyle();
        }

        public void UpdateState(KeyboardMouseState state)
        {
            foreach (KeyVisual visual in _keyVisuals)
            {
                bool pressed = visual.Keys.Any(state.IsPressed);
                visual.Border.Background = pressed ? _activeBrush : _inactiveBrush;
                visual.Label.Foreground = pressed ? Brushes.Black : _textBrush;
                visual.Border.Opacity = pressed ? 1.0 : 0.92;
            }
        }

        private void BuildPreset(string presetName)
        {
            string normalized = NormalizePreset(presetName);
            if (string.Equals(normalized, _currentPreset, StringComparison.OrdinalIgnoreCase) && _keyVisuals.Count > 0)
            {
                return;
            }

            KeyboardPreset preset = normalized switch
            {
                "WASD + Mouse" => WasdMousePreset(),
                "FPS Completo" => FullFpsPreset(),
                "Rocket League" => RocketLeaguePreset(),
                "Setas + Mouse" => ArrowsMousePreset(),
                "Numpad" => NumpadPreset(),
                _ => CompactFpsPreset()
            };

            _currentPreset = preset.Name;
            OverlayWidth = preset.Width;
            OverlayHeight = preset.Height;
            KeyboardCanvas.Width = preset.Width;
            KeyboardCanvas.Height = preset.Height;
            KeyboardCanvas.Children.Clear();
            _keyVisuals.Clear();

            foreach (KeySpec key in preset.Keys)
            {
                var border = new Border
                {
                    Width = key.Width,
                    Height = key.Height,
                    CornerRadius = new CornerRadius(key.IsMouse ? 9 : 4),
                    BorderThickness = new Thickness(key.IsMouse ? 1.5 : 1.6),
                    Child = new TextBlock
                    {
                        Text = key.Label,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = key.FontSize,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                Canvas.SetLeft(border, key.X);
                Canvas.SetTop(border, key.Y);
                KeyboardCanvas.Children.Add(border);
                _keyVisuals.Add(new KeyVisual(border, (TextBlock)border.Child, key.VirtualKeys));
            }
        }

        private void ApplyIdleStyle()
        {
            foreach (KeyVisual visual in _keyVisuals)
            {
                visual.Border.BorderBrush = _outlineBrush;
                visual.Border.Background = _inactiveBrush;
                visual.Label.Foreground = _textBrush;
            }
        }

        private static string NormalizePreset(string? preset)
        {
            string value = string.IsNullOrWhiteSpace(preset) ? "FPS Compacto" : preset.Trim();
            return value.ToLowerInvariant() switch
            {
                "compact" or "compacto" or "fps compacto" => "FPS Compacto",
                "wasd" or "wasd mouse" or "wasd + mouse" => "WASD + Mouse",
                "fps" or "fps completo" or "completo" => "FPS Completo",
                "rocket" or "rocket league" or "rl" => "Rocket League",
                "setas" or "setas + mouse" or "arrows" => "Setas + Mouse",
                "numpad" or "numerico" or "teclado numerico" => "Numpad",
                _ => "FPS Compacto"
            };
        }

        private static SolidColorBrush TryMakeBrush(string hex, Color fallback)
        {
            try
            {
                if (ColorConverter.ConvertFromString(hex) is Color color)
                {
                    return new SolidColorBrush(color);
                }
            }
            catch
            {
            }

            return new SolidColorBrush(fallback);
        }

        private static KeyboardPreset CompactFpsPreset()
        {
            var keys = new List<KeySpec>
            {
                Key("Q", Vk.Q, 38, 4), Key("W", Vk.W, 66, 4), Key("E", Vk.E, 94, 4), Key("R", Vk.R, 122, 4),
                Key("A", Vk.A, 44, 32), Key("S", Vk.S, 72, 32), Key("D", Vk.D, 100, 32), Key("F", Vk.F, 128, 32),
                Key("Shift", Vk.Shift, 0, 60, 42, 26, 10), Key("Z", Vk.Z, 44, 60), Key("X", Vk.X, 72, 60),
                Key("C", Vk.C, 100, 60), Key("V", Vk.V, 128, 60),
                Key("Ctrl", Vk.Control, 0, 88, 42, 26, 10), Key("Space", Vk.Space, 44, 88, 112, 26, 10),
                Mouse("LMB", Vk.LButton, 190, 22, 38, 30), Mouse("RMB", Vk.RButton, 230, 22, 38, 30),
                Mouse("M4", Vk.XButton1, 190, 56, 38, 25), Mouse("M5", Vk.XButton2, 230, 56, 38, 25)
            };

            return new KeyboardPreset("FPS Compacto", 276, 120, keys);
        }

        private static KeyboardPreset WasdMousePreset()
        {
            var keys = new List<KeySpec>
            {
                Key("W", Vk.W, 48, 0, 44, 38, 15),
                Key("A", Vk.A, 0, 40, 44, 38, 15), Key("S", Vk.S, 48, 40, 44, 38, 15), Key("D", Vk.D, 96, 40, 44, 38, 15),
                Key("Shift", Vk.Shift, 0, 82, 68, 32, 11), Key("Space", Vk.Space, 72, 82, 68, 32, 11),
                Mouse("LMB", Vk.LButton, 166, 24, 48, 38, 12), Mouse("RMB", Vk.RButton, 218, 24, 48, 38, 12),
                Mouse("M4", Vk.XButton1, 166, 66, 48, 30, 11), Mouse("M5", Vk.XButton2, 218, 66, 48, 30, 11)
            };

            return new KeyboardPreset("WASD + Mouse", 276, 118, keys);
        }

        private static KeyboardPreset FullFpsPreset()
        {
            var keys = new List<KeySpec>();
            int x = 54;
            foreach ((string label, int key) in new[] { ("1", Vk.D1), ("2", Vk.D2), ("3", Vk.D3), ("4", Vk.D4), ("5", Vk.D5), ("6", Vk.D6) })
            {
                keys.Add(Key(label, key, x, 0));
                x += 34;
            }

            keys.AddRange(new[]
            {
                Key("Tab", Vk.Tab, 0, 36, 50, 28, 10), Key("Q", Vk.Q, 54, 36), Key("W", Vk.W, 88, 36),
                Key("E", Vk.E, 122, 36), Key("R", Vk.R, 156, 36), Key("T", Vk.T, 190, 36),
                Key("Caps", Vk.CapsLock, 0, 70, 58, 28, 10), Key("A", Vk.A, 62, 70), Key("S", Vk.S, 96, 70),
                Key("D", Vk.D, 130, 70), Key("F", Vk.F, 164, 70), Key("G", Vk.G, 198, 70),
                Key("Shift", Vk.Shift, 0, 104, 70, 28, 10), Key("Z", Vk.Z, 74, 104), Key("X", Vk.X, 108, 104),
                Key("C", Vk.C, 142, 104), Key("V", Vk.V, 176, 104), Key("B", Vk.B, 210, 104),
                Key("Ctrl", Vk.Control, 0, 138, 52, 28, 10), Key("Alt", Vk.Alt, 56, 138, 46, 28, 10),
                Key("Space", Vk.Space, 106, 138, 138, 28, 10),
                Mouse("LMB", Vk.LButton, 284, 36, 52, 34, 12), Mouse("RMB", Vk.RButton, 340, 36, 52, 34, 12),
                Mouse("MMB", Vk.MButton, 312, 74, 52, 28, 11), Mouse("M4", Vk.XButton1, 284, 106, 52, 28, 11),
                Mouse("M5", Vk.XButton2, 340, 106, 52, 28, 11)
            });

            return new KeyboardPreset("FPS Completo", 410, 170, keys);
        }

        private static KeyboardPreset RocketLeaguePreset()
        {
            var keys = new List<KeySpec>
            {
                Key("W", Vk.W, 58, 0, 44, 34, 14), Key("Q", Vk.Q, 8, 16, 40, 30, 13), Key("E", Vk.E, 112, 16, 40, 30, 13),
                Key("A", Vk.A, 8, 52, 44, 34, 14), Key("S", Vk.S, 58, 52, 44, 34, 14), Key("D", Vk.D, 108, 52, 44, 34, 14),
                Key("Shift", Vk.Shift, 0, 94, 58, 30, 10), Key("Ctrl", Vk.Control, 62, 94, 54, 30, 10),
                Key("Space", Vk.Space, 120, 94, 92, 30, 10), Key("R", Vk.R, 162, 16, 40, 30, 13), Key("F", Vk.F, 214, 16, 40, 30, 13),
                Mouse("LMB", Vk.LButton, 244, 58, 48, 30, 11), Mouse("RMB", Vk.RButton, 296, 58, 48, 30, 11)
            };

            return new KeyboardPreset("Rocket League", 352, 128, keys);
        }

        private static KeyboardPreset ArrowsMousePreset()
        {
            var keys = new List<KeySpec>
            {
                Key("Up", Vk.Up, 58, 0, 48, 34, 11),
                Key("Left", Vk.Left, 6, 38, 48, 34, 10), Key("Down", Vk.Down, 58, 38, 48, 34, 10), Key("Right", Vk.Right, 110, 38, 52, 34, 10),
                Key("Shift", Vk.Shift, 6, 80, 66, 30, 10), Key("Ctrl", Vk.Control, 76, 80, 54, 30, 10), Key("Space", Vk.Space, 134, 80, 74, 30, 10),
                Mouse("LMB", Vk.LButton, 226, 20, 44, 34, 11), Mouse("RMB", Vk.RButton, 274, 20, 44, 34, 11),
                Mouse("M4", Vk.XButton1, 226, 58, 44, 28, 11), Mouse("M5", Vk.XButton2, 274, 58, 44, 28, 11)
            };

            return new KeyboardPreset("Setas + Mouse", 326, 116, keys);
        }

        private static KeyboardPreset NumpadPreset()
        {
            var keys = new List<KeySpec>
            {
                Key("7", Vk.Num7, 0, 0), Key("8", Vk.Num8, 38, 0), Key("9", Vk.Num9, 76, 0), Key("/", Vk.Divide, 114, 0),
                Key("4", Vk.Num4, 0, 36), Key("5", Vk.Num5, 38, 36), Key("6", Vk.Num6, 76, 36), Key("*", Vk.Multiply, 114, 36),
                Key("1", Vk.Num1, 0, 72), Key("2", Vk.Num2, 38, 72), Key("3", Vk.Num3, 76, 72), Key("-", Vk.Subtract, 114, 72),
                Key("0", Vk.Num0, 0, 108, 74, 32, 13), Key(".", Vk.Decimal, 76, 108), Key("+", Vk.Add, 114, 108),
                Key("Enter", Vk.Enter, 152, 72, 56, 68, 10)
            };

            return new KeyboardPreset("Numpad", 214, 144, keys);
        }

        private static KeySpec Key(string label, int virtualKey, double x, double y, double width = 30, double height = 28, double fontSize = 12)
        {
            return new KeySpec(label, new[] { virtualKey }, x, y, width, height, fontSize, false);
        }

        private static KeySpec Mouse(string label, int virtualKey, double x, double y, double width, double height, double fontSize = 11)
        {
            return new KeySpec(label, new[] { virtualKey }, x, y, width, height, fontSize, true);
        }

        private sealed class KeyboardPreset
        {
            public KeyboardPreset(string name, double width, double height, IReadOnlyList<KeySpec> keys)
            {
                Name = name;
                Width = width;
                Height = height;
                Keys = keys;
            }

            public string Name { get; }
            public double Width { get; }
            public double Height { get; }
            public IReadOnlyList<KeySpec> Keys { get; }
        }

        private sealed class KeySpec
        {
            public KeySpec(string label, IReadOnlyList<int> virtualKeys, double x, double y, double width, double height, double fontSize, bool isMouse)
            {
                Label = label;
                VirtualKeys = virtualKeys;
                X = x;
                Y = y;
                Width = width;
                Height = height;
                FontSize = fontSize;
                IsMouse = isMouse;
            }

            public string Label { get; }
            public IReadOnlyList<int> VirtualKeys { get; }
            public double X { get; }
            public double Y { get; }
            public double Width { get; }
            public double Height { get; }
            public double FontSize { get; }
            public bool IsMouse { get; }
        }

        private sealed class KeyVisual
        {
            public KeyVisual(Border border, TextBlock label, IReadOnlyList<int> keys)
            {
                Border = border;
                Label = label;
                Keys = keys;
            }

            public Border Border { get; }
            public TextBlock Label { get; }
            public IReadOnlyList<int> Keys { get; }
        }

        private static class Vk
        {
            public const int LButton = 0x01;
            public const int RButton = 0x02;
            public const int MButton = 0x04;
            public const int XButton1 = 0x05;
            public const int XButton2 = 0x06;
            public const int Tab = 0x09;
            public const int Enter = 0x0D;
            public const int Shift = 0x10;
            public const int Control = 0x11;
            public const int Alt = 0x12;
            public const int CapsLock = 0x14;
            public const int Space = 0x20;
            public const int Left = 0x25;
            public const int Up = 0x26;
            public const int Right = 0x27;
            public const int Down = 0x28;
            public const int D1 = 0x31;
            public const int D2 = 0x32;
            public const int D3 = 0x33;
            public const int D4 = 0x34;
            public const int D5 = 0x35;
            public const int D6 = 0x36;
            public const int A = 0x41;
            public const int B = 0x42;
            public const int C = 0x43;
            public const int D = 0x44;
            public const int E = 0x45;
            public const int F = 0x46;
            public const int G = 0x47;
            public const int Q = 0x51;
            public const int R = 0x52;
            public const int S = 0x53;
            public const int T = 0x54;
            public const int V = 0x56;
            public const int W = 0x57;
            public const int X = 0x58;
            public const int Z = 0x5A;
            public const int Num0 = 0x60;
            public const int Num1 = 0x61;
            public const int Num2 = 0x62;
            public const int Num3 = 0x63;
            public const int Num4 = 0x64;
            public const int Num5 = 0x65;
            public const int Num6 = 0x66;
            public const int Num7 = 0x67;
            public const int Num8 = 0x68;
            public const int Num9 = 0x69;
            public const int Multiply = 0x6A;
            public const int Add = 0x6B;
            public const int Subtract = 0x6D;
            public const int Decimal = 0x6E;
            public const int Divide = 0x6F;
        }
    }
}
