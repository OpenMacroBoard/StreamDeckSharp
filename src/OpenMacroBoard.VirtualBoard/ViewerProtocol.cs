using OpenMacroBoard.SDK;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text.Json;

namespace OpenMacroBoard.VirtualBoard;

/// <summary>
/// The messages between the server and a viewer (the browser page). The protocol is
/// described in the README of this project.
/// </summary>
internal static class ViewerProtocol
{
    public const byte KeyImageMessageType = 1;
    public const int KeyImageHeaderSize = 7;

    public static byte[] BoardMessage(VirtualBoardSettings settings, string? boardEndpoint, byte brightness)
    {
        return Write(json =>
        {
            json.WriteString("type", "board");
            json.WriteString("deviceName", settings.DeviceName);

            if (boardEndpoint is null)
            {
                json.WriteNull("endpoint");
            }
            else
            {
                json.WriteString("endpoint", boardEndpoint);
            }

            json.WriteNumber("countX", settings.KeysX);
            json.WriteNumber("countY", settings.KeysY);
            json.WriteNumber("keySize", settings.KeySize);
            json.WriteNumber("gapSize", settings.KeyGap);
            json.WriteNumber("brightness", brightness);
        });
    }

    public static byte[] KeyStateMessage(int keyId, bool isDown)
    {
        return Write(json =>
        {
            json.WriteString("type", "keyState");
            json.WriteNumber("key", keyId);
            json.WriteBoolean("down", isDown);
        });
    }

    public static byte[] BrightnessMessage(byte percent)
    {
        return Write(json =>
        {
            json.WriteString("type", "brightness");
            json.WriteNumber("percent", percent);
        });
    }

    /// <summary>
    /// Binary message: type (1 byte), key ID, width and height (2 bytes each, little endian),
    /// then the pixels as RGBA, row by row. A width and height of zero mean a black key.
    /// </summary>
    public static byte[] KeyImageMessage(int keyId, KeyBitmap image)
    {
        IKeyBitmapDataAccess access = image;
        var bgr = access.GetData().Span;

        var width = bgr.IsEmpty ? 0 : access.Width;
        var height = bgr.IsEmpty ? 0 : access.Height;
        var pixelCount = width * height;

        var message = new byte[KeyImageHeaderSize + pixelCount * 4];
        message[0] = KeyImageMessageType;
        BinaryPrimitives.WriteUInt16LittleEndian(message.AsSpan(1), (ushort)keyId);
        BinaryPrimitives.WriteUInt16LittleEndian(message.AsSpan(3), (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(message.AsSpan(5), (ushort)height);

        var rgba = message.AsSpan(KeyImageHeaderSize);

        for (var i = 0; i < pixelCount; i++)
        {
            rgba[i * 4] = bgr[i * 3 + 2];
            rgba[i * 4 + 1] = bgr[i * 3 + 1];
            rgba[i * 4 + 2] = bgr[i * 3];
            rgba[i * 4 + 3] = 255;
        }

        return message;
    }

    /// <summary>
    /// Reads a key message of a viewer: <c>{"type":"key","key":3,"down":true}</c>.
    /// </summary>
    public static bool TryReadKeyMessage(ReadOnlySpan<byte> utf8Json, out int keyId, out bool isDown)
    {
        keyId = 0;
        isDown = false;

        try
        {
            var reader = new Utf8JsonReader(utf8Json);

            if (!JsonDocument.TryParseValue(ref reader, out var document))
            {
                return false;
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("type", out var type)
                    || type.ValueKind != JsonValueKind.String
                    || type.GetString() != "key"
                    || !root.TryGetProperty("key", out var key)
                    || !key.TryGetInt32(out keyId)
                    || !root.TryGetProperty("down", out var down)
                    || (down.ValueKind != JsonValueKind.True && down.ValueKind != JsonValueKind.False))
                {
                    return false;
                }

                isDown = down.GetBoolean();
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            // For example a number where no number is expected.
            return false;
        }
    }

    private static byte[] Write(Action<Utf8JsonWriter> writeProperties)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            writeProperties(json);
            json.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }
}
