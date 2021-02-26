#region License
/* Copyright 2012-2013, 2017-2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Linux
{
    static class NativeMethods
    {
		const string libc = "libc";

		public enum error
		{
			OK = 0,
			EPERM = 1,
			EINTR = 4,
			EIO = 5,
			ENXIO = 6,
			EBADF = 9,
			EAGAIN = 11,
			EACCES = 13,
			EBUSY = 16,
			ENODEV = 19,
			EINVAL = 22
		}
			
		[Flags]
		public enum oflag
		{
			RDONLY = 0x000,
			WRONLY = 0x001,
			RDWR = 0x002,
			CREAT = 0x040,
			EXCL = 0x080,
            NOCTTY = 0x100,
			TRUNC = 0x200,
			APPEND = 0x400,
			NONBLOCK = 0x800
		}
	
		[Flags]
		public enum pollev : short
		{
			IN = 0x01,
			PRI = 0x02,
			OUT = 0x04,
			ERR = 0x08,
			HUP = 0x10,
			NVAL = 0x20
		}

		public struct pollfd
		{
			public int fd;
			public pollev events;
			public pollev revents;
		}

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

		public static bool uname(out string sysname, out Version release)
		{
			string releaseStr; release = null;
			if (!uname(out sysname, out releaseStr)) { return false; }
            releaseStr = new string(releaseStr.Trim().TakeWhile(ch => (ch >= '0' && ch <= '9') || ch == '.').ToArray());
			release = new Version(releaseStr);
			return true;
		}

        public static bool uname(out string sysname, out string release)
        {
            string syscallPath = "Mono.Unix.Native.Syscall, Mono.Posix, PublicKeyToken=0738eb9f132ed756";
            var syscall = Type.GetType(syscallPath);
            if (syscall != null)
            {
                var unameArgs = new object[1];
                int unameRet = (int)syscall.InvokeMember("uname",
                                                         BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, unameArgs,
                                                         CultureInfo.InvariantCulture);
                if (unameRet >= 0)
                {
                    var uname = unameArgs[0];
                    Func<string, string> getMember = s => (string)uname.GetType().InvokeMember(s,
                                                                                               BindingFlags.GetField, null, uname, new object[0],
                                                                                               CultureInfo.InvariantCulture);
                    sysname = getMember("sysname"); release = getMember("release");
                    return true;
                }
            }

            try
            {
                if (File.Exists("/proc/sys/kernel/ostype") && File.Exists("/proc/sys/kernel/osrelease"))
                {
                    sysname = File.ReadAllText("/proc/sys/kernel/ostype").TrimEnd('\n');
                    release = File.ReadAllText("/proc/sys/kernel/osrelease").TrimEnd('\n');
                    if (sysname != "" && release != "") { return true; }
                }
            }
            catch
            {

            }

            sysname = null; release = null;
            return false;
        }
		
		[DllImport(libc, SetLastError = true)]
		public static extern int open(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string filename,
			 oflag oflag);
		
		[DllImport(libc, SetLastError = true)]
		public static extern int close(int filedes);

		[DllImport(libc, SetLastError = true)]
		public static extern IntPtr read(int filedes, IntPtr buffer, UIntPtr size);
		
		[DllImport(libc, SetLastError = true)]
		public static extern IntPtr write(int filedes, IntPtr buffer, UIntPtr size);

		[DllImport(libc, SetLastError = true)]
		public static extern int poll(pollfd[] fds, IntPtr nfds, int timeout);

        [DllImport(libc, SetLastError = true)]
        public static extern int poll(ref pollfd fds, IntPtr nfds, int timeout);

        public static bool TryParseHex(string hex, out int result)
        {
            return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseVersion(string version, out int major, out int minor)
        {
            major = 0; minor = 0; if (version == null) { return false; }
            string[] parts = version.Split(new[] { '.' }, 2); if (parts.Length != 2) { return false; }
            return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
        }

        #region ioctl
        // TODO: Linux changes these up depending on platform. Eventually we'll handle it.
        //       For now, x86 and ARM are safe with this definition.
        public const int IOC_NONE = 0;
        public const int IOC_WRITE = 1;
        public const int IOC_READ = 2;
        public const int IOC_NRBITS = 8;
        public const int IOC_TYPEBITS = 8;
        public const int IOC_SIZEBITS = 14;
        public const int IOC_DIRBITS = 2;
        public const int IOC_NRSHIFT = 0;
        public const int IOC_TYPESHIFT = IOC_NRSHIFT + IOC_NRBITS;
        public const int IOC_SIZESHIFT = IOC_TYPESHIFT + IOC_TYPEBITS;
        public const int IOC_DIRSHIFT = IOC_SIZESHIFT + IOC_SIZEBITS;

        public static UIntPtr IOC(int dir, int type, int nr, int size)
        {
            // Make sure to cast this to uint. We do NOT want this casted from int...
            uint value = (uint)dir << IOC_DIRSHIFT | (uint)type << IOC_TYPESHIFT | (uint)nr << IOC_NRSHIFT | (uint)size << IOC_SIZESHIFT;
            return (UIntPtr)value;
        }

        public static UIntPtr IOW(int type, int nr, int size)
        {
            return IOC(IOC_WRITE, type, nr, size);
        }

        public static UIntPtr IOR(int type, int nr, int size)
        {
            return IOC(IOC_READ, type, nr, size);
        }

        public static UIntPtr IOWR(int type, int nr, int size)
        {
            return IOC(IOC_WRITE | IOC_READ, type, nr, size);
        }

        #region hidraw
        public const int HID_MAX_DESCRIPTOR_SIZE = 4096;
        public static readonly UIntPtr HIDIOCGRDESCSIZE = IOR((byte)'H', 1, 4);
        public static readonly UIntPtr HIDIOCGRDESC = IOR((byte)'H', 2, Marshal.SizeOf(typeof(hidraw_report_descriptor)));
        public static UIntPtr HIDIOCSFEATURE(int length) { return IOWR((byte)'H', 6, length); }
        public static UIntPtr HIDIOCGFEATURE(int length) { return IOWR((byte)'H', 7, length); }

        public struct hidraw_report_descriptor
        {
            public uint size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = HID_MAX_DESCRIPTOR_SIZE)]
            public byte[] value;
        }

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, UIntPtr command, out uint value);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, UIntPtr command, ref hidraw_report_descriptor value);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, UIntPtr command, IntPtr value);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, UIntPtr command, ref termios value);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, UIntPtr command);
        #endregion
        #endregion

        #region termios
        public static readonly UIntPtr TIOCEXCL = (UIntPtr)0x540c;
        public static readonly UIntPtr TIOCNXCL = (UIntPtr)0x540d;

        public static readonly UIntPtr TCGETS2 = IOR((byte)'T', 0x2a, Marshal.SizeOf(typeof(termios)));
        public static readonly UIntPtr TCSETS2 = IOW((byte)'T', 0x2b, Marshal.SizeOf(typeof(termios)));
        public static readonly UIntPtr TCSETSW2 = IOW((byte)'T', 0x2c, Marshal.SizeOf(typeof(termios)));
        public static readonly UIntPtr TCSETSF2 = IOW((byte)'T', 0x2d, Marshal.SizeOf(typeof(termios)));

        // See /usr/include/asm-generic/termbits.h
        public const int VTIME = 5;
        public const int VMIN = 6;

        public const uint IGNBRK = 0x0001;
        public const uint BRKINT = 0x0002;
        public const uint PARMRK = 0x0008;
        public const uint ISTRIP = 0x0020;
        public const uint INLCR = 0x0040;
        public const uint IGNCR = 0x0080;
        public const uint ICRNL = 0x0100;
        public const uint IXON = 0x0400;

        public const uint OPOST = 0x0001;

        public const uint CBAUD = 0x100f;
        public const uint BOTHER = 0x1000;

        public const uint CSIZE = 0x0030;
        public const uint CS7 = 0x0020;
        public const uint CS8 = 0x0030;
        public const uint CSTOPB = 0x0040;
        public const uint CREAD = 0x0080;
        public const uint PARENB = 0x0100;
        public const uint PARODD = 0x0200;
        public const uint CLOCAL = 0x0800;
        public const uint CRTSCTS = 0x80000000u;

        public const uint ECHO = 0x0008;
        public const uint ECHONL = 0x0040;
        public const uint ICANON = 0x0002;
        public const uint ISIG = 0x0001;
        public const uint IEXTEN = 0x8000;

        public const int TCIFLUSH = 0;

        public const int TCSANOW = 0;

        public unsafe struct termios // termios2
        {
            public uint c_iflag;
            public uint c_oflag;
            public uint c_cflag;
            public uint c_lflag;
            public byte c_line;
            public fixed byte c_cc[19];
            public uint c_ispeed;
            public uint c_ospeed;
        }

        public static void cfmakeraw(ref termios termios)
        {
            // See https://linux.die.net/man/3/cfmakeraw "Raw mode" heading.
            termios.c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL | IXON);
            termios.c_oflag &= ~OPOST;
            termios.c_lflag &= ~(ECHO | ECHONL | ICANON | ISIG | IEXTEN);
            termios.c_cflag &= ~(CSIZE | PARENB);
            termios.c_cflag |= CS8;
        }

        public static int cfsetspeed(ref termios termios, uint speed)
        {
            termios.c_cflag &= ~CBAUD;
            termios.c_cflag |= BOTHER;
            termios.c_ispeed = speed;
            termios.c_ospeed = speed;
            return 0;
        }

        public static int tcgetattr(int filedes, out termios termios)
        {
            termios = new termios();
            return ioctl(filedes, TCGETS2, ref termios);
        }

        public static int tcsetattr(int filedes, int actions, ref termios termios)
        {
            Debug.Assert(actions == TCSANOW);
            return ioctl(filedes, TCSETS2, ref termios);
        }

        [DllImport(libc, SetLastError = true)]
        public static extern int tcdrain(int filedes);

        [DllImport(libc, SetLastError = true)]
        public static extern int tcflush(int filedes, int action);
        #endregion
    }
}
