using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckSharp
{
    public class StreamDeckKeyEventArgs : EventArgs
    {
        public int Key { get; }
        public bool IsDown { get; }

        public StreamDeckKeyEventArgs(int key, bool isDown)
        {
            Key = key;
            IsDown = isDown;
        }
    }
}
