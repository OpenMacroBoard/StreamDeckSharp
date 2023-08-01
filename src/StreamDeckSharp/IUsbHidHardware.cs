namespace StreamDeckSharp
{
    /// <summary>
    /// USB HID specific hardware information
    /// </summary>
    public interface IUsbHidHardware : IHardware
    {
        /// <summary>
        /// Unique identifier for USB device. Vendor and product ID pair.
        /// </summary>
        UsbVendorProductPair UsbId { get; }
    }
}
