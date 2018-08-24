using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// Represents a keyboard layout for macro boards by representing all LCD keys
    /// as a collection of rectangles with their position on the board
    /// </summary>
    /// <remarks>
    /// This structure allows OpenMacroBoard to support complex layouts (eq. optimus maximus).
    /// </remarks>
    public class KeyPositionCollection : IKeyPositionCollection
    {
        private readonly Rectangle[] keyPositions;

        /// <summary>
        /// Creates a <see cref="KeyPositionCollection"/>.
        /// </summary>
        /// <param name="keyPositions"></param>
        public KeyPositionCollection(IEnumerable<Rectangle> keyPositions)
        {
            this.keyPositions = keyPositions.ToArray();
            VerifyKeyPositionData(this.keyPositions);

            Area = GetFullArea(this.keyPositions);
        }

        /// <summary>
        /// Creates a <see cref="KeyPositionCollection"/> based on a rectangular grid layout.
        /// </summary>
        /// <param name="xCount">Number of keys in the x-coordinate (horizontal)</param>
        /// <param name="yCount">Number of keys in the y-coordinate (vertical)</param>
        /// <param name="width">Key width (px)</param>
        /// <param name="height">Key height (px)</param>
        /// <param name="dx">Distance between keys in x-coordinate (px)</param>
        /// <param name="dy">Distance between keys in y-coordinate (px)</param>
        public KeyPositionCollection(int xCount, int yCount, int width, int height, int dx, int dy)
            : this(CreateKeyPositions(xCount, yCount, width, height, dx, dy))
        {

        }

        /// <summary>
        /// Creates a <see cref="KeyPositionCollection"/> based on a rectangular grid layout.
        /// </summary>
        /// <param name="xCount">Number of keys in the x-coordinate (horizontal)</param>
        /// <param name="yCount">Number of keys in the y-coordinate (vertical)</param>
        /// <param name="keySize">Square key size (px)</param>
        /// <param name="keyDistance">Distance between keys (px)</param>
        public KeyPositionCollection(int xCount, int yCount, int keySize, int keyDistance)
            : this(CreateKeyPositions(xCount, yCount, keySize, keySize, keyDistance, keyDistance))
        {

        }

        /// <summary>
        /// Enumerates all keys
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Rectangle> GetEnumerator()
        {
            for (int i = 0; i < keyPositions.Length; i++)
                yield return keyPositions[i];
        }

        /// <summary>
        /// Gets a key position with a given index.
        /// </summary>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        public Rectangle this[int keyIndex]
            => keyPositions[keyIndex];

        /// <summary>
        /// The number of keys
        /// </summary>
        public int Count
            => keyPositions.Length;

        /// <summary>
        /// The smallest area that contains all keys
        /// </summary>
        /// <remarks>
        /// This can be used for example to create full screen images that span over all keys
        /// </remarks>
        public Rectangle Area { get; }

        private static IEnumerable<Rectangle> CreateKeyPositions(int xCount, int yCount, int width, int height, int dx, int dy)
        {
            var kWidth = width + dx;
            var kHeight = height + dy;

            for (int y = 0; y < yCount; y++)
                for (int x = 0; x < xCount; x++)
                    yield return new Rectangle(kWidth * x, kHeight * y, width, height);
        }

        private static void VerifyKeyPositionData(IEnumerable<Rectangle> rectangles)
        {
            foreach (var r in rectangles)
            {
                if (r.Width <= 0 || r.Height <= 0)
                    throw new ArgumentException("Height and Width must be ≥ 1");

                if (r.Left < 0 || r.Top < 0)
                    throw new ArgumentException("All key positions must be positive");
            }
        }

        private static Rectangle GetFullArea(IEnumerable<Rectangle> rectangles)
        {
            var minX = 0;
            var minY = 0;
            var maxX = 0;
            var maxY = 0;

            foreach (var kp in rectangles)
            {
                minX = Math.Min(minX, kp.Left);
                minY = Math.Min(minY, kp.Top);
                maxX = Math.Max(maxX, kp.Right);
                maxY = Math.Max(maxY, kp.Bottom);
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();        
    }
}
