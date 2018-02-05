using System;

namespace StreamDeckSharp
{
    public class KeyEventArgs : EventArgs
    {
        public int Key { get; }
        public bool IsDown { get; }

        public KeyEventArgs(int key, bool isDown)
        {
            Key = key;
            IsDown = isDown;
        }
    }
}
