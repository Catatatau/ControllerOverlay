using System.Collections.Generic;
using System.Linq;

namespace ControllerOverlay.Input
{
    public class KeyboardMouseState
    {
        private readonly HashSet<int> _pressedKeys = new();

        public bool W => IsPressed(0x57);
        public bool A => IsPressed(0x41);
        public bool S => IsPressed(0x53);
        public bool D => IsPressed(0x44);
        public bool Q => IsPressed(0x51);
        public bool E => IsPressed(0x45);
        public bool R => IsPressed(0x52);
        public bool F => IsPressed(0x46);
        public bool Z => IsPressed(0x5A);
        public bool X => IsPressed(0x58);
        public bool C => IsPressed(0x43);
        public bool V => IsPressed(0x56);
        public bool B => IsPressed(0x42);
        public bool Shift => IsPressed(0x10);
        public bool Ctrl => IsPressed(0x11);
        public bool Space => IsPressed(0x20);
        public bool LClick => IsPressed(0x01);
        public bool RClick => IsPressed(0x02);
        public bool Mouse4 => IsPressed(0x05);
        public bool Mouse5 => IsPressed(0x06);

        public bool IsPressed(int virtualKey)
        {
            return _pressedKeys.Contains(virtualKey);
        }

        public void SetPressedKeys(IEnumerable<int> virtualKeys)
        {
            _pressedKeys.Clear();
            foreach (int key in virtualKeys)
            {
                _pressedKeys.Add(key);
            }
        }

        public KeyboardMouseState Snapshot()
        {
            var snapshot = new KeyboardMouseState();
            snapshot.SetPressedKeys(_pressedKeys.ToArray());
            return snapshot;
        }
    }
}
