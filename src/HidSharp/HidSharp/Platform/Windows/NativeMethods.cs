#region License
/* Copyright 2010-2012, 2016 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

#pragma warning disable 169, 649

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using HidSharp.Experimental;
using HidSharp.Utility;

namespace HidSharp.Platform.Windows
{
    unsafe static class NativeMethods
    {
        public static readonly Guid GuidForBluetoothLEDevice = new Guid("{781AEE18-7733-4CE4-ADD0-91F41C67B592}");
        public static readonly Guid GuidForBluetoothHciEvent = new Guid("{FC240062-1541-49BE-B463-84C4DCD7BF7F}");
        public static readonly Guid GuidForBluetoothRadioInRange = new Guid("{EA3B5B82-26EE-450E-B0D8-D26FE30A3869}");
        public static readonly Guid GuidForBluetoothRadioOutOfRange = new Guid("{E28867C9-C2AA-4CED-B969-4570866037C4}");
        public static readonly Guid GuidForComPort = new Guid("{86E0D1E0-8089-11D0-9CE4-08003E301F73}");
        public static readonly Guid GuidForPortsClass = new Guid("{4D36E978-E325-11CE-BFC1-08002BE10318}");
        public static readonly Guid GuidForUsbHub = new Guid("{F18A0E88-C30C-11D0-8815-00A0C906BED8}");

        // For constants, see PInvoke.Net,
        //  http://doxygen.reactos.org/de/d2a/hidclass_8h_source.html
        //  http://www.rpi.edu/dept/cis/software/g77-mingw32/include/winioctl.h
        // and Google.
        public const int DICS_FLAG_GLOBAL = 1;
        public const int DIREG_DEV = 1;
        public const int ERROR_GEN_FAILURE = 31;
        public const int ERROR_HANDLE_EOF = 38;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int ERROR_NO_MORE_ITEMS = 259;
        public const int ERROR_OPERATION_ABORTED = 995;
        public const int ERROR_IO_PENDING = 997;
        public const int ERROR_INVALID_PARAMETER = unchecked((int)0x80070057);
        public const int ERROR_MORE_DATA = unchecked((int)0x800700ea);
        public const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
        public const uint FILE_ANY_ACCESS = 0;
        public const uint FILE_DEVICE_KEYBOARD = 11;
        public const uint FILE_DEVICE_UNKNOWN = 34;
        public const uint HKEY_CURRENT_USER = 0x80000001;
        public const uint HKEY_LOCAL_MACHINE = 0x80000002;
        public const uint KEY_ALL_ACCESS = 0xf003f;
        public const uint KEY_NOTIFY = 0x00010;
        public const uint KEY_READ = 0x20019;
        public const uint KEY_WRITE = 0x20006;
        public const uint METHOD_BUFFERED = 0;
        public const uint METHOD_NEITHER = 3;
        public const uint REG_NOTIFY_CHANGE_NAME = 1;
        public const uint REG_NOTIFY_CHANGE_LAST_SET = 4;
        public const uint REG_SZ = 1;
        public const uint SPDRP_DEVICEDESC = 0;
        public const uint SPDRP_HARDWAREID = 1;
        public const uint SPDRP_FRIENDLYNAME = 12;
        public const uint SPDRP_ADDRESS = 28;
        public const uint WAIT_OBJECT_0 = 0;
        public const uint WAIT_OBJECT_1 = 1;
        public const uint WAIT_TIMEOUT = 258;
        public const uint WM_DEVICECHANGE = 537;

        public const uint RTS_CONTROL_DISABLE = 0;
        public const uint RTS_CONTROL_ENABLE = 1;
        public const uint RTS_CONTROL_HANDSHAKE = 2;
        public const uint RTS_CONTROL_TOGGLE = 3;

        public const byte NOPARITY = 0;
        public const byte ODDPARITY = 1;
        public const byte EVENPARITY = 2;
        public const byte MARKPARITY = 3;
        public const byte SPACEPARITY = 4;

        public const byte ONESTOPBIT = 0;
        public const byte ONE5STOPBITS = 1;
        public const byte TWOSTOPBITS = 2;

        public const uint PURGE_TXABORT = 1;
        public const uint PURGE_RXABORT = 2;
        public const uint PURGE_TXCLEAR = 4;
        public const uint PURGE_RXCLEAR = 8;

        public const int DN_DEVICE_DISCONNECTED = 0x2000000;

        public static uint CTL_CODE(uint devType, uint func, uint method, uint access)
        {
            return devType << 16 | access << 14 | func << 2 | method;
        }

        public static uint HID_CTL_CODE(uint id)
        {
            return CTL_CODE(FILE_DEVICE_KEYBOARD, id, METHOD_NEITHER, FILE_ANY_ACCESS);
        }

        public static int HIDP_ERROR_CODES(int sev, ushort code)
        {
            return sev << 28 | 0x11 << 16 | code;
        }

        public static readonly int HIDP_STATUS_SUCCESS = HIDP_ERROR_CODES(0, 0);
        public static readonly int HIDP_STATUS_INVALID_PREPARSED_DATA = HIDP_ERROR_CODES(12, 1);
        public static readonly int HIDP_STATUS_USAGE_NOT_FOUND = HIDP_ERROR_CODES(12, 4);
        public static readonly int HIDP_STATUS_IS_VALUE_ARRAY = HIDP_ERROR_CODES(12, 12);
        public static readonly uint IOCTL_HID_GET_REPORT_DESCRIPTOR = HID_CTL_CODE(1);
        public static readonly uint IOCTL_USB_GET_NODE_INFORMATION = CTL_CODE(FILE_DEVICE_UNKNOWN, 258, METHOD_BUFFERED, FILE_ANY_ACCESS);
        public static readonly uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION = CTL_CODE(FILE_DEVICE_UNKNOWN, 259, METHOD_BUFFERED, FILE_ANY_ACCESS);
        public static readonly uint IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION = CTL_CODE(FILE_DEVICE_UNKNOWN, 260, METHOD_BUFFERED, FILE_ANY_ACCESS);
        public static readonly uint IOCTL_USB_GET_NODE_CONNECTION_NAME = CTL_CODE(FILE_DEVICE_UNKNOWN, 261, METHOD_BUFFERED, FILE_ANY_ACCESS);
        public static readonly uint IOCTL_USB_GET_NODE_CONNECTION_DRIVERKEY_NAME = CTL_CODE(FILE_DEVICE_UNKNOWN, 264, METHOD_BUFFERED, FILE_ANY_ACCESS);

        public const uint CM_DRP_DRIVER = 10;

        public const int BLUETOOTH_MAX_NAME_SIZE = 248;

        public enum WM_DEVICECHANGE_wParam
        {
            DBT_CONFIGCHANGECANCELED = 0x0019,
            DBT_CONFIGCHANGED = 0x0018,
            DBT_CUSTOMEVENT = 0x8006,
            DBT_DEVICEARRIVAL = 0x8000,
            DBT_DEVICEQUERYREMOVE = 0x8001,
            DBT_DEVICEQUERYREMOVEFAILED = 0x8002,
            DBT_DEVICEREMOVECOMPLETE = 0x8004,
            DBT_DEVICEREMOVEPENDING = 0x8003,
            DBT_DEVICETYPESPECIFIC = 0x8005,
            DBT_DEVNODES_CHANGED = 0x0007,
            DBT_QUERYCHANGECONFIG = 0x0017,
            DBT_USERDEFINED = 0xFFFF
        }

        [Flags]
        public enum EFileAccess : uint
        {
            None = 0,
            Read = 0x80000000,
            Write = 0x40000000,
            Execute = 0x20000000,
            All = 0x10000000
        }

        [Flags]
        public enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
            All = Read | Write | Delete
        }

        public enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Writethrough = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [Flags]
        public enum DIGCF
        {
            None = 0,
            Default = 1,
            Present = 2,
            AllClasses = 4,
            Profile = 8,
            DeviceInterface = 16
        }

        [Flags]
        public enum SPINT
        {
            None = 0,
            Active = 1,
            Default = 2,
            Removed = 4
        }

        public struct HDEVINFO
        {
            IntPtr Value;

            public void Invalidate()
            {
                Value = (IntPtr)(-1);
            }

            public bool IsValid
            {
                get { return Value != (IntPtr)(-1); }
            }
        }

        public struct POINT
        {
            public int X, Y;
        }

        public struct MSG
        {
            public IntPtr Window;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public POINT Point;
        }

        public struct WNDCLASS
        {
            public uint Style;
            public WindowProc WindowProc;
            public int ClassExtra, WindowExtra;
            public IntPtr Instance, Icon, Cursor, Background;
            public string MenuName, ClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HDR
        {
            public int Size, DeviceType, Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int Size, DeviceType, Reserved;
            public Guid ClassGuid;
            public char Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int Size, DeviceType, Reserved;
            public IntPtr DeviceHandle, NotifyHandle;
            public Guid EventGuid;
            public int NameOffset;
            public fixed byte Data[1];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OSVERSIONINFO
        {
            public int OSVersionInfoSize;
            public uint MajorVersion, MinorVersion, BuildNumber, PlatformID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SetupPacket
        {
            public byte bmRequest;
            public byte bRequest;
            public ushort wValue;
            public ushort wIndex;
            public ushort wLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_DESCRIPTOR_REQUEST
        {
            public uint ConnectionIndex;
            public SetupPacket SetupPacket;
        }

        public struct SP_DEVINFO_DATA
        {
            public int Size;
            public Guid ClassGuid;
            public uint DevInst;
            IntPtr Reserved;
        }

        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int Size;
            public Guid InterfaceClassGuid;
            public SPINT Flags;
            IntPtr Reserved;
        }

        [Obfuscation(Exclude = true)]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] public string DevicePath;
        }

        public enum HIDP_REPORT_TYPE
        {
            Input,
            Output,
            Feature,
            Count // for arrays
        }

        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID, ProductID, VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct HIDP_CAPS
        {
            public ushort Usage, UsagePage;
            public ushort InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
            fixed ushort Reserved[17];
            public ushort NumberLinkCollectionNodes,
                NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices,
                NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices,
                NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct HIDP_LINK_COLLECTION_NODE
        {
            public ushort LinkUsage, LinkUsagePage;
            public ushort Parent, NumberOfChildren, NextSibling, FirstChild;
            public byte CollectionType;
            public byte IsAlias { get { return (byte)(IsAliasByte & 1); } }
            byte IsAliasByte;
            fixed byte Reserved[2];
            public IntPtr UserContext;
        }

        public struct HIDP_DATA
        {
            public ushort DataIndex;
            ushort Reserved;
            public uint RawValue;
        }

        [StructLayout(LayoutKind.Sequential, Size = 72)]
        public unsafe struct HIDP_DATA_CAPS
        {
            public ushort UsagePage;
            public byte ReportID;
            public byte IsAlias;
            public ushort BitField;
            public ushort LinkCollection;
            public ushort LinkUsage;
            public ushort LinkUsagePage;
            public byte IsRange;
            public byte IsStringRange;
            public byte IsDesignatorRange;
            public byte IsAbsolute;

            public byte VALUE_HasNull;

            byte Reserved;

            public ushort VALUE_ReportSize;
            public ushort VALUE_ReportCount;

            fixed ushort Reserved2[5];

            public uint VALUE_UnitsExp;
            public uint VALUE_Units;

            public int VALUE_LogicalMin;
            public int VALUE_LogicalMax;
            public int VALUE_PhysicalMin;
            public int VALUE_PhysicalMax;

            public ushort UsageIndex;
            public ushort UsageMax; // if IsRange
            public ushort StringIndex;
            public ushort StringMax; // if IsStringRange
            public ushort DesignatorIndex;
            public ushort DesignatorMax; // if IsDesignatorRange
            public ushort DataIndex;
            public ushort DataIndexMax; // if IsRange?
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct USB_NODE_CONNECTION_DRIVERKEY_NAME
        {
            public uint ConnectionIndex;
            public uint ActualLength;
            public fixed char NodeName[1024];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_DEVICE_DESCRIPTOR
        {
            public byte bLength;
            public byte bDescriptorType;
            public ushort bcdUSB;
            public byte bDeviceClass;
            public byte bDeviceSubClass;
            public byte bDeviceProtocol;
            public byte bMaxPacketSize0;
            public ushort idVendor;
            public ushort idProduct;
            public ushort bcdDevice;
            public byte iManufacturer;
            public byte iProduct;
            public byte iSerialNumber;
            public byte bNumConfigurations;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_ENDPOINT_DESCRIPTOR
        {
            public byte bLength;
            public byte bDescriptorType;
            public byte bEndpointAddress;
            public byte bmAttributes;
            public ushort wMaxPacketSize;
            public byte bInterval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_PIPE_INFO
        {
            public USB_ENDPOINT_DESCRIPTOR EndpointDescriptor;
            public uint ScheduleOffset;
        }

        public enum USB_CONNECTION_STATUS
        {
            NoDeviceConnected,
            DeviceConnected,
            // others
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USB_NODE_CONNECTION_INFORMATION
        {
            public uint ConnectionIndex;
            public USB_DEVICE_DESCRIPTOR DeviceDescriptor;
            public byte CurrentConfigurationValue;
            public byte LowSpeed;
            public byte DeviceIsHub;
            public ushort DeviceAddress;
            public uint NumberOfOpenPipes;
            public USB_CONNECTION_STATUS ConnectionStatus;
            public USB_PIPE_INFO PipeInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DCB
        {
            public int DCBlength;
            public uint BaudRate;
            public uint fFlags;
            public bool fBinary { get { return GetBool(0); } set { SetBool(0, value); } }
            public bool fParity { get { return GetBool(1); } set { SetBool(1, value); } }
            public bool fOutxCtsFlow { get { return GetBool(2); } set { SetBool(2, value); } }
            public bool fOutxDsrFlow { get { return GetBool(3); } set { SetBool(3, value); } }
            public uint fDtrControl { get { return GetBits(4, 2); } set { SetBits(4, 2, value); } }
            public bool fDsrSensitivity { get { return GetBool(6); } set { SetBool(6, value); } }
            public bool fTXContinueOnXoff { get { return GetBool(7); } set { SetBool(7, value); } }
            public bool fOutX { get { return GetBool(8); } set { SetBool(8, value); } }
            public bool fInX { get { return GetBool(9); } set { SetBool(9, value); } }
            public bool fErrorChar { get { return GetBool(10); } set { SetBool(10, value); } }
            public bool fNull { get { return GetBool(11); } set { SetBool(11, value); } }
            public uint fRtsControl { get { return GetBits(12, 2); } set { SetBits(12, 2, value); } }
            public bool fAbortOnError { get { return GetBool(14); } set { SetBool(14, value); } }
            ushort Reserved1;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public byte XonChar;
            public byte XoffChar;
            public byte ErrorChar;
            public byte EofChar;
            public byte EvtChar;
            ushort Reserved2;

            static uint GetBitMask(int bitCount)
            {
                return (1u << bitCount) - 1;
            }

            uint GetBits(int bitOffset, int bitCount)
            {
                return (fFlags >> bitOffset) & GetBitMask(bitCount);
            }

            void SetBits(int bitOffset, int bitCount, uint value)
            {
                uint mask = GetBitMask(bitCount); fFlags &= ~(mask << bitOffset); fFlags |= (value & mask) << bitOffset;
            }

            bool GetBool(int bitOffset)
            {
                return GetBits(bitOffset, 1) != 0;
            }

            void SetBool(int bitOffset, bool value)
            {
                SetBits(bitOffset, 1, value ? 1u : 0);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct BTH_LE_UUID
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.I1)]
            public byte IsShortUuid;

            [FieldOffset(4)]
            public ushort ShortUuid;

            [FieldOffset(4)]
            public Guid LongUuid;

            public BleUuid ToGuid()
            {
                return IsShortUuid != 0 ? new BleUuid(ShortUuid) : new BleUuid(LongUuid);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_SERVICE
        {
            public BTH_LE_UUID ServiceUuid;
            public ushort AttributeHandle;
        }

        [StructLayout(LayoutKind.Explicit, Size = 36)]
        public struct BTH_LE_GATT_CHARACTERISTIC
        {
            public const int Size = 36;

            [FieldOffset(0)]
            public ushort ServiceHandle;

            [FieldOffset(4)]
            public BTH_LE_UUID CharacteristicUuid;

            [FieldOffset(24)]
            public ushort AttributeHandle;

            [FieldOffset(26)]
            public ushort CharacteristicValueHandle;

            [FieldOffset(28)]
            public byte IsBroadcastable;

            [FieldOffset(29)]
            public byte IsReadable;
            
            [FieldOffset(30)]
            public byte IsWritable;

            [FieldOffset(31)]
            public byte IsWritableWithoutResponse;

            [FieldOffset(32)]
            public byte IsSignedWritable;

            [FieldOffset(33)]
            public byte IsNotifiable;

            [FieldOffset(34)]
            public byte IsIndicatable;

            [FieldOffset(35)]
            public byte HasExtendedProperties;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_GATT_VALUE_CHANGED_EVENT
        {
            public ushort ChangedAttributeHandle;
            public UIntPtr CharacteristicValueDataSize;
            public BTH_LE_GATT_CHARACTERISTIC_VALUE* CharacteristicValue;
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        public struct BLUETOOTH_GATT_VALUE_CHANGED_EVENT_REGISTRATION
        {
            public const int Size = 4;

            [FieldOffset(0)]
            public ushort NumCharacteristics;

            //[FieldOffset(4)]
            //public fixed BTH_LE_GATT_CHARACTERISTIC Characteristics[1];
        }

        [StructLayout(LayoutKind.Explicit, Size = BTH_LE_GATT_CHARACTERISTIC_VALUE.Size)]
        public struct BTH_LE_GATT_CHARACTERISTIC_VALUE
        {
            public const int Size = 4;

            [FieldOffset(0)]
            public uint DataSize;

            [FieldOffset(4)]
            public fixed byte Data[1];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_DESCRIPTOR
        {
            public ushort ServiceHandle;
            public ushort CharacteristicHandle;
            public BTH_LE_GATT_DESCRIPTOR_TYPE DescriptorType;
            public BTH_LE_UUID DescriptorUuid;
            public ushort AttributeHandle;
        }

        public enum BTH_LE_GATT_DESCRIPTOR_TYPE
        {
            CharacteristicExtendedProperties,
            CharacteristicUserDescription,
            ClientCharacteristicConfiguration,
            ServerCharacteristicConfiguration,
            CharacteristicFormat,
            CharacteristicAggregateFormat,
            CustomDescriptor
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE_EXTENDED_PROPERTIES
        {
            public byte IsReliableWriteEnabled;
            public byte IsAuxiliariesWritable;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE_CCCD
        {
            public byte IsSubscribeToNotification;
            public byte IsSubscribeToIndication;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE_SCCD
        {
            public byte IsBroadcast;
        }

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE_FORMAT
        {
            [FieldOffset(0)]
            public byte Format;

            [FieldOffset(1)]
            public byte Exponent;

            [FieldOffset(4)]
            public BTH_LE_UUID Unit;

            [FieldOffset(24)]
            public byte Namespace;

            [FieldOffset(28)]
            public BTH_LE_UUID Description;
        }

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE_PARAMS
        {
            [FieldOffset(0)]
            public BTH_LE_GATT_DESCRIPTOR_VALUE_EXTENDED_PROPERTIES ExtendedProperties;

            [FieldOffset(0)]
            public BTH_LE_GATT_DESCRIPTOR_VALUE_CCCD Cccd;

            [FieldOffset(0)]
            public BTH_LE_GATT_DESCRIPTOR_VALUE_SCCD Sccd;

            [FieldOffset(0)]
            public BTH_LE_GATT_DESCRIPTOR_VALUE_FORMAT Format;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_LE_GATT_DESCRIPTOR_VALUE
        {
            public const int Size = 4 + 20 + 48 + BTH_LE_GATT_CHARACTERISTIC_VALUE.Size;

            public BTH_LE_GATT_DESCRIPTOR_TYPE DescriptorType;
            public BTH_LE_UUID DescriptorUuid;
            public BTH_LE_GATT_DESCRIPTOR_VALUE_PARAMS Params;
            public BTH_LE_GATT_CHARACTERISTIC_VALUE Value;
        }

        public enum BTH_LE_GATT_EVENT_TYPE
        {
            CharacteristicValueChangedEvent
        }

        public delegate void BLUETOOTH_GATT_EVENT_CALLBACK(BTH_LE_GATT_EVENT_TYPE eventType,
                                                           BLUETOOTH_GATT_VALUE_CHANGED_EVENT* eventParameter,
                                                           IntPtr context);

        [Flags]
        public enum BLUETOOTH_GATT_FLAGS : uint
        {
            ENCRYPTED = 1,
            AUTHENTICATED = 2,
            FORCE_READ_FROM_DEVICE = 4,
            FORCE_READ_FROM_CACHE = 8,
            SIGNED_WRITE = 16,
            WRITE_WITHOUT_RESPONSE = 32
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public int dwSize;
            public int fReturnAuthenticated;
            public int fReturnRemembered;
            public int fReturnUnknown;
            public int fReturnConnected;
            public int fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_DEVICE_INFO
        {
            public int dwSize;
            public BLUETOOTH_ADDRESS Address;
            public uint ulClassOfDevice;
            public int fConnected;
            public int fRemembered;
            public int fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            public fixed char szName[BLUETOOTH_MAX_NAME_SIZE];
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct BLUETOOTH_ADDRESS
        {
            [FieldOffset(0)]
            public ulong Addr;

            [FieldOffset(0)]
            public fixed byte Bytes[6];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_DEVICE_INFO
        {
            public BDIF flags;
            public ulong address;
            public uint classOfDevice;
            public fixed byte name[BLUETOOTH_MAX_NAME_SIZE];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_HCI_EVENT_INFO
        {
            public ulong bthAddress;
            public byte connectionType;
            public byte connected;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BTH_RADIO_IN_RANGE
        {
            public BTH_DEVICE_INFO deviceInfo;
            public BDIF previousDeviceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear, wMonth, wDayOfWeek, wDay;
            public ushort wHour, wMinute, wSecond, wMilliseconds;
        }

        [Flags]
        public enum BDIF : uint
        {
            Address          = 0x0000001,
            Cod              = 0x0000002,
            Name             = 0x0000004,
            Paired           = 0x0000008,
            Personal         = 0x0000010,
            Connected        = 0x0000020,
            ShortName        = 0x0000040,
            Visible          = 0x0000080,
            SspSupported     = 0x0000100,
            SspPaired        = 0x0000200,
            SspMitmProtected = 0x0000400,
            Rssi             = 0x0001000,
            Eir              = 0x0002000,
            Br               = 0x0004000, // Windows 8
            Le               = 0x0008000, // Windows 8
            LePaired         = 0x0010000, // Windows 8
            LePersonal       = 0x0020000, // Windows 8
            LeMitmProtected  = 0x0040000, // Windows 8
            LePrivacyEnabled = 0x0080000, // Windows 8
            LeRandomAddress  = 0x0100000, // Windows 8
            LeDiscoverable   = 0x0200000, // Windows 10
            LeName           = 0x0400000, // Windows 10
            Unknown1         = 0x1000000, // ???
            Unknown2         = 0x2000000  // ???
        }

        public static IntPtr CreateAutoResetEventOrThrow()
        {
            return CreateResetEventOrThrow(false);
        }

        public static IntPtr CreateManualResetEventOrThrow()
        {
            return CreateResetEventOrThrow(true);
        }

        static IntPtr CreateResetEventOrThrow(bool manualReset)
        {
            IntPtr @event = CreateEvent(IntPtr.Zero, manualReset, false, IntPtr.Zero);
            if (@event == IntPtr.Zero) { throw new IOException("Event creation failed."); }
            return @event;
        }

        public unsafe static void OverlappedOperation(IntPtr ioHandle,
            IntPtr eventHandle, int eventTimeout, IntPtr closeEventHandle,
            bool overlapResult,
            NativeOverlapped* overlapped, out uint bytesTransferred)
        {
            bool closed = false;

            if (!overlapResult)
            {
                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != ERROR_IO_PENDING)
                {
                    var ex = new Win32Exception();
                    throw new IOException(string.Format("Operation failed early: {0}", ex.Message), ex);
                }

                IntPtr* handles = stackalloc IntPtr[2];
                handles[0] = eventHandle; handles[1] = closeEventHandle;
                uint waitResult = WaitForMultipleObjects(2, handles, false, WaitForMultipleObjectsGetTimeout(eventTimeout));
                switch (waitResult)
                {
                    case WAIT_OBJECT_0: break;
                    case WAIT_OBJECT_1: closed = true; goto default;
                    default: CancelIo(ioHandle); break;
                }
            }

            if (!GetOverlappedResult(ioHandle, overlapped, out bytesTransferred, true))
            {
                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != ERROR_HANDLE_EOF)
                {
                    if (closed)
                    {
                        throw CommonException.CreateClosedException();
                    }

                    if (win32Error == ERROR_OPERATION_ABORTED)
                    {
                        throw new TimeoutException("Operation timed out.");
                    }

                    throw new IOException("Operation failed after some time.", new Win32Exception());
                }

                bytesTransferred = 0;
            }
        }

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Child(out uint childDevInst, uint devInst, int flags = 0);

        public static int CM_Get_Device_ID(uint devInst, out string deviceID)
        {
            int ret; deviceID = null;
            
            int length;
            ret = CM_Get_Device_ID_Size(out length, devInst);
            if (ret != 0) { return ret; }

            var chars = new char[length + 1];
            ret = CM_Get_Device_ID(devInst, chars, chars.Length);
            if (ret != 0) { return ret; }

            deviceID = new string(chars, 0, length);
            return 0;
        }

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        public static extern int CM_Get_Device_ID(uint devInst, char[] buffer, int length, int flags = 0);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Device_ID_Size(out int length, uint devInst, int flags = 0);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Parent(out uint parentDevInst, uint devInst, int flags = 0);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_Sibling(out uint siblingDevInst, uint devInst, int flags = 0);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_DevNode_Status(out uint status, out uint problemNumber, uint devInst, int flags = 0);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        public static extern int CM_Locate_DevNode(out uint devInst, string deviceID, int flags = 0);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        public static extern int CM_Get_DevNode_Registry_Property(uint devInst, uint property, uint* dataType, void* buffer, ref uint length, uint flags);

        [DllImport("cfgmgr32.dll")]
        public static extern uint CMP_WaitNoPendingInstallEvents(uint timeout);

        [DllImport("user32.dll")]
        public static extern ushort RegisterClass(ref WNDCLASS windowClass);

        public const int CW_USEDEFAULT = unchecked((int)0x80000000);
        public static readonly IntPtr HWND_MESSAGE = (IntPtr)(-3);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateWindowEx(uint exStyle,
                                                   string className, string windowName,
                                                   uint style, int x, int y, int width, int height,
                                                   IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr WindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessage(out MSG message, IntPtr window, uint messageMin, uint messageMax);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage(ref MSG message);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG message);

        public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        [DllImport("user32.dll")]
        public static extern IntPtr RegisterDeviceNotification(IntPtr recipient, ref DEV_BROADCAST_DEVICEINTERFACE notificationFilter, int flags);

        public const int DBT_DEVTYP_HANDLE = 6;
        [DllImport("user32.dll")]
        public static extern IntPtr RegisterDeviceNotification(IntPtr recipient, ref DEV_BROADCAST_HANDLE notificationFilter, int flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterDeviceNotification(IntPtr handle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterClass(string className, IntPtr instance);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVersionEx(ref OSVERSIONINFO version);
         
        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(out Guid hidGuid);

        public static Guid HidD_GetHidGuid()
        {
            Guid guid; HidD_GetHidGuid(out guid); return guid;
        }

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetAttributes(IntPtr handle, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetManufacturerString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetProductString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetSerialNumberString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_GetFeature(IntPtr handle, byte* buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_SetFeature(IntPtr handle, byte* buffer, int bufferLength);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_SetNumInputBuffers(IntPtr handle, int count);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_GetPreparsedData(IntPtr handle, out IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_FreePreparsedData(IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_GetCaps(IntPtr preparsed, out HIDP_CAPS caps);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_SetData(HIDP_REPORT_TYPE reportType, ref HIDP_DATA dataList, ref int dataCount, IntPtr preparsed, byte[] report, int reportLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_SetUsages(HIDP_REPORT_TYPE reportType, ushort usagePage, ushort linkCollection, ref ushort usage, ref int usageCount, IntPtr preparsed, byte[] report, int reportLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_SetUsageValueArray(HIDP_REPORT_TYPE reportType, ushort usagePage, ushort linkCollection, ushort usage, byte[] usageValue, ushort usageValueLength, IntPtr preparsed, byte[] report, int reportLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_GetLinkCollectionNodes([Out] HIDP_LINK_COLLECTION_NODE[] nodes, ref int count, IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_GetButtonCaps(HIDP_REPORT_TYPE reportType, [Out] HIDP_DATA_CAPS[] buttons, ref ushort count, IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_GetValueCaps(HIDP_REPORT_TYPE reportType, [Out] HIDP_DATA_CAPS[] values, ref ushort count, IntPtr preparsed);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegOpenKeyEx(IntPtr parentHandle, string subkey, uint options, uint access, out IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegNotifyChangeKeyValue(IntPtr handle, bool watchSubtree, uint notifyFilter, IntPtr @event, bool asynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RegQueryValueEx(IntPtr handle, string valueName, uint reserved, IntPtr type, char[] buffer, ref int lengthInBytes);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern HDEVINFO SetupDiGetClassDevs
            (IntPtr classGuid, string enumerator, IntPtr hwndParent, DIGCF flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern HDEVINFO SetupDiGetClassDevs
            ([MarshalAs(UnmanagedType.LPStruct)] Guid classGuid, string enumerator, IntPtr hwndParent, DIGCF flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiDestroyDeviceInfoList(HDEVINFO deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInfo(HDEVINFO deviceInfoSet, int memberIndex,
            ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInterfaces(HDEVINFO deviceInfoSet, IntPtr deviceInfoData,
            [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceClassGuid, int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInterfaces(HDEVINFO deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData,
            [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceClassGuid, int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize, IntPtr requiredSize, IntPtr deviceInfoData);

        public static bool SetupDiGetDeviceInterfaceDetail(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            out SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData)
        {
            deviceInterfaceDetailData = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA();
            deviceInterfaceDetailData.Size = IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize);

            if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet,
                ref deviceInterfaceData, ref deviceInterfaceDetailData,
                Marshal.SizeOf(deviceInterfaceDetailData) - 4, IntPtr.Zero, IntPtr.Zero))
            {
                return true;
            }
            else
            {
                deviceInterfaceDetailData = default(SP_DEVICE_INTERFACE_DETAIL_DATA);
                return false;
            }
        }

        public static bool SetupDiGetDeviceInterfaceDevicePath(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            out string devicePath)
        {
            NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA didetail;

            if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, out didetail))
            {
                devicePath = didetail.DevicePath; return true;
            }

            devicePath = null; return false;
        }

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceRegistryProperty(HDEVINFO deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property, out uint propertyDataType,
            char[] buffer, int lengthInBytes, IntPtr lengthInBytesRequired);

        public static bool TryGetDeviceRegistryProperty(HDEVINFO deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property, out string value)
        {
            value = null;

            uint propertyDataType; char[] propertyValueChars = new char[64]; int propertyValueLength = 63 * 2;
            if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, out propertyDataType,
                propertyValueChars, propertyValueLength, IntPtr.Zero))
            {
                if (propertyDataType == REG_SZ)
                {
                    value = NativeMethods.NTString(propertyValueChars);
                }
            }

            return value != null;
        }

        public static bool TryGetSerialPortFriendlyName(HDEVINFO deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, out string friendlyName)
        {
            if (NativeMethods.TryGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_FRIENDLYNAME, out friendlyName))
            {

            }
            else if (NativeMethods.TryGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_DEVICEDESC, out friendlyName))
            {

            }
            else
            {
                friendlyName = null;
            }

            return !string.IsNullOrEmpty(friendlyName);
        }

        public static bool TryGetSerialPortName(HDEVINFO deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, out string portName)
        {
            portName = null;

            IntPtr hkey = NativeMethods.SetupDiOpenDevRegKey(deviceInfoSet, ref deviceInfoData);
            if (hkey != (IntPtr)(-1))
            {
                try
                {
                    char[] portNameChars = new char[64]; int portNameLength = 63 * 2;
                    if (0 == NativeMethods.RegQueryValueEx(hkey, "PortName", 0, IntPtr.Zero, portNameChars, ref portNameLength))
                    {
                        Array.Resize(ref portNameChars, portNameLength / 2);

                        string newPortName = NativeMethods.NTString(portNameChars);
                        if (newPortName.Length >= 4 && newPortName.StartsWith("COM"))
                        {
                            int newPortNumber;
                            if (int.TryParse(newPortName.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out newPortNumber) &&
                                newPortName == "COM" + newPortNumber.ToString(CultureInfo.InvariantCulture))
                            {
                                portName = newPortName;
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethods.RegCloseKey(hkey);
                }
            }

            return portName != null;
        }

        public delegate void EnumerateDeviceInterfacesCallback(NativeMethods.HDEVINFO deviceInfoSet,
                                               NativeMethods.SP_DEVINFO_DATA deviceInfoData,
                                               NativeMethods.SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                                               string deviceID, string devicePath);
        public static void EnumerateDeviceInterfaces(Guid guid, EnumerateDeviceInterfacesCallback callback)
        {
            EnumerateDeviceInterfaces(guid, null, callback);
        }

        public static void EnumerateDeviceInterfaces(Guid guid, string deviceIDToFilterTo, EnumerateDeviceInterfacesCallback callback)
        {
            EnumerateDevicesCore(NativeMethods.SetupDiGetClassDevs(guid, deviceIDToFilterTo, IntPtr.Zero, NativeMethods.DIGCF.DeviceInterface | NativeMethods.DIGCF.Present),
                (devInfo, dvi, deviceID) =>
                {
                    NativeMethods.SP_DEVICE_INTERFACE_DATA did = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    did.Size = Marshal.SizeOf(did);

                    for (int i = 0; NativeMethods.SetupDiEnumDeviceInterfaces(devInfo, ref dvi, guid, i, ref did); i++)
                    {
                        string devicePath;
                        if (NativeMethods.SetupDiGetDeviceInterfaceDevicePath(devInfo, ref did, out devicePath))
                        {
                            callback(devInfo, dvi, did, deviceID, devicePath);
                        }
                    }
                });
        }


        public delegate void EnumerateDevicesCallback(NativeMethods.HDEVINFO deviceInfoSet,
                                               NativeMethods.SP_DEVINFO_DATA deviceInfoData,
                                               string deviceID);
        public static void EnumerateDevices(Guid guid, EnumerateDevicesCallback callback)
        {
            EnumerateDevicesCore(NativeMethods.SetupDiGetClassDevs(guid, null, IntPtr.Zero, NativeMethods.DIGCF.Present), callback);
        }

        static void EnumerateDevicesCore(NativeMethods.HDEVINFO devInfo, EnumerateDevicesCallback callback)
        {
            if (devInfo.IsValid)
            {
                try
                {
                    NativeMethods.SP_DEVINFO_DATA dvi = new NativeMethods.SP_DEVINFO_DATA();
                    dvi.Size = Marshal.SizeOf(dvi);

                    for (int j = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, j, ref dvi); j++)
                    {
                        string deviceID;
                        if (0 != NativeMethods.CM_Get_Device_ID(dvi.DevInst, out deviceID)) { continue; }

                        callback(devInfo, dvi, deviceID);
                    }
                }
                finally
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);
                }
            }
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiOpenDevRegKey(HDEVINFO deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            int scope = DICS_FLAG_GLOBAL,
            int profile = 0,
            int keyType = DIREG_DEV,
            uint desiredAccess = KEY_READ);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiOpenDeviceInterfaceRegKey(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            uint reserved = 0,
            uint desiredAccess = KEY_READ);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr eventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool manualReset,
            [MarshalAs(UnmanagedType.Bool)] bool initialState,
            IntPtr name);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(string filename, EFileAccess desiredAccess,
            EFileShare shareMode, IntPtr securityAttributes,
            ECreationDisposition creationDisposition, EFileAttributes attributes, IntPtr template);

        public static IntPtr CreateFileFromDevice(string filename, EFileAccess desiredAccess, EFileShare shareMode)
        {
            return CreateFile(filename, desiredAccess, shareMode, IntPtr.Zero,
                ECreationDisposition.OpenExisting,
                EFileAttributes.Device | EFileAttributes.Overlapped,
                IntPtr.Zero);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

		public static bool CloseHandle(ref IntPtr handle)
		{
			if (!CloseHandle(handle)) { return false; }
			handle = IntPtr.Zero; return true;
		}
		
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool ReadFile(IntPtr handle, byte* buffer, int bytesToRead,
            IntPtr bytesRead, NativeOverlapped* overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool WriteFile(IntPtr handle, byte* buffer, int bytesToWrite,
            IntPtr bytesWritten, NativeOverlapped* overlapped);

        public static string NTString(char[] buffer)
        {
            int index = Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, index >= 0 ? index : buffer.Length);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CancelIo(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool DeviceIoControl(IntPtr handle,
            uint ioControlCode, void* inBuffer, uint inBufferSize, void* outBuffer, uint outBufferSize,
            out uint bytesReturned, NativeOverlapped* overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCommState(IntPtr handle, ref DCB dcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCommState(IntPtr handle, ref DCB dcb);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCommTimeouts(IntPtr handle, ref COMMTIMEOUTS timeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCommTimeouts(IntPtr handle, out COMMTIMEOUTS timeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PurgeComm(IntPtr handle, uint flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlushFileBuffers(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetOverlappedResult(IntPtr handle,
            NativeOverlapped* overlapped, out uint bytesTransferred,
            [MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ResetEvent(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEvent(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern uint WaitForMultipleObjects(uint count, IntPtr* handles,
            [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint milliseconds);

        public static uint WaitForMultipleObjectsGetTimeout(int eventTimeout)
        {
            return eventTimeout < 0 ? ~(uint)0 : (uint)eventTimeout;
        }

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTGetServices(IntPtr handle,
                                                          ushort allocatedCount, [Out] BTH_LE_GATT_SERVICE[] services,
                                                          out ushort returnedCount,
                                                          uint flags = 0);


        public static NativeMethods.BTH_LE_GATT_SERVICE[] BluetoothGATTGetServices(IntPtr handle)
        {
            int error;

            ushort allocated;
            error = NativeMethods.BluetoothGATTGetServices(handle, 0, null, out allocated);
            if (error != NativeMethods.ERROR_MORE_DATA) { return null; }

            var services = new NativeMethods.BTH_LE_GATT_SERVICE[allocated];
            if (allocated > 0)
            {
                ushort returned;
                error = NativeMethods.BluetoothGATTGetServices(handle, allocated, services, out returned);
                if (error != 0) { return null; }
                if (allocated != returned) { return null; }
            }

            return services;
        }

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTGetCharacteristics(IntPtr handle,
                                                                 [In] ref BTH_LE_GATT_SERVICE service,
                                                                 ushort allocatedCount, [Out] BTH_LE_GATT_CHARACTERISTIC[] characteristics,
                                                                 out ushort returnedCount,
                                                                 uint flags = 0);

        public static BTH_LE_GATT_CHARACTERISTIC[] BluetoothGATTGetCharacteristics(IntPtr handle,
                                                                                   ref BTH_LE_GATT_SERVICE service)
        {
            int error;

            ushort allocated;
            error = NativeMethods.BluetoothGATTGetCharacteristics(handle, ref service,  0, null, out allocated);
            if (error == NativeMethods.ERROR_NOT_FOUND) { allocated = 0; } else
            if (error != NativeMethods.ERROR_MORE_DATA) { return null; }

            var characteristics = new NativeMethods.BTH_LE_GATT_CHARACTERISTIC[allocated];
            if (allocated > 0)
            {
                ushort returned;
                error = NativeMethods.BluetoothGATTGetCharacteristics(handle, ref service, allocated, characteristics, out returned);
                if (error != 0) { return null; }
                if (allocated != returned) { return null; }
            }

            return characteristics;
        }

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTGetDescriptors(IntPtr handle,
                                                             [In] ref BTH_LE_GATT_CHARACTERISTIC characteristic,
                                                             ushort allocatedCount, [Out] BTH_LE_GATT_DESCRIPTOR[] descriptors,
                                                             out ushort returnedCount,
                                                             uint flags = 0);

        public static BTH_LE_GATT_DESCRIPTOR[] BluetoothGATTGetDescriptors(IntPtr handle,
                                                                           ref BTH_LE_GATT_CHARACTERISTIC characteristic)
        {
            int error;

            ushort allocated;
            error = NativeMethods.BluetoothGATTGetDescriptors(handle, ref characteristic, 0, null, out allocated);
            if (error == NativeMethods.ERROR_NOT_FOUND) { allocated = 0; } else
            if (error != NativeMethods.ERROR_MORE_DATA) { return null; }

            var descriptors = new NativeMethods.BTH_LE_GATT_DESCRIPTOR[allocated];
            if (allocated > 0)
            {
                ushort returned;
                error = NativeMethods.BluetoothGATTGetDescriptors(handle, ref characteristic, allocated, descriptors, out returned);
                if (error != 0) { return null; }
                if (allocated != returned) { return null; }
            }

            return descriptors;
        }

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTGetCharacteristicValue(IntPtr handle,
                                                                     [In] ref BTH_LE_GATT_CHARACTERISTIC characteristic,
                                                                     uint valueDataSize,
                                                                     BTH_LE_GATT_CHARACTERISTIC_VALUE* value,
                                                                     out ushort valueSizeRequired,
                                                                     BLUETOOTH_GATT_FLAGS flags);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTSetCharacteristicValue(IntPtr handle,
                                                                     [In] ref BTH_LE_GATT_CHARACTERISTIC characteristic,
                                                                     BTH_LE_GATT_CHARACTERISTIC_VALUE* value,
                                                                     ulong reliableWriteContext,
                                                                     BLUETOOTH_GATT_FLAGS flags);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTGetDescriptorValue(IntPtr handle,
                                                                 [In] ref BTH_LE_GATT_DESCRIPTOR descriptor,
                                                                 uint valueDataSize,
                                                                 BTH_LE_GATT_DESCRIPTOR_VALUE* value,
                                                                 out ushort valueSizeRequired,
                                                                 BLUETOOTH_GATT_FLAGS flags);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTSetDescriptorValue(IntPtr handle,
                                                                 [In] ref BTH_LE_GATT_DESCRIPTOR descriptor,
                                                                 BTH_LE_GATT_DESCRIPTOR_VALUE* value,
                                                                 BLUETOOTH_GATT_FLAGS flags);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTRegisterEvent(IntPtr handle,
                                                            BTH_LE_GATT_EVENT_TYPE eventType,
                                                            BLUETOOTH_GATT_VALUE_CHANGED_EVENT_REGISTRATION* eventParameter,
                                                            BLUETOOTH_GATT_EVENT_CALLBACK callback,
                                                            IntPtr context,
                                                            out IntPtr eventHandle,
                                                            int flags = 0);

        [DllImport("BluetoothAPIs.dll", SetLastError = true)]
        public static extern int BluetoothGATTUnregisterEvent(IntPtr eventHandle, int flags = 0);

        public static bool TryOpenToGetInfo(string path, Func<IntPtr, bool> action)
        {
            var handle = NativeMethods.CreateFileFromDevice(path, NativeMethods.EFileAccess.None, NativeMethods.EFileShare.Read | NativeMethods.EFileShare.Write);
            if (handle == (IntPtr)(-1)) { return false; }

            try
            {
                return action(handle);
            }
            catch (Exception e)
            {
                HidSharpDiagnostics.Trace("CreateFileFromDevice failed: {0}", e);
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }

            return false;
        }

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS @params, out IntPtr radioHandle);

        [DllImport("bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BluetoothFindNextRadio(IntPtr searchHandle, out IntPtr radioHandle);

        [DllImport("bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BluetoothFindRadioClose(IntPtr searchHandle);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS @params, ref BLUETOOTH_DEVICE_INFO info);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindNextDevice(IntPtr searchHandle, ref BLUETOOTH_DEVICE_INFO info);

        [DllImport("bthprops.cpl", SetLastError = true)]
        public static extern bool BluetoothFindDeviceClose(IntPtr searchHandle);
    }
}
