using System;
using System.Collections.Generic;
using System.Windows;
using OpenMacroBoard.SDK;

namespace OpenMacroBoard.VirtualBoard
{
    internal class VirtualBoardLayout
    {
        public VirtualBoardLayout(IKeyPositionCollection keys, double width, double height)
        {
            var scaleX = width / keys.Area.Width;
            var scaleY = height / keys.Area.Height;

            var scale = Math.Min(scaleX, scaleY);

            var offsetX = (width - keys.Area.Width * scale) / 2;
            var offsetY = (height - keys.Area.Height * scale) / 2;

            KeyPositions = new List<Rect>();

            for (int i = 0; i < keys.Count; i++)
            {
                KeyPositions.Add(new Rect(
                    offsetX + keys[i].Left * scale,
                    offsetY + keys[i].Top * scale,
                    keys[i].Width * scale,
                    keys[i].Height * scale
                ));
            }
        }

        public IList<Rect> KeyPositions { get; }
    }
}
