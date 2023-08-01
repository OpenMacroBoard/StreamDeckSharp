using System;

namespace StreamDeckSharp
{
    /// <summary>
    /// Fully quallified USB product identifier. Includes the USB Vendor ID and the USB Product ID.
    /// </summary>
    public readonly struct UsbVendorProductPair : IEquatable<UsbVendorProductPair>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsbVendorProductPair"/> struct.
        /// </summary>
        public UsbVendorProductPair(int vendorId, int productId)
        {
            UsbVendorId = vendorId;
            UsbProductId = productId;
        }

        /// <summary>
        /// USB vendor id
        /// </summary>
        public int UsbVendorId { get; }

        /// <summary>
        /// USB product id
        /// </summary>
        public int UsbProductId { get; }

        /// <summary>
        /// The == operator. Calls <see cref="Equals(UsbVendorProductPair, UsbVendorProductPair)"/> internally.
        /// </summary>
        public static bool operator ==(UsbVendorProductPair a, UsbVendorProductPair b)
        {
            return Equals(a, b);
        }

        /// <summary>
        /// The == operator. Calls <see cref="Equals(UsbVendorProductPair, UsbVendorProductPair)"/> internally
        /// and inverts the result.
        /// </summary>
        public static bool operator !=(UsbVendorProductPair a, UsbVendorProductPair b)
        {
            return !Equals(a, b);
        }

        /// <summary>
        /// Indicates whether the two givel objects is equal.
        /// </summary>
        /// <param name="a">First object.</param>
        /// <param name="b">Second object.</param>
        /// <returns>true if the two objects are equal; otherwise, false.</returns>
        public static bool Equals(UsbVendorProductPair a, UsbVendorProductPair b)
        {
            return a.UsbVendorId == b.UsbVendorId && a.UsbProductId == b.UsbProductId;
        }

        /// <inheritdoc/>
        public bool Equals(UsbVendorProductPair other)
        {
            return Equals(this, other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not UsbVendorProductPair other)
            {
                return false;
            }

            return Equals(this, other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return UsbVendorId.GetHashCode() ^ UsbProductId.GetHashCode();
        }
    }
}
