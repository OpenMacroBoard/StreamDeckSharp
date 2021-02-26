#region License
/* Copyright 2012-2013, 2017, 2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform.MacOS
{
    static class NativeMethods
    {
        const string libc = "libc";
        const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
        const string IOKit = "/System/Library/Frameworks/IOKit.framework/IOKit";

        public enum error
        {
            EINTR = 4,
            EACCES = 13,
            EBUSY = 16,
            EAGAIN = 35
        }

        public enum oflag
        {
            RDWR = 0x00002,
            NONBLOCK = 0x00004,
            NOCTTY = 0x20000
        }

        public enum pollev : short
        {
            IN = 1,
            OUT = 4,
            ERR = 8,
            HUP = 16,
            NVAL = 32
        }

        public const int F_SETFL = 4;
        public const int TCIFLUSH = 1;
        public const int TCOFLUSH = 2;
        public const int TCIOFLUSH = 3;
        public const int TCSANOW = 0;
        public const int TCSADRAIN = 1;
        public const int TCSAFLUSH = 2;
        public static readonly UIntPtr TIOCEXCL = _IO((byte)'t', (byte)13);
        public static readonly UIntPtr TIOCNXCL = _IO((byte)'t', (byte)14);
        public const int VMIN = 16;
        public const int VTIME = 17;

        public const ulong IXON = 0x200;
        public const ulong IXOFF = 0x400;
        public const ulong CSIZE = 0x300;
        public const ulong CS7 = 0x200;
        public const ulong CS8 = 0x300;
        public const ulong CSTOPB = 0x400;
        public const ulong CREAD = 0x800;
        public const ulong PARENB = 0x1000;
        public const ulong PARODD = 0x2000;
        public const ulong HUPCL = 0x4000;
        public const ulong CLOCAL = 0x8000;
        public const ulong CRTSCTS = 0x30000;
        public const ulong OPOST = 0x1;

        public const uint IOC_VOID = 0x20000000;
        public const uint IOC_OUT = 0x40000000;
        public const uint IOC_IN = 0x80000000;
        public const uint IOC_INOUT = IOC_IN | IOC_OUT;
        static UIntPtr _IOC(uint inout, byte group, byte num, int len) { return (UIntPtr)(inout | (uint)len << 16 | (uint)group << 8 | (uint)num); }
        static UIntPtr _IO(byte group, byte num) { return _IOC(IOC_VOID, group, num, 0); }

        public static readonly IntPtr kCFRunLoopDefaultMode = CFStringCreateWithCharacters("kCFRunLoopDefaultMode");
        public static readonly IntPtr kIOFirstMatchNotification = CFStringCreateWithCharacters("IOServiceFirstMatch");
        public static readonly IntPtr kIOMatchedNotification = CFStringCreateWithCharacters("IOServiceMatched");
        public static readonly IntPtr kIOTerminatedNotification = CFStringCreateWithCharacters("IOServiceTerminate");
        public static readonly IntPtr kIOHIDVendorIDKey = CFStringCreateWithCharacters("VendorID");
        public static readonly IntPtr kIOHIDProductIDKey = CFStringCreateWithCharacters("ProductID");
        public static readonly IntPtr kIOHIDVersionNumberKey = CFStringCreateWithCharacters("VersionNumber");
        public static readonly IntPtr kIOHIDManufacturerKey = CFStringCreateWithCharacters("Manufacturer");
        public static readonly IntPtr kIOHIDProductKey = CFStringCreateWithCharacters("Product");
        public static readonly IntPtr kIOHIDSerialNumberKey = CFStringCreateWithCharacters("SerialNumber");
        public static readonly IntPtr kIOHIDLocationIDKey = CFStringCreateWithCharacters("LocationID");
        public static readonly IntPtr kIOHIDMaxInputReportSizeKey = CFStringCreateWithCharacters("MaxInputReportSize");
        public static readonly IntPtr kIOHIDMaxOutputReportSizeKey = CFStringCreateWithCharacters("MaxOutputReportSize");
        public static readonly IntPtr kIOHIDMaxFeatureReportSizeKey = CFStringCreateWithCharacters("MaxFeatureReportSize");
        public static readonly IntPtr kIOHIDReportDescriptorKey = CFStringCreateWithCharacters("ReportDescriptor");
        public static readonly IntPtr kIOCalloutDeviceKey = CFStringCreateWithCharacters("IOCalloutDevice");

        public delegate void IOHIDCallback(IntPtr context, IOReturn result, IntPtr sender);
        public delegate void IOHIDDeviceCallback(IntPtr context, IOReturn result, IntPtr sender, IntPtr device);
        public delegate void IOHIDReportCallback(IntPtr context, IOReturn result, IntPtr sender,
                                                 IOHIDReportType type, uint reportID, IntPtr report, IntPtr reportLength);
        public delegate void IOServiceMatchingCallback(IntPtr context, int iterator);

        public enum OSErr : short
        {
            noErr = 0,
            gestaltUnknownErr = -5550,
            gestaltUndefSelectorErr = -5551,
            gestaltDupSelectorErr = -5552,
            gestaltLocationErr = -5553
        }

        public enum OSType : uint
        {
            gestaltSystemVersion = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'v' << 0,
            gestaltSystemVersionMajor = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'1' << 0,
            gestaltSystemVersionMinor = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'2' << 0,
            gestaltSystemVersionBugFix = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'3' << 0
        }

        public enum IOOptionBits
        {
            None = 0
        }

        public enum IOHIDElementType
        {
            InputMisc = 1,
            InputButton = 2,
            InputAxis = 3,
            InputScanCodes = 4,
            Output = 129,
            Feature = 257,
            Collection = 513
        }

        public enum IOHIDReportType
        {
            Input = 0,
            Output,
            Feature
        }

        public enum IOReturn
        {
            Success = 0,
            ExclusiveAccess = -536870203,
            NotSupported = -536870201,
            Offline = -536870185,
            NotPermitted = -536870174
        }

        public struct io_string_t
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] Value;

            public io_string_t Clone()
            {
                return new io_string_t() { Value = (byte[])Value.Clone() };
            }

            public override bool Equals(object obj)
            {
                return obj is io_string_t && this == (io_string_t)obj;
            }

            public override int GetHashCode()
            {
                return Value.Length >= 1 ? Value[0] : -1;
            }

            public override string ToString()
            {
                return Encoding.UTF8.GetString(Value.TakeWhile(ch => ch != 0).ToArray());
            }

            public static bool operator ==(io_string_t io1, io_string_t io2)
            {
                return io1.Value.SequenceEqual(io2.Value);
            }

            public static bool operator !=(io_string_t io1, io_string_t io2)
            {
                return !(io1 == io2);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct pollfd
        {
            public int fd;
            public pollev events;
            public pollev revents;
        }

        public unsafe struct termios
        {
            public UIntPtr c_iflag;
            public UIntPtr c_oflag;
            public UIntPtr c_cflag;
            public UIntPtr c_lflag;
            public fixed byte c_cc[20];
            public UIntPtr c_ispeed;
            public UIntPtr c_ospeed;
        }

        public enum CFNumberType
        {
            Int = 9
        }

        public struct CFRange
        {
            public IntPtr Start, Length;
        }

        public struct IOObject : IDisposable
        {
            public int Handle { get; set; }
            public bool IsSet { get { return Handle != 0; } }

            void IDisposable.Dispose()
            {
                if (IsSet) { IOObjectRelease(Handle); Handle = 0; }
            }

            public static implicit operator int(IOObject self)
            {
                return self.Handle;
            }
        }

        public struct CFType : IDisposable
        {
            public IntPtr Handle { get; set; }
            public bool IsSet { get { return Handle != IntPtr.Zero; } }

            void IDisposable.Dispose()
            {
                if (IsSet) { CFRelease(Handle); Handle = IntPtr.Zero; }
            }

            public static implicit operator IntPtr(CFType self)
            {
                return self.Handle;
            }
        }

        public static CFType ToCFType(this IntPtr handle)
        {
            return new CFType() { Handle = handle };
        }

        public static IOObject ToIOObject(this int handle)
        {
            return new IOObject() { Handle = handle };
        }

        [DllImport(CoreServices, EntryPoint = "Gestalt")]
        public static extern OSErr Gestalt(OSType selector, out IntPtr response);

        [DllImport(CoreFoundation, EntryPoint = "CFGetTypeID")]
		public static extern uint CFGetTypeID(IntPtr type);

        [DllImport(CoreFoundation, EntryPoint = "CFArrayGetTypeID")]
        public static extern uint CFArrayGetTypeID();

        [DllImport(CoreFoundation, EntryPoint = "CFDataGetTypeID")]
        public static extern uint CFDataGetTypeID();

        [DllImport(CoreFoundation, EntryPoint = "CFNumberGetTypeID")]
		public static extern uint CFNumberGetTypeID();

        [DllImport(CoreFoundation, EntryPoint = "CFStringGetTypeID")]
		public static extern uint CFStringGetTypeID();

        [DllImport(CoreFoundation, EntryPoint = "CFDictionaryCreateMutable")]
        public static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, IntPtr capacity,
                                                       		  IntPtr keyCallbacks, IntPtr valueCallbacks);

        public static IntPtr CFDictionaryCreateMutable()
        {
            return CFDictionaryCreateMutable(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport(CoreFoundation, EntryPoint = "CFDictionarySetValue")]
        public static extern void CFDictionarySetValue(IntPtr dict, IntPtr key, IntPtr value);

        [DllImport(CoreFoundation, EntryPoint = "CFArrayGetCount")]
        public static extern IntPtr CFArrayGetCount(IntPtr array);

        [DllImport(CoreFoundation, EntryPoint = "CFArrayGetValueAtIndex")]
        public static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, IntPtr index);

        [DllImport(CoreFoundation, EntryPoint = "CFDataGetBytes")]
        public static extern void CFDataGetBytes(IntPtr data, CFRange range, byte[] buffer);

        public static byte[] CFDataGetBytes(IntPtr data)
        {
            if (data == IntPtr.Zero || CFGetTypeID(data) != CFDataGetTypeID()) { return null; }
            byte[] buffer = new byte[checked((int)CFDataGetLength(data))];
            CFDataGetBytes(data, new CFRange() { Start = (IntPtr)0, Length = (IntPtr)buffer.Length }, buffer);
            return buffer;
        }

        [DllImport(CoreFoundation, EntryPoint = "CFDataGetLength")]
        public static extern IntPtr CFDataGetLength(IntPtr data);

        [DllImport(CoreFoundation, EntryPoint = "CFNumberCreate")]
        public static extern IntPtr CFNumberCreate(IntPtr allocator, CFNumberType type, ref int value);

        public static IntPtr CFNumberCreate(int value)
        {
            return CFNumberCreate(IntPtr.Zero, CFNumberType.Int, ref value);
        }

        [DllImport(CoreFoundation, EntryPoint = "CFNumberGetValue")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CFNumberGetValue(IntPtr number, CFNumberType type, out int value);

        public static int? CFNumberGetValue(IntPtr number)
        {
            int value;
            return number != IntPtr.Zero && CFGetTypeID(number) == CFNumberGetTypeID() &&
				CFNumberGetValue(number, CFNumberType.Int, out value) ? (int?)value : null;
        }

        [DllImport(CoreFoundation, CharSet = CharSet.Unicode, EntryPoint = "CFStringCreateWithCharacters")]
        public static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, char[] buffer, IntPtr length);

        public static IntPtr CFStringCreateWithCharacters(string str)
        {
            return CFStringCreateWithCharacters(IntPtr.Zero, str.ToCharArray(), (IntPtr)str.Length);
        }

        [DllImport(CoreFoundation, CharSet = CharSet.Unicode, EntryPoint = "CFStringGetCharacters")]
        public static extern void CFStringGetCharacters(IntPtr str, CFRange range, char[] buffer);

        public static string CFStringGetCharacters(IntPtr str)
        {
            if (str == IntPtr.Zero || CFGetTypeID(str) != CFStringGetTypeID()) { return null; }
            char[] buffer = new char[checked((int)CFStringGetLength(str))];
            CFStringGetCharacters(str, new CFRange() { Start = (IntPtr)0, Length = (IntPtr)buffer.Length }, buffer);
            return new string(buffer);
        }

        [DllImport(CoreFoundation, EntryPoint = "CFStringGetLength")]
        public static extern IntPtr CFStringGetLength(IntPtr str);

        [DllImport(CoreFoundation, EntryPoint = "CFRunLoopRun")]
        public static extern void CFRunLoopRun();

        [DllImport(CoreFoundation, EntryPoint = "CFRunLoopGetCurrent")]
        public static extern IntPtr CFRunLoopGetCurrent();

        [DllImport(CoreFoundation, EntryPoint = "CFRunLoopAddSource")]
        public static extern void CFRunLoopAddSource(IntPtr runLoop, IntPtr source, IntPtr mode);

        [DllImport(CoreFoundation, EntryPoint = "CFRunLoopRemoveSource")]
        public static extern void CFRunLoopRemoveSource(IntPtr runLoop, IntPtr source, IntPtr mode);

        [DllImport(CoreFoundation, EntryPoint = "CFRunLoopStop")]
        public static extern void CFRunLoopStop(IntPtr runLoop);

        [DllImport(CoreFoundation, EntryPoint = "CFRelease")]
        public static extern void CFRelease(IntPtr obj);

        [DllImport(CoreFoundation, EntryPoint = "CFRetain")]
        public static extern void CFRetain(IntPtr obj);

        [DllImport(CoreFoundation, EntryPoint = "CFSetGetCount")]
        public static extern IntPtr CFSetGetCount(IntPtr set);

        [DllImport(CoreFoundation, EntryPoint = "CFSetGetValues")]
        public static extern void CFSetGetValues(IntPtr set, IntPtr[] values);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceCreate")]
        public static extern IntPtr IOHIDDeviceCreate(IntPtr allocator, int service);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceOpen")]
        public static extern IOReturn IOHIDDeviceOpen(IntPtr device, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceCopyMatchingElements")]
        public static extern IntPtr IOHIDDeviceCopyMatchingElements(IntPtr device, IntPtr matching, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceRegisterInputReportCallback")]
        public static extern void IOHIDDeviceRegisterInputReportCallback(IntPtr device, IntPtr report, IntPtr reportLength,
                                                                         IOHIDReportCallback callback, IntPtr context);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceRegisterRemovalCallback")]
        public static extern void IOHIDDeviceRegisterRemovalCallback(IntPtr device, IOHIDCallback callback, IntPtr context);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceGetReport")]
        public static extern IOReturn IOHIDDeviceGetReport(IntPtr device, IOHIDReportType type, IntPtr reportID, IntPtr report, ref IntPtr reportLength);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceSetReport")]
        public static extern IOReturn IOHIDDeviceSetReport(IntPtr device, IOHIDReportType type, IntPtr reportID, IntPtr report, IntPtr reportLength);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceScheduleWithRunLoop")]
        public static extern void IOHIDDeviceScheduleWithRunLoop(IntPtr device, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceUnscheduleFromRunLoop")]
        public static extern void IOHIDDeviceUnscheduleFromRunLoop(IntPtr device, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit, EntryPoint = "IOHIDDeviceClose")]
        public static extern IOReturn IOHIDDeviceClose(IntPtr device, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit, EntryPoint = "IOHIDElementGetReportID")]
        public static extern uint IOHIDElementGetReportID(IntPtr element);

        [DllImport(IOKit, EntryPoint = "IOHIDElementGetType")]
        public static extern IOHIDElementType IOHIDElementGetType(IntPtr element);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerCreate")]
        public static extern IntPtr IOHIDManagerCreate(IntPtr allocator, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerSetDeviceMatching")]
        public static extern void IOHIDManagerSetDeviceMatching(IntPtr manager, IntPtr matching);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerRegisterDeviceMatchingCallback")]
        public static extern void IOHIDManagerRegisterDeviceMatchingCallback(IntPtr manager, IOHIDDeviceCallback callback, IntPtr context);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerRegisterDeviceRemovalCallback")]
        public static extern void IOHIDManagerRegisterDeviceRemovalCallback(IntPtr manager, IOHIDDeviceCallback callback, IntPtr context);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerScheduleWithRunLoop")]
        public static extern void IOHIDManagerScheduleWithRunLoop(IntPtr manager, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit, EntryPoint = "IOHIDManagerUnscheduleFromRunLoop")]
        public static extern void IOHIDManagerUnscheduleFromRunLoop(IntPtr manager, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit, EntryPoint = "IONotificationPortCreate")]
        public static extern IntPtr IONotificationPortCreate(int masterPort);

        [DllImport(IOKit, EntryPoint = "IONotificationPortGetRunLoopSource")]
        public static extern IntPtr IONotificationPortGetRunLoopSource(IntPtr notifyPort);

        [DllImport(IOKit, EntryPoint = "IOIteratorNext")]
        public static extern int IOIteratorNext(int iterator);

        [DllImport(IOKit, EntryPoint = "IOObjectRetain")]
        public static extern IOReturn IOObjectRetain(int @object);

        [DllImport(IOKit, EntryPoint = "IOObjectRelease")]
        public static extern IOReturn IOObjectRelease(int @object);

        [DllImport(IOKit, EntryPoint = "IORegistryEntryCreateCFProperty")]
        public static extern IntPtr IORegistryEntryCreateCFProperty(int entry, IntPtr strKey, IntPtr allocator, IOOptionBits options = IOOptionBits.None);

        public static int? IORegistryEntryGetCFProperty_Int(int entry, IntPtr intKey)
        {
            using (var property = IORegistryEntryCreateCFProperty(entry, intKey, IntPtr.Zero).ToCFType())
            {
                return CFNumberGetValue(property);
            }
        }

        public static string IORegistryEntryGetCFProperty_String(int entry, IntPtr strKey)
        {
            using (var property = IORegistryEntryCreateCFProperty(entry, strKey, IntPtr.Zero).ToCFType())
            {
                return CFStringGetCharacters(property);
            }
        }

        public static byte[] IORegistryEntryGetCFProperty_Data(int entry, IntPtr dataKey)
        {
            using (var property = IORegistryEntryCreateCFProperty(entry, dataKey, IntPtr.Zero).ToCFType())
            {
                return CFDataGetBytes(property);
            }
        }

        [DllImport(IOKit, EntryPoint = "IORegistryEntryFromPath")]
        public static extern int IORegistryEntryFromPath(int masterPort, ref io_string_t path);

        [DllImport(IOKit, EntryPoint = "IORegistryEntryGetPath")] // plane = IOService
        public static extern IOReturn IORegistryEntryGetPath(int entry, [MarshalAs(UnmanagedType.LPStr)] string plane, out io_string_t path);

        [DllImport(IOKit, EntryPoint = "IOServiceAddMatchingNotification")]
        public static extern IOReturn IOServiceAddMatchingNotification(IntPtr notifyPort, IntPtr notifyType, IntPtr matching, IOServiceMatchingCallback callback, IntPtr context, out int iterator);

        [DllImport(IOKit, EntryPoint = "IOServiceGetMatchingServices")]
        public static extern IOReturn IOServiceGetMatchingServices(int masterPort, IntPtr matching, out int iterator);

        [DllImport(IOKit, EntryPoint = "IOServiceMatching")] // name = IOHIDDevice
        public static extern IntPtr IOServiceMatching([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(libc, SetLastError = true, EntryPoint = "open")]
        public static extern int open(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string filename,
             oflag oflag);

        [DllImport(libc, SetLastError = true, EntryPoint = "read")]
        public static extern IntPtr read(int filedes, IntPtr buffer, UIntPtr size);

        [DllImport(libc, SetLastError = true, EntryPoint = "write")]
        public static extern IntPtr write(int filedes, IntPtr buffer, UIntPtr size);

        [DllImport(libc, EntryPoint = "poll")]
        public static extern int poll(ref pollfd fd, uint nfds, int timeout = -1); // < 0 if failed

        [DllImport(libc, SetLastError = true, EntryPoint = "fcntl")]
        public static extern int fcntl(int filedes, int cmd, int arg);

        [DllImport(libc, SetLastError = true, EntryPoint = "ioctl")]
        public static extern int ioctl(int filedes, UIntPtr request);

        [DllImport(libc, SetLastError = true, EntryPoint = "close")]
        public static extern int close(int filedes); // < 0 if failed

        [DllImport(libc, SetLastError = true, EntryPoint = "cfmakeraw")]
        public static extern void cfmakeraw(ref termios termios);

        [DllImport(libc, SetLastError = true, EntryPoint = "cfsetspeed")]
        public static extern int cfsetspeed(ref termios termios, UIntPtr speed);

        [DllImport(libc, SetLastError = true, EntryPoint = "tcgetattr")]
        public static extern int tcgetattr(int filedes, out termios termios);

        [DllImport(libc, SetLastError = true, EntryPoint = "tcsetattr")]
        public static extern int tcsetattr(int filedes, int actions, ref termios termios);

        [DllImport(libc, SetLastError = true, EntryPoint = "tcdrain")]
        public static extern int tcdrain(int filedes);

        [DllImport(libc, SetLastError = true, EntryPoint = "tcflush")]
        public static extern int tcflush(int filedes, int action);

        public static int retry(Func<int> sysfunc)
        {
            while (true)
            {
                int ret = sysfunc(); var error = (error)Marshal.GetLastWin32Error();
                if (ret >= 0 || error != error.EINTR) { return ret; }
            }
        }

        public static IntPtr retry(Func<IntPtr> sysfunc)
        {
            while (true)
            {
                IntPtr ret = sysfunc(); var error = (error)Marshal.GetLastWin32Error();
                if ((long)ret >= 0 || error != error.EINTR) { return ret; }
            }
        }
    }
}
