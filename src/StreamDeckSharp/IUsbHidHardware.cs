namespace StreamDeckSharp
{
    /// <summary>
    /// USB HID specific hardware information
    /// </summary>
    public interface IUsbHidHardware : IHardware
    {
        /// <summary>
        /// USB vendor id
        /// </summary>
        int UsbVendorId { get; }

        /// <summary>
        /// USB product id
        /// </summary>
        int UsbProductId { get; }
    }
}
