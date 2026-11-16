using Emgu.CV;
using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace OpenMacroBoard.Examples.VideoPlayer;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args == null || args.Length < 1)
        {
            Console.WriteLine("Provide a path to a video as the first command line parameter");
            return;
        }

        // Set your video path here or as command line argument
        var videoPath = args[0];

        using var deck = ExampleHelper.OpenBoard();
        PlayVideoAForgeFFMPEG(deck, videoPath);
    }

    private static void PlayVideoAForgeFFMPEG(IMacroBoard deck, string videoPath)
    {
        using var reader = new VideoCapture(videoPath);
        var frameRate = reader.Get(Emgu.CV.CvEnum.CapProp.Fps);

        if (frameRate <= 1)
        {
            // FPS below 1, we fall back to 30 FPS.
            frameRate = 30.0;
        }

        var frameLength = (int)Math.Round(1000.0 / frameRate);
        long frameNum = 0;

        var frame = new Mat();

        while (reader.Grab())
        {
            var sw = Stopwatch.StartNew();

            reader.Retrieve(frame);

            if (frame.IsEmpty)
            {
                continue;
            }

            deck.DrawFullScreenBitmap(ConvertFrame(frame.ToBitmap()));

            var wait = frameLength - (int)sw.ElapsedMilliseconds;
            sw.Restart();

            if (wait > 0)
            {
                Thread.Sleep(wait);
            }

            frameNum++;

            if (frameNum % 10 == 0 && Console.KeyAvailable)
            {
                return;
            }
        }
    }

    private static Image<Bgr24> ConvertFrame(System.Drawing.Bitmap b)
    {
        var rect = new System.Drawing.Rectangle(0, 0, b.Width, b.Height);

        var lockData = b.LockBits(
            rect,
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb
        );

        try
        {
            var data = new byte[lockData.Stride * b.Height];
            Marshal.Copy(lockData.Scan0, data, 0, data.Length);

            var conversionNeeded = lockData.Stride != b.Width * 3;

            if (conversionNeeded)
            {
                var newData = new byte[b.Width * b.Height * 3];

                for (int y = 0; y < b.Height; y++)
                {
                    for (int x = 0; x < b.Width; x++)
                    {
                        var rPos = y * lockData.Stride + x * 3;
                        var wPos = (y * b.Width + x) * 3;

                        newData[wPos] = data[rPos];
                        newData[wPos + 1] = data[rPos + 1];
                        newData[wPos + 2] = data[rPos + 2];
                    }
                }

                data = newData;
            }

            return Image.LoadPixelData<Bgr24>(data, b.Width, b.Height);
        }
        finally
        {
            b.UnlockBits(lockData);
        }
    }
}
