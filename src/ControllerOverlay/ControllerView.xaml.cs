using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ControllerOverlay.Input;
using ControllerOverlay.Settings;

namespace ControllerOverlay
{
    public partial class ControllerView : UserControl
    {
        private AppSettings? _settings;

        private readonly SolidColorBrush _fillY = new((Color)ColorConverter.ConvertFromString("#F1C40F"));
        private readonly SolidColorBrush _fillX = new((Color)ColorConverter.ConvertFromString("#004DFF"));
        private readonly SolidColorBrush _fillB = new((Color)ColorConverter.ConvertFromString("#D9192E"));
        private readonly SolidColorBrush _fillA = new((Color)ColorConverter.ConvertFromString("#00B83F"));
        private readonly SolidColorBrush _fillTriangle = new((Color)ColorConverter.ConvertFromString("#00B83F"));
        private readonly SolidColorBrush _fillSquare = new((Color)ColorConverter.ConvertFromString("#C77DFF"));
        private readonly SolidColorBrush _fillCircle = new((Color)ColorConverter.ConvertFromString("#D9192E"));
        private readonly SolidColorBrush _fillCross = new((Color)ColorConverter.ConvertFromString("#004DFF"));

        private readonly SolidColorBrush _transparent = Brushes.Transparent;
        private readonly SolidColorBrush _white = new((Color)ColorConverter.ConvertFromString("#E0E0E0"));
        private readonly SolidColorBrush _darkBg = new((Color)ColorConverter.ConvertFromString("#000000")) { Opacity = 0.72 };

        private readonly DropShadowEffect _glowY;
        private readonly DropShadowEffect _glowX;
        private readonly DropShadowEffect _glowB;
        private readonly DropShadowEffect _glowA;
        private readonly DropShadowEffect _glowWhite;

        private SolidColorBrush _curTop = Brushes.White;
        private SolidColorBrush _curLeft = Brushes.White;
        private SolidColorBrush _curRight = Brushes.White;
        private SolidColorBrush _curBottom = Brushes.White;
        private DropShadowEffect? _curGlowTop;
        private DropShadowEffect? _curGlowLeft;
        private DropShadowEffect? _curGlowRight;
        private DropShadowEffect? _curGlowBottom;

        private string _currentLayout = "Xbox";

        public ControllerView()
        {
            InitializeComponent();
            _glowY = MakeGlow("#F1C40F");
            _glowX = MakeGlow("#004DFF");
            _glowB = MakeGlow("#D9192E");
            _glowA = MakeGlow("#00B83F");
            _glowWhite = MakeGlow("#E0E0E0");
            ApplyLayoutColors();
            ApplyVisualTheme("Neon");
        }

        private static DropShadowEffect MakeGlow(string hex)
        {
            return new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(hex),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.9
            };
        }

        public void ApplySettings(AppSettings settings)
        {
            _settings = settings;
            SetBackground(settings.IsBackgroundTransparent);
            if (!string.Equals(settings.Layout, "Auto", System.StringComparison.OrdinalIgnoreCase))
            {
                _currentLayout = settings.Layout;
            }

            ApplyLayoutColors();
            ApplyVisualTheme(settings.Theme);
        }

        public void SetBackground(bool transparent)
        {
            RootBorder.Background = transparent ? _transparent : _darkBg;
        }

        public void AutoDetectLayout(string controllerName)
        {
            if (_settings != null && _settings.Layout != "Auto")
            {
                return;
            }

            string detected = IsPlayStationController(controllerName) ? "PlayStation" : "Xbox";
            if (_currentLayout != detected)
            {
                _currentLayout = detected;
                Dispatcher.InvokeAsync(ApplyLayoutColors);
            }
        }

        private void ApplyLayoutColors()
        {
            if (string.Equals(_currentLayout, "PlayStation", System.StringComparison.OrdinalIgnoreCase))
            {
                Lbl_Y.Text = "\u25B3";
                Lbl_X.Text = "\u25A1";
                Lbl_B.Text = "\u25CB";
                Lbl_A.Text = "X";

                Btn_Y.Stroke = _fillTriangle;
                Lbl_Y.Foreground = _fillTriangle;
                Btn_X.Stroke = _fillSquare;
                Lbl_X.Foreground = _fillSquare;
                Btn_B.Stroke = _fillCircle;
                Lbl_B.Foreground = _fillCircle;
                Btn_A.Stroke = _fillCross;
                Lbl_A.Foreground = _fillCross;

                _curTop = _fillTriangle;
                _curLeft = _fillSquare;
                _curRight = _fillCircle;
                _curBottom = _fillCross;

                Lbl_LT.Text = "L2";
                Lbl_RT.Text = "R2";
                Lbl_LB.Text = "L1";
                Lbl_RB.Text = "R1";
            }
            else
            {
                Lbl_Y.Text = "Y";
                Lbl_X.Text = "X";
                Lbl_B.Text = "B";
                Lbl_A.Text = "A";

                Btn_Y.Stroke = _fillY;
                Lbl_Y.Foreground = _fillY;
                Btn_X.Stroke = _fillX;
                Lbl_X.Foreground = _fillX;
                Btn_B.Stroke = _fillB;
                Lbl_B.Foreground = _fillB;
                Btn_A.Stroke = _fillA;
                Lbl_A.Foreground = _fillA;

                _curTop = _fillY;
                _curLeft = _fillX;
                _curRight = _fillB;
                _curBottom = _fillA;

                Lbl_LT.Text = "LT";
                Lbl_RT.Text = "RT";
                Lbl_LB.Text = "LB";
                Lbl_RB.Text = "RB";
            }

            _curGlowTop = null;
            _curGlowLeft = null;
            _curGlowRight = null;
            _curGlowBottom = null;
        }

