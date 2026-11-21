using System;

namespace OpenMacroBoard.SDK.Helper;

/// <summary>
/// Extensions from <see cref="IKeyLayout"/>s.
/// </summary>
public static class KeyLayoutAreaHelperExtensions
{
    /// <summary>
    /// Calculates a <see cref="OmbRectangle"/> which spans all keys from a given <see cref="IKeyLayout"/>.
    /// </summary>
    /// <param name="keyLayout">The key layout this area is calculated for.</param>
    /// <returns>Returns a rectangle that spans all keys.</returns>
    public static OmbRectangle GetFullArea(this IKeyLayout keyLayout)
    {
        var minX = 0;
        var minY = 0;
        var maxX = 0;
        var maxY = 0;

        foreach (var keyRect in keyLayout)
        {
            minX = Math.Min(minX, keyRect.Left);
            minY = Math.Min(minY, keyRect.Top);
            maxX = Math.Max(maxX, keyRect.Right);
            maxY = Math.Max(maxY, keyRect.Bottom);
        }

        return new OmbRectangle(minX, minY, maxX - minX, maxY - minY);
    }
}
