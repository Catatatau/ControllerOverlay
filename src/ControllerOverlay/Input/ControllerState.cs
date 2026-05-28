using System;

namespace ControllerOverlay.Input
{
    public class ControllerState
    {
        public bool IsConnected { get; set; }
        public string ControllerName { get; set; } = "Unknown";
        public string Layout { get; set; } = "Xbox"; // Xbox, PlayStation, Generic

        // Buttons
        public bool A { get; set; } // Cross
        public bool B { get; set; } // Circle
        public bool X { get; set; } // Square
        public bool Y { get; set; } // Triangle

        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }

        public bool L1 { get; set; }
        public bool R1 { get; set; }
        public bool L3 { get; set; } // Left stick click
        public bool R3 { get; set; } // Right stick click

        public bool Start { get; set; } // Options
        public bool Select { get; set; } // Share / Back
        public bool Home { get; set; } // Guide / PS

        // Axes (-1.0 to 1.0)
        public double LeftStickX { get; set; }
        public double LeftStickY { get; set; }
        public double RightStickX { get; set; }
        public double RightStickY { get; set; }

        // Triggers (0.0 to 1.0)
        public double L2 { get; set; }
        public double R2 { get; set; }

        public ControllerState Snapshot()
        {
            return new ControllerState
            {
                IsConnected = IsConnected,
                ControllerName = ControllerName,
                Layout = Layout,
                A = A,
                B = B,
                X = X,
                Y = Y,
                DPadUp = DPadUp,
                DPadDown = DPadDown,
                DPadLeft = DPadLeft,
                DPadRight = DPadRight,
                L1 = L1,
                R1 = R1,
                L3 = L3,
                R3 = R3,
                Start = Start,
                Select = Select,
                Home = Home,
                LeftStickX = LeftStickX,
                LeftStickY = LeftStickY,
                RightStickX = RightStickX,
                RightStickY = RightStickY,
                L2 = L2,
                R2 = R2
            };
        }

        public void ApplyDeadzone(double deadzone)
        {
            LeftStickX = ApplyStickDeadzone(LeftStickX, deadzone);
            LeftStickY = ApplyStickDeadzone(LeftStickY, deadzone);
            RightStickX = ApplyStickDeadzone(RightStickX, deadzone);
            RightStickY = ApplyStickDeadzone(RightStickY, deadzone);
        }

        private double ApplyStickDeadzone(double value, double deadzone)
        {
            if (Math.Abs(value) < deadzone) return 0;
            return Math.Sign(value) * ((Math.Abs(value) - deadzone) / (1.0 - deadzone));
        }
    }
}
