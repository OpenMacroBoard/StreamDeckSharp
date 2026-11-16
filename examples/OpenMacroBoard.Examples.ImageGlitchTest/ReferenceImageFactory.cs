using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OpenMacroBoard.Examples.ImageGlitchTest;

[SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "It's fine.")]
internal class ReferenceImageFactory
{
    private const double TAU = Math.PI * 2;

    private readonly int imgSize;
    private readonly int keyCount;

    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private readonly Random rnd = new();
    private readonly List<Func<int, KeyBitmap>> availableImageFactories = [];
    private Func<int, KeyBitmap> currentImageFactory;

    public ReferenceImageFactory(int imgSize, int keyCount)
    {
        this.imgSize = imgSize;
        this.keyCount = keyCount;

        availableImageFactories.AddRange(
        [
            GetBlank,
            Rainbow,
            GetStableFilledImage,
            GetStableLineImageVertical,
            GetChangingLineImageVertical,
            GetStableLineImageHorizontal,
            GetChangingLineImageHorizontal,
            GetChangingFilledImage,
        ]);

        currentImageFactory = availableImageFactories[0];
    }

    public int ModeCount
        => availableImageFactories.Count;

    public int CurrentMode
    {
        get;
        set
        {
            field = Mod(value, ModeCount);
            currentImageFactory = availableImageFactories[field];
        }
    }

    public KeyBitmap GetKeyBitmap(int keyId)
    {
        return currentImageFactory(keyId);
    }

    private static int Mod(int x, int m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }

    private static KeyBitmap GetBlank(int keyId)
    {
        return KeyBitmap.Black;
    }

    private static double LinearToSine(double linear, double min, double max)
    {
        return (Math.Sin(linear * TAU) / 2.0 + 0.5) * (max - min) + min;
    }

    private static double LinearToSine(double linear)
    {
        return LinearToSine(linear, 0, 1);
    }

    private static int DiscreteScale(
        int minOutInclusive,
        int maxOutExclusive,
        int minInInclusive,
        int maxInExclusive,
        int value,
        Func<double, double> transferFunction = null
    )
    {
        var maxOutInclusive = maxOutExclusive - 1;
        var maxInInclusive = maxInExclusive - 1;

        if (value < minInInclusive || value > maxInInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var diffOut = maxOutInclusive - minOutInclusive;
        var diffIn = maxInInclusive - minInInclusive;

        if (diffIn <= 0)
        {
            throw new ArgumentException(nameof(diffIn));
        }

        if (diffOut <= 0)
        {
            throw new ArgumentException(nameof(diffOut));
        }

        var percent = (value - minInInclusive) / (double)diffIn;

        if (transferFunction != null)
        {
            percent = transferFunction(percent);
        }

        var outVal = (int)Math.Round(percent * diffOut, 0) + minOutInclusive;

        if (outVal <= minOutInclusive)
        {
            return minOutInclusive;
        }

        if (outVal >= maxOutInclusive)
        {
            return maxOutInclusive;
        }

        return outVal;
    }

    private int GetSawtoothTime(int msBase)
    {
        return (int)(stopwatch.ElapsedMilliseconds % msBase);
    }

    private KeyBitmap GetStableFilledImage(int key)
    {
        return GetGrayImage((byte)DiscreteScale(0, 256, 0, keyCount, key, (g) => Math.Pow(g, 2.2)));
    }

    private KeyBitmap GetStableLineImageVertical(int key)
    {
        return GetVerticalStripeImage(DiscreteScale(0, imgSize, 0, keyCount, key));
    }

    private KeyBitmap GetStableLineImageHorizontal(int key)
    {
        return GetHorizontalStripeImage(DiscreteScale(0, imgSize, 0, keyCount, key));
    }

    private KeyBitmap GetChangingLineImageVertical(int key)
    {
        return GetVerticalStripeImage(rnd.Next(0, imgSize));
    }

    private KeyBitmap GetChangingLineImageHorizontal(int key)
    {
        return GetHorizontalStripeImage(rnd.Next(0, imgSize));
    }

    private KeyBitmap GetChangingFilledImage(int key)
    {
        const int limit = 3000;
        var t = GetSawtoothTime(limit);
        return GetGrayImage((byte)DiscreteScale(0, 255, 0, limit, t, LinearToSine));
    }

    private KeyBitmap GetVerticalStripeImage(int pos)
    {
        var raw = new byte[imgSize * imgSize * 3];

        for (var y = 0; y < imgSize; y++)
        {
            var p = (y * imgSize + pos) * 3;
            raw[p + 0] = 255;
            raw[p + 1] = 255;
            raw[p + 2] = 255;
        }

        return KeyBitmap.Create.FromBgr24Array(imgSize, imgSize, raw);
    }

    private KeyBitmap Rainbow(int keyId)
    {
        var raw = new byte[imgSize * imgSize * 3];

        for (var y = 0; y < imgSize; y++)
        {
            for (var x = 0; x < imgSize; x++)
            {
                var p = (y * imgSize + x) * 3;

                var nX = (int)Math.Round((double)x / imgSize * 256, 0);
                var nY = (int)Math.Round((double)y / imgSize * 256, 0);

                if (nX > 255)
                {
                    nX = 255;
                }

                if (nY > 255)
                {
                    nY = 255;
                }

                var blue = (byte)nY;
                var yellow = (byte)nX;
                raw[p + 0] = blue;
                raw[p + 1] = yellow;
                raw[p + 2] = yellow;
            }
        }

        return KeyBitmap.Create.FromBgr24Array(imgSize, imgSize, raw);
    }

    private KeyBitmap GetHorizontalStripeImage(int pos)
    {
        var raw = new byte[imgSize * imgSize * 3];

        for (var x = 0; x < imgSize; x++)
        {
            var p = (pos * imgSize + x) * 3;
            raw[p + 0] = 255;
            raw[p + 1] = 255;
            raw[p + 2] = 255;
        }

        return KeyBitmap.Create.FromBgr24Array(imgSize, imgSize, raw);
    }

    private KeyBitmap GetGrayImage(byte b)
    {
        var raw = new byte[imgSize * imgSize * 3];

        for (var i = 0; i < raw.Length; i++)
        {
            raw[i] = b;
        }

        return KeyBitmap.Create.FromBgr24Array(imgSize, imgSize, raw);
    }
}