        private void ApplyVisualTheme(string? theme)
        {
            string normalized = (theme ?? "Neon").Trim().ToLowerInvariant();
            bool clean = normalized == "limpo" || normalized == "clean";
            bool dark = normalized == "preto" || normalized == "dark" || normalized == "black";

            var outlineColor = clean
                ? (Color)ColorConverter.ConvertFromString("#F8FAFF")
                : dark
                    ? (Color)ColorConverter.ConvertFromString("#FFFFFF")
                    : (Color)ColorConverter.ConvertFromString("#E8EEF7");

            _white.Color = outlineColor;
            _darkBg.Opacity = dark ? 0.88 : clean ? 0.56 : 0.72;

            double stroke = clean ? 1.4 : 1.6;
            double stickStroke = clean ? 3.2 : 3.5;
            byte fillAlpha = (byte)(dark ? 38 : clean ? 8 : 18);
            var idleFill = new SolidColorBrush(Color.FromArgb(fillAlpha, outlineColor.R, outlineColor.G, outlineColor.B));

            foreach (var border in new[] { LT_Outline, LB_Outline, RT_Outline, RB_Outline })
            {
                border.BorderBrush = _white;
                border.BorderThickness = new Thickness(stroke);
                border.Background = idleFill;
            }

            LT_Fill.Background = _white;
            LB_Fill.Background = _white;
            RT_Fill.Background = _white;
            RB_Fill.Background = _white;

            Lbl_LT.Foreground = _white;
            Lbl_LB.Foreground = _white;
            Lbl_RT.Foreground = _white;
            Lbl_RB.Foreground = _white;

            LeftStickBase.Stroke = _white;
            LeftStickBase.StrokeThickness = stickStroke;
            LeftStick.Fill = new SolidColorBrush(clean
                ? (Color)ColorConverter.ConvertFromString("#D9DCE3")
                : dark
                    ? (Color)ColorConverter.ConvertFromString("#BFC4CE")
                    : (Color)ColorConverter.ConvertFromString("#C5C8CF"));
            RootBorder.CornerRadius = dark ? new CornerRadius(6) : new CornerRadius(0);
        }

        private static bool IsPlayStationController(string controllerName)
        {
            string lower = controllerName.ToLowerInvariant();
            return lower.Contains("dualsense") ||
                   lower.Contains("dualshock") ||
                   lower.Contains("wireless controller") ||
                   lower.Contains("sony") ||
                   lower.Contains("ps5") ||
                   lower.Contains("ps4");
        }

        public void UpdateState(ControllerState state)
        {
            Dispatcher.InvokeAsync(() =>
            {
                SetFaceBtn(Btn_Y, Lbl_Y, state.Y, _curTop, _curGlowTop);
                SetFaceBtn(Btn_X, Lbl_X, state.X, _curLeft, _curGlowLeft);
                SetFaceBtn(Btn_B, Lbl_B, state.B, _curRight, _curGlowRight);
                SetFaceBtn(Btn_A, Lbl_A, state.A, _curBottom, _curGlowBottom);

                LT_Fill.Opacity = state.L2;
                RT_Fill.Opacity = state.R2;
                Lbl_LT.Foreground = state.L2 > 0.5 ? Brushes.Black : _white;
                Lbl_RT.Foreground = state.R2 > 0.5 ? Brushes.Black : _white;

                LB_Fill.Opacity = state.L1 ? 1.0 : 0.0;
                RB_Fill.Opacity = state.R1 ? 1.0 : 0.0;
                Lbl_LB.Foreground = state.L1 ? Brushes.Black : _white;
                Lbl_RB.Foreground = state.R1 ? Brushes.Black : _white;

                LeftStickBase.Stroke = state.L3 ? _fillX : _white;
                LeftStickBase.Effect = null;
                LeftStickTranslate.X = state.LeftStickX * 9;
                LeftStickTranslate.Y = state.LeftStickY * 9;
            });
        }

        private void SetFaceBtn(System.Windows.Shapes.Ellipse btn, TextBlock lbl, bool pressed,
            SolidColorBrush color, DropShadowEffect? glow)
        {
            if (pressed)
            {
                btn.Fill = color;
                btn.Effect = null;
                lbl.Foreground = Brushes.Black;
            }
            else
            {
                btn.Fill = _transparent;
                btn.Effect = null;
                lbl.Foreground = color;
            }
        }
    }
}
