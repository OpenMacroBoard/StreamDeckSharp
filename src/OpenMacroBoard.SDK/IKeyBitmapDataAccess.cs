using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// An interface that allows you to access the underlying data of <see cref="KeyBitmap"/>s
    /// </summary>
    public interface IKeyBitmapDataAccess
    {
        /// <summary>
        /// Gets a value indicating wheter the underlying byte array is null
        /// </summary>
        /// <value></value>
        bool IsNull { get; }

        /// <summary>
        /// Gets the stride for the bitmap data
        /// </summary>
        int Stride { get; }

        /// <summary>
        /// Gets the length of the bitmap data array
        /// </summary>
        int DataLength { get; }

        /// <summary>
        /// Copies <paramref name="length"/> number of bytes from the bitmap data array to a given array.
        /// </summary>
        /// <param name="targetArray">Target array</param>
        /// <param name="targetIndex">Index of first byte in <paramref name="targetArray"/></param>
        /// <param name="startIndex">Index of first byte in bitmap data array</param>
        /// <param name="length">Number of bytes to copy</param>
        void CopyData(byte[] targetArray, int targetIndex, int startIndex, int length);

        /// <summary>
        /// Copies the bitmap data array to a given array.
        /// </summary>
        /// <param name="targetArray">Target array</param>
        /// <param name="targetIndex">Index of first byte in <paramref name="targetArray"/></param>
        void CopyData(byte[] targetArray, int targetIndex);

        /// <summary>
        /// Creates a copy if the internal bitmap data array
        /// </summary>
        /// <returns>raw bgr24 bitmap array</returns>
        byte[] CopyData();
    }
}
