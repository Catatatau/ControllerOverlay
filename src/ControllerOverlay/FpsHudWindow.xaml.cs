using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ControllerOverlay
{
    public partial class FpsHudWindow : Window
    {
        public bool AllowDragging { get; set; } = true;

        public FpsHudWindow()
        {
            InitializeComponent();
        }

        public void ApplyAppearance(bool transparentBackground, double scale)
        {
            double safeScale = scale < 0.5 ? 0.5 : scale > 3.0 ? 3.0 : scale;
            HudPanel.Background = transparentBackground
                ? Brushes.Transparent
                : new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0));
            HudPanel.Padding = transparentBackground
                ? new Thickness(0)
                : new Thickness(8 * safeScale, 6 * safeScale, 8 * safeScale, 6 * safeScale);

            FpsText.FontSize = 13 * safeScale;
            BallSpeedText.FontSize = 13 * safeScale;
            BallSpeedText.Margin = new Thickness(0, 3 * safeScale, 0, 0);
        }

        public void SetMetricVisibility(bool showFps, bool showBallSpeed)
        {
            FpsText.Visibility = showFps ? Visibility.Visible : Visibility.Collapsed;
            BallSpeedText.Visibility = showBallSpeed ? Visibility.Visible : Visibility.Collapsed;
            Visibility = showFps || showBallSpeed ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetMetricValues(string fps, string ballSpeed)
        {
            FpsText.Text = fps;
            BallSpeedText.Text = ballSpeed;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AllowDragging || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            DragMove();
        }
    }
}
