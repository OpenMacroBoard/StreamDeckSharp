namespace OpenMacroBoard.SocketIO.Internals;

internal enum PackageType : byte
{
    Reserved0 = 0x00,
    Reserved1 = 0x01,
    Reserved2 = 0x02,

    /// <summary>
    /// [0x03] [keyId : UInt16] [is_down : byte|bool]
    /// </summary>
    KeyStateChange = 0x03,          // out ---->

    /// <summary>
    /// [0x04] [keyId : UInt16] [img_width : UInt16] [img_height : UInt16] [data_length : int] [data : byte[]]
    /// </summary>
    SetKeyImage = 0x04,             // in  <----

    /// <summary>
    /// [0x05] [percent_brightness (0-100) : byte]
    /// </summary>
    SetBrightness = 0x05,           // in  <----

    /// <summary>
    /// [0x08]
    /// </summary>
    ShowLogo = 0x06,                // in  <----
}
