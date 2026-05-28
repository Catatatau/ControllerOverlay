using System;

namespace ControllerOverlay.Settings
{
    public class AppSettings
    {
        public bool ClickThrough { get; set; } = false;
        public bool AlwaysOnTop { get; set; } = true;
        public string Theme { get; set; } = "Neon";
        public string Layout { get; set; } = "Auto"; // Auto, Xbox, PlayStation, Generic
        public double Opacity { get; set; } = 1.0;
        public double Scale { get; set; } = 1.0;
        public double Deadzone { get; set; } = 0.08;
        public string AccentColor { get; set; } = "#00FFCC";
        public bool DebugMode { get; set; } = false;
        public bool LockPosition { get; set; } = false;
        public bool IsBackgroundTransparent { get; set; } = true;
        public bool ShowFps { get; set; } = true;
        public bool ShowBallSpeed { get; set; } = true;
        public int FpsUdpPort { get; set; } = 49123;
        public double StatsPanelLeft { get; set; } = 420;
        public double StatsPanelTop { get; set; } = 8;
        public double StatsPanelScale { get; set; } = 1.0;
        public bool IsStatsPanelMovable { get; set; } = false;
        public bool IsStatsBackgroundTransparent { get; set; } = false;
    }
}
