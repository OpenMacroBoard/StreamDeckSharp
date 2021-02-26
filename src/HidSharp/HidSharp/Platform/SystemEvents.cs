#region License
/* Copyright 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

// Last updated 2017/12/9.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HidSharp.Platform.SystemEvents
{
    #region Native
    #region POSIX (Shared)
    abstract class PosixNativeMethods
    {
        public static readonly IntPtr IntPtrNegativeOne = (IntPtr.Size == 8) ? new IntPtr((long)(-1)) : new IntPtr((int)(-1));

        public abstract int GetTickCount();

        public abstract int shm_open(string filename, int oflag, int mode);
        public abstract int chmod(string filename, int mode);
        public abstract int fchmod(int filedes, int mode);
        public abstract int ftruncate(int filedes, long length);
        public abstract int close(int filedes);

        public abstract IntPtr mmap(IntPtr addr, UIntPtr size, int prot, int flags, int fd, long offset);
        public abstract int munmap(IntPtr addr, UIntPtr length);

        public int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }

        public int retry(Func<int> sysfunc)
        {
            while (true)
            {
                var ret = sysfunc();
                if (ret != -1 || GetLastError() != EINTR) { return ret; }
            }
        }

        public IntPtr retry(Func<IntPtr> sysfunc)
        {
            while (true)
            {
                var ret = sysfunc();
                if (ret != IntPtrNegativeOne || GetLastError() != EINTR) { return ret; }
            }
        }

        public abstract int MAP_SHARED { get; }
        public abstract int O_RDWR { get; }
        public abstract int O_CREAT { get; }
        public abstract int PROT_READ { get; }
        public abstract int PROT_WRITE { get; }
        public abstract int EINTR { get; }
    }
    #endregion

    #region Linux
    unsafe class LinuxNativeMethods : PosixNativeMethods
    {
        public override int GetTickCount()
        {
            timespec timespec;
            if (clock_gettime(CLOCK_MONOTONIC, out timespec) < 0) { throw new InvalidOperationException(); }
            return (int)(uint)(ulong)((long)timespec.seconds * 1000 + (long)timespec.nanoseconds / 1000000);
        }

        #region Platform Invoke
        const string libc = "libc";
        const string librt = "librt.so.1";

        public const int CLOCK_MONOTONIC = 1;
        public const int IN_ACCESS = 0x0001;
        public const int IN_MODIFY = 0x0002;
        public const int IN_ATTRIB = 0x0004;
        public override int MAP_SHARED { get { return 1; } }
        public const int NAME_MAX = 255;
        public override int PROT_READ { get { return 1; } }
        public override int PROT_WRITE { get { return 2; } }
        public override int O_RDWR { get { return 0x002; } }
        public override int O_CREAT { get { return 0x040; } }
        public override int EINTR { get { return 4; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct inotify_event
        {
            public int wd;
            public uint mask;
            public uint cookie;
            public uint len;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct timespec
        {
            public IntPtr seconds;
            public IntPtr nanoseconds;
        }

        [DllImport(libc, SetLastError = true)]
        static extern int clock_gettime(int clockid, out timespec timespec);

        public override int shm_open(string filename, int oflag, int mode)
        {
            return native_shm_open(filename, oflag, mode);
        }

        [DllImport(librt, EntryPoint = "shm_open", SetLastError = true)]
        static extern int native_shm_open([MarshalAs(UnmanagedType.LPStr)] string filename, int oflag, int mode);

        public override int chmod(string filename, int mode)
        {
            return native_chmod(filename, mode);
        }

        [DllImport(libc, EntryPoint = "chmod", SetLastError = true)]
        static extern int native_chmod([MarshalAs(UnmanagedType.LPStr)] string filename, int mode);

        public override int fchmod(int filedes, int mode)
        {
            return native_fchmod(filedes, mode);
        }

        [DllImport(libc, EntryPoint = "fchmod", SetLastError = true)]
        static extern int native_fchmod(int filedes, int mode);

        public override int ftruncate(int filedes, long length)
        {
            return native_ftruncate64(filedes, length);
        }

        [DllImport(libc, EntryPoint = "ftruncate64", SetLastError = true)]
        static extern int native_ftruncate64(int filedes, long length); // < 0 if failed

        [DllImport(libc, SetLastError = true)]
        public static extern IntPtr read(int filedes, IntPtr buffer, UIntPtr size);

        public override int close(int filedes)
        {
            return native_close(filedes);
        }

        [DllImport(libc, EntryPoint = "close", SetLastError = true)]
        static extern int native_close(int filedes);

        public override IntPtr mmap(IntPtr addr, UIntPtr size, int prot, int flags, int fd, long offset)
        {
            return native_mmap64(addr, size, prot, flags, fd, offset);
        }

        [DllImport(libc, EntryPoint = "mmap64", SetLastError = true)]
        static extern IntPtr native_mmap64(IntPtr addr, UIntPtr size, int prot, int flags, int fd, long offset);

        public override int munmap(IntPtr addr, UIntPtr length)
        {
            return native_munmap(addr, length);
        }

        [DllImport(libc, EntryPoint = "munmap", SetLastError = true)]
        static extern int native_munmap(IntPtr addr, UIntPtr length);

        [DllImport(libc, SetLastError = true)]
        public static extern int utime([MarshalAs(UnmanagedType.LPStr)] string file, IntPtr times);

        [DllImport(libc, SetLastError = true)]
        public static extern int inotify_init();

        [DllImport(libc, SetLastError = true)]
        public static extern int inotify_add_watch(int fd, [MarshalAs(UnmanagedType.LPStr)] string pathname, int mask);

        [DllImport(libc, SetLastError = true)]
        public static extern int inotify_rm_watch(int fd, int wd);
        #endregion
    }
    #endregion

    #region MacOS
    unsafe class MacOSNativeMethods : PosixNativeMethods
    {
        static uint _machTaskSelf;
        static double _scale;

        static MacOSNativeMethods()
        {
            IntPtr syslib = dlopen("/usr/lib/libSystem.dylib", RTLD_LAZY); if (syslib == IntPtr.Zero) { throw new InvalidOperationException("Unable to load libSystem."); }
            IntPtr taskSelf = dlsym(syslib, "mach_task_self_"); if (taskSelf == IntPtr.Zero) { throw new InvalidOperationException("Unable to load mach_task_self."); }
            _machTaskSelf = (uint)Marshal.ReadInt32(taskSelf);
            dlclose(syslib);

            mach_timebase_info_data_t info;
            mach_timebase_info(out info);
            _scale = (double)info.numer / (double)info.denom / 1e6;
        }

        public static uint GetMachTaskSelf()
        {
            return _machTaskSelf;
        }

        public override int GetTickCount()
        {
            int tickCount = (int)(uint)(ulong)Math.Round((ulong)mach_absolute_time() * _scale);
            return tickCount;
        }

        #region Platform Invoke
        const string libc = "libc";
        const string libdl = "libdl";

        public const int KERN_SUCCESS = 0;
        public const int RTLD_LAZY = 1; // dlopen
        public const int MACH_PORT_RIGHT_RECEIVE = 1; // mach_port_allocate
        public const int MACH_RCV_MSG = 2;
        public const int MACH_RCV_TOO_LARGE = 268451844;
        public override int MAP_SHARED { get { return 1; } } // mmap
        public override int PROT_READ { get { return 1; } }
        public override int PROT_WRITE { get { return 2; } }
        public const int NOTIFY_REUSE = 1; // notify_register_file_descriptor
        public const int NOTIFY_STATUS_OK = 0;
        public override int O_RDWR { get { return 0x002; } } // shm_open
        public override int O_CREAT { get { return 0x200; } }
        public const short POLLIN = 1; // poll
        public const short POLLERR = 8;
        public override int EINTR { get { return 4; } }
        public const int EBADF = 9;
        public const int ENODEV = 19;
        public const int EINVAL = 22;
        public const int ESPIPE = 29;

        [StructLayout(LayoutKind.Sequential)]
        public struct mach_msg_header_t
        {
            public uint msgh_bits;
            public uint msgh_size;
            public uint msgh_remote_port;
            public uint msgh_local_port;
            public uint msgh_voucher_port;
            public int msgh_id;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mach_msg_trailer_t
        {
            public uint type;
            public uint size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct mach_msg_t
        {
            public mach_msg_header_t header;
            public mach_msg_trailer_t trailer;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct mach_timebase_info_data_t
        {
            public uint numer;
            public uint denom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct pollfd
        {
            public int fd;
            public short events;
            public short revents;
        }

        [DllImport(libdl)]
        static extern IntPtr dlopen([MarshalAs(UnmanagedType.LPStr)] string path, int mode);

        [DllImport(libdl)]
        static extern IntPtr dlsym(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);

        [DllImport(libdl)]
        static extern int dlclose(IntPtr handle);

        [DllImport(libc)]
        static extern long mach_absolute_time();

        [DllImport(libc)]
        static extern void mach_timebase_info(out mach_timebase_info_data_t info);

        [DllImport(libc)]
        public static extern int mach_msg_overwrite(IntPtr msg, int option, uint send_size, uint recv_size, uint recv_name, uint timeout, uint notify, IntPtr recv_msg, uint recv_limit);

        [DllImport(libc)]
        public static extern int mach_port_allocate(uint task, uint right, out uint name);

        [DllImport(libc)]
        public static extern uint notify_post([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(libc)]
        public static extern uint notify_register_file_descriptor([MarshalAs(UnmanagedType.LPStr)] string name, ref int fd, int flags, out int token);

        [DllImport(libc)]
        public static extern uint notify_register_mach_port([MarshalAs(UnmanagedType.LPStr)] string name, ref uint port, int flags, out int token);

        [DllImport(libc)]
        public static extern uint notify_cancel(int token);

        [DllImport(libc)]
        public static extern int poll(ref pollfd fd, uint nfds, int timeout = -1); // < 0 if failed

        [DllImport(libc)]
        public static extern IntPtr read(int filedes, byte[] buffer, UIntPtr nbyte);

        public override int shm_open(string filename, int oflag, int mode)
        {
            return native_shm_open(filename, oflag, mode);
        }

        [DllImport(libc, EntryPoint = "shm_open", SetLastError = true)]
        static extern int native_shm_open([MarshalAs(UnmanagedType.LPStr)] string filename, int oflag, int mode); // < 0 if failed

        public override int chmod(string filename, int mode)
        {
            return native_chmod(filename, mode);
        }

        [DllImport(libc, EntryPoint = "chmod", SetLastError = true)]
        static extern int native_chmod([MarshalAs(UnmanagedType.LPStr)] string filename, int mode);

        public override int fchmod(int filedes, int mode)
        {
            return native_fchmod(filedes, mode);
        }

        [DllImport(libc, EntryPoint = "fchmod", SetLastError = true)]
        static extern int native_fchmod(int filedes, int mode);

        public override int ftruncate(int filedes, long length)
        {
            return native_ftruncate(filedes, length);
        }

        [DllImport(libc, EntryPoint = "ftruncate", SetLastError = true)]
        static extern int native_ftruncate(int filedes, long length); // < 0 if failed

        public override int close(int filedes)
        {
            return native_close(filedes);
        }

        [DllImport(libc, EntryPoint = "close", SetLastError = true)]
        static extern int native_close(int filedes); // < 0 if failed

        public override IntPtr mmap(IntPtr addr, UIntPtr size, int prot, int flags, int fd, long offset)
        {
            return native_mmap(addr, size, prot, flags, fd, offset);
        }

        [DllImport(libc, EntryPoint = "mmap", SetLastError = true)]
        static extern IntPtr native_mmap(IntPtr addr, UIntPtr size, int prot, int flags, int fd, long offset);

        public override int munmap(IntPtr addr, UIntPtr length)
        {
            return native_munmap(addr, length);
        }

        [DllImport(libc, EntryPoint = "munmap", SetLastError = true)]
        static extern int native_munmap(IntPtr addr, UIntPtr length);
        #endregion
    }
    #endregion
    #endregion

    #region System
    internal abstract class SystemEvent : IDisposable
    {
        protected SystemEvent(string name)
        {
            if (name == null) { throw new ArgumentNullException(); }
            Name = name;
        }

        public abstract void Dispose();
        public abstract void Reset();
        public abstract void Set();

        public bool Wait(int timeout)
        {
            try
            {
                return WaitHandle.WaitOne(timeout);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        public abstract bool CreatedNew { get; }
        public string Name { get; private set; }
        public abstract WaitHandle WaitHandle { get; }
    }

    internal abstract class SystemMutex : IDisposable
    {
        static HashSet<string> _antirecursionList = new HashSet<string>();
        Thread _lockThread; // Mostly for debugging. Mutexes must be released by the threads that locked them.

        protected SystemMutex(string name)
        {
            if (name == null) { throw new ArgumentNullException(); }
            Name = name;
        }

        public abstract void Dispose();
        protected abstract bool WaitOne(int timeout);
        protected abstract void ReleaseMutex();

        sealed class ResourceLock : IDisposable
        {
            int _disposed;

            internal SystemMutex M;

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) { return; }

                try
                {
                    M.ReleaseMutexOuter();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        public bool TryLock(out IDisposable @lock)
        {
            return TryLock(Timeout.Infinite, out @lock);
        }

        public bool TryLock(int timeout, out IDisposable @lock)
        {
            @lock = null;

            try
            {
                if (!WaitOneOuter(timeout)) { return false; }
            }
            catch (AbandonedMutexException e)
            {
                Debug.WriteLine(e);
                return false;
            }

            @lock = new ResourceLock() { M = this };
            return true;
        }

        bool WaitOneOuter(int timeout)
        {
            if (!WaitOneInner(timeout)) { return false; }

            lock (_antirecursionList)
            {
                if (_antirecursionList.Contains(Name))
                {
                    ReleaseMutexInner(); return false;
                }

                _antirecursionList.Add(Name); return true;
            }
        }

        bool WaitOneInner(int timeout)
        {
            try
            {
                if (!WaitOne(timeout))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e); return false;
            }

            if (_lockThread != null) { throw new InvalidOperationException(); }
            _lockThread = Thread.CurrentThread; return true;
        }

        void ReleaseMutexOuter()
        {
            lock (_antirecursionList)
            {
                _antirecursionList.Remove(Name);
                ReleaseMutexInner();
            }
        }

        void ReleaseMutexInner()
        {
            if (_lockThread != Thread.CurrentThread) { throw new InvalidOperationException(); }
            ReleaseMutex(); _lockThread = null;
        }

        public abstract bool CreatedNew { get; }
        public string Name { get; private set; }
    }

    internal abstract class EventManager
    {
        internal abstract void Start();

        public abstract SystemEvent CreateEvent(string name);
        public abstract SystemMutex CreateMutex(string name);

        public bool MutexMayExist(string name) // Call it "MayExist" because it *might* not -- another could call this at the same time.
        {
            using (var mutex = CreateMutex(name)) { return !mutex.CreatedNew; }
        }
    }
    #endregion

    #region POSIX Implementation (Shared)
    internal abstract class PosixEventManager : EventManager
    {
        const string EventsKind = "Events";
        const string MutexesKind = "Mutexes";
        const int DontRefreshInterval = 4000;
        const int RefreshInterval = 5000;
        const int TimeoutInterval = 30000;
        const int TimeTravelInterval = 1000;

        sealed class PosixEvent : SystemEvent
        {
            struct LockStructure
            {
                public int Ttl;
                public int RefCount;
                public int SetID;
                public int ResetID;
            }

            bool _createdNew;
            ManualResetEvent _event;
            PosixEventManager _manager;
            object _refreshHandle;

            public PosixEvent(PosixEventManager manager, string name)
                : base(name)
            {
                if (manager == null) { throw new ArgumentNullException(); }
                _manager = manager;
                _event = new ManualResetEvent(false);

                UpdateEventStruct(inL =>
                {
                    if (inL.RefCount == 0) { _createdNew = true; }

                    var outL = inL;
                    outL.RefCount++;
                    outL.Ttl = _manager.GetTickCount();
                    return outL;
                });

                manager.RegisterRefreshCallback(Refresh, out _refreshHandle, GetEventFilename(EventsKind, Name), GetSHMFilename(EventsKind, Name));
            }

            public override void Dispose()
            {
                if (_event != null)
                {
                    _manager.UnregisterRefreshCallback(ref _refreshHandle);

                    UpdateEventStruct(inL =>
                    {
                        var outL = inL;
                        outL.RefCount--;
                        return outL;
                    });

                    _event.Close();
                    _event = null;
                }
            }

            void Refresh()
            {
                UpdateEventStruct(inL =>
                {
                    int ttl = _manager.GetTickCount();
                    if ((uint)(ttl - inL.Ttl) < DontRefreshInterval) { return null; }

                    var outL = inL;
                    outL.Ttl = ttl;
                    return outL;
                });
            }

            public override void Reset()
            {
                UpdateEventStruct(inL =>
                {
                    if (inL.ResetID == inL.SetID) { return null; }

                    var outL = inL;
                    outL.ResetID = outL.SetID;
                    outL.Ttl = _manager.GetTickCount();
                    return outL;
                }, true);
            }

            public override void Set()
            {
                UpdateEventStruct(inL =>
                {
                    if (inL.ResetID != inL.SetID) { return null; }

                    var outL = inL;
                    outL.SetID++;
                    outL.Ttl = _manager.GetTickCount();
                    return outL;
                }, true);
            }

            void UpdateEventStream(Func<byte[], bool> editCallback)
            {
                _manager.UpdateEventStream(EventsKind, Name, 16, editCallback);
            }

            void UpdateEventStruct(Func<LockStructure, LockStructure?> editCallback, bool updateFSW = false)
            {
                UpdateEventStream(buffer =>
                {
                    int ttl = _manager.GetTickCount();

                    var inL = new LockStructure();
                    inL.Ttl = BitConverter.ToInt32(buffer, 0);
                    inL.RefCount = BitConverter.ToInt32(buffer, 4);
                    inL.SetID = BitConverter.ToInt32(buffer, 8);
                    inL.ResetID = BitConverter.ToInt32(buffer, 12);

                    if (inL.RefCount <= 0)
                    {
                        inL = new LockStructure();
                    }

                    int refreshTime = ttl - inL.Ttl;
                    if (inL.Ttl != 0)
                    {
                        if (refreshTime >= TimeoutInterval || refreshTime <= -TimeTravelInterval)
                        {
                            Debug.WriteLine(string.Format("{0} : Event Timed Out", Name));
                            inL = new LockStructure();
                        }
                    }

                    var outLmaybe = editCallback(inL);
                    if (outLmaybe == null) { UpdateEvent(inL.SetID != inL.ResetID); return false; }
                    var outL = outLmaybe.Value;

                    Array.Copy(BitConverter.GetBytes(outL.Ttl), 0, buffer, 0, 4);
                    Array.Copy(BitConverter.GetBytes(outL.RefCount), 0, buffer, 4, 4);
                    Array.Copy(BitConverter.GetBytes(outL.SetID), 0, buffer, 8, 4);
                    Array.Copy(BitConverter.GetBytes(outL.ResetID), 0, buffer, 12, 4);
                    UpdateEvent(outL.SetID != inL.ResetID); return updateFSW;
                });
            }

            void UpdateEvent(bool set)
            {
                if (set) { _event.Set(); } else { _event.Reset(); }
            }

            public override bool CreatedNew
            {
                get { return _createdNew; }
            }

            public override WaitHandle WaitHandle
            {
                get { return _event; }
            }
        }

        sealed class PosixMutex : SystemMutex
        {
            struct LockStructure
            {
                public int Ttl;
                public int RefCount;
                //public int Reserved;
                public int LockTtl;
                public Guid LockGuid;
            }

            bool _createdNew;
            Guid _guid;
            PosixEventManager _manager;
            object _refreshHandle;

            public PosixMutex(PosixEventManager manager, string name)
                : base(name)
            {
                if (manager == null) { throw new ArgumentNullException(); }
                _guid = Guid.NewGuid(); // Will not equal Guid.Empty.
                _manager = manager;

                UpdateEventStruct(inL =>
                {
                    if (inL.RefCount == 0) { _createdNew = true; }

                    var outL = inL;
                    outL.RefCount++;
                    return outL;
                });

                _manager.RegisterRefreshCallback(Refresh, out _refreshHandle, null, null);
            }

            public override void Dispose()
            {
                _manager.UnregisterRefreshCallback(ref _refreshHandle);

                UpdateEventStruct(inL =>
                {
                    var outL = inL;
                    if (outL.LockGuid == _guid)
                    {
                        outL.LockGuid = Guid.Empty;
                    }
                    outL.RefCount--;
                    return outL;
                });
            }

            void Refresh()
            {
                UpdateEventStruct(inL =>
                {
                    int ttl = _manager.GetTickCount();

                    var outL = inL;
                    return outL;
                });
            }

            protected override bool WaitOne(int timeout)
            {
                int start = _manager.GetTickCount();

                do
                {
                    bool locked = false;

                    UpdateEventStruct(inL =>
                    {
                        if (inL.LockGuid != Guid.Empty)
                        {
                            if (inL.LockGuid == _guid) { throw new InvalidOperationException("Already locked by this mutex."); }
                            return null;
                        }

                        var outL = inL;
                        outL.LockGuid = _guid; locked = true;
                        return outL;
                    });

                    if (locked)
                    {
                        return true;
                    }
                    Thread.Sleep(50);
                }
                while ((uint)(_manager.GetTickCount() - start) <= (uint)timeout); // This covers Timeout.Infinite as well.

                return false;
            }

            protected override void ReleaseMutex()
            {
                UpdateEventStruct(inL =>
                {
                    if (inL.LockGuid == Guid.Empty) { throw new InvalidOperationException("Not locked by anyone."); }
                    if (inL.LockGuid != _guid) { throw new InvalidOperationException("Not locked by this mutex."); }

                    var outL = inL;
                    outL.LockGuid = Guid.Empty;
                    return outL;
                });
            }

            void UpdateEventStream(Func<byte[], bool> editCallback)
            {
                _manager.UpdateEventStream(MutexesKind, Name, 32, editCallback);
            }

            void UpdateEventStruct(Func<LockStructure, LockStructure?> editCallback)
            {
                UpdateEventStream(buffer =>
                {
                    int ttl = _manager.GetTickCount();

                    var inL = new LockStructure();
                    inL.Ttl = BitConverter.ToInt32(buffer, 0);
                    inL.RefCount = BitConverter.ToInt32(buffer, 4);
                    inL.LockTtl = BitConverter.ToInt32(buffer, 12);
                    var lockGuid = new byte[16]; Array.Copy(buffer, 16, lockGuid, 0, lockGuid.Length);
                    inL.LockGuid = new Guid(lockGuid);

                    if (inL.RefCount <= 0)
                    {
                        inL = new LockStructure();
                    }

                    int refreshTime = ttl - inL.Ttl;
                    if (inL.Ttl != 0)
                    {
                        if (refreshTime >= TimeoutInterval || refreshTime <= -TimeTravelInterval)
                        {
                            Debug.WriteLine(string.Format("{0} : Mutex Timed Out", Name));
                            inL = new LockStructure();
                        }
                    }

                    int lockTime = ttl - inL.LockTtl;
                    if (inL.LockGuid != Guid.Empty || inL.LockTtl != 0)
                    {
                        if (lockTime >= TimeoutInterval || lockTime < -TimeTravelInterval)
                        {
                            Debug.WriteLine(string.Format("{0} : Mutex Lock Timed Out", Name));
                            inL.LockGuid = Guid.Empty; inL.LockTtl = 0;
                        }
                    }

                    var outLmaybe = editCallback(inL);
                    if (outLmaybe == null) { return false; }
                    var outL = outLmaybe.Value;

                    outL.Ttl = ttl;
                    if (outL.LockGuid == _guid) { outL.LockTtl = ttl; }

                    Array.Copy(BitConverter.GetBytes(outL.Ttl), 0, buffer, 0, 4);
                    Array.Copy(BitConverter.GetBytes(outL.RefCount), 0, buffer, 4, 4);
                    Array.Copy(BitConverter.GetBytes(outL.LockTtl), 0, buffer, 12, 4);
                    Array.Copy(outL.LockGuid.ToByteArray(), 0, buffer, 16, 16);
                    return false; // Mutexes don't need to update a FileSystemWatcher.
                });
            }

            public override bool CreatedNew
            {
                get { return _createdNew; }
            }
        }

        Dictionary<object, Action> _jobs;
        ManualResetEvent _jobThreadReady;
        Thread _jobThread;

        public PosixEventManager()
        {
            SyncRoot = new object();
            NativeMethods = CreateNativeMethods();
        }

        internal override void Start()
        {
            _jobs = new Dictionary<object, Action>();
            _jobThreadReady = new ManualResetEvent(false);
            _jobThread = new Thread(RunJobThread) { IsBackground = true, Name = "HID System Events Job Manager" };
            _jobThread.Start();
            _jobThreadReady.WaitOne();
        }

        public override SystemEvent CreateEvent(string name)
        {
            return new PosixEvent(this, name);
        }

        public override SystemMutex CreateMutex(string name)
        {
            return new PosixMutex(this, name);
        }

        protected abstract PosixNativeMethods CreateNativeMethods();

        internal unsafe void UpdateEventStream(string kind, string name, int length, Func<byte[], bool> editCallback)
        {
            // 2017/06/27: SO DUMB. Find a better way. There has GOT to be a reliable-ish one. SO DUMB. :P
            // 2017/06/28: On the plus side, this does work reliably. So maybe it's good enough for now.
            foreach (string directory in GetEventDirectoryParts(kind))
            {
                try { Directory.CreateDirectory(directory); }
                catch { }

                try { NativeMethods.retry(() => NativeMethods.chmod(directory, 7 << 6 | 7 << 3 | 7 << 0)); }
                catch { }
            }

            string eventName = GetEventFilename(kind, name);
            using (var stream = File.Open(eventName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                int streamHandle = (int)stream.SafeFileHandle.DangerousGetHandle();
                try { NativeMethods.retry(() => NativeMethods.fchmod(streamHandle, 6 << 6 | 6 << 3 | 6)); }
                catch { }

                while (true)
                {
                    try { stream.Lock(0, 0); }
                    catch (IOException) { Thread.Sleep(50); continue; }
                    break;
                }

                var shmName = GetSHMFilename(kind, name);
                int shm;
                try { shm = NativeMethods.retry(() => NativeMethods.shm_open(shmName, NativeMethods.O_CREAT | NativeMethods.O_RDWR, 6 << 6 | 6 << 3 | 6)); } catch (DllNotFoundException) { shm = -1; }
                if (shm < 0) { throw new InvalidOperationException(string.Format("Failed to open shared memory {0}: {1}", shmName, NativeMethods.GetLastError())); }

                NativeMethods.retry(() => NativeMethods.fchmod(shm, 6 << 6 | 6 << 3 | 6));

                try
                {
                    NativeMethods.retry(() => NativeMethods.ftruncate(shm, length)); // Apparently, on shared memory, this only works the first time on MacOS. Good times.

                    IntPtr ptr = NativeMethods.mmap(IntPtr.Zero, (UIntPtr)length, NativeMethods.PROT_READ | NativeMethods.PROT_WRITE, NativeMethods.MAP_SHARED, shm, 0);
                    if (ptr == PosixNativeMethods.IntPtrNegativeOne)
                    {
                        throw new InvalidOperationException("Failed to map memory: " + NativeMethods.GetLastError().ToString());
                    }

                    try
                    {
                        byte[] buffer = new byte[length];
                        Marshal.Copy(ptr, buffer, 0, length);
                        bool update = editCallback(buffer);
                        Marshal.Copy(buffer, 0, ptr, length);

                        if (update)
                        {
                            RunNotify(stream, eventName, shmName);
                        }
                    }
                    finally
                    {
                        NativeMethods.munmap(ptr, (UIntPtr)length);
                    }
                }
                finally
                {
                    NativeMethods.retry(() => NativeMethods.close(shm));
                }
            }
        }

        protected abstract object CreateJobObject();

        protected abstract void RegisterJobObjectNotify(object jobObject, string eventName, string shmName);

        protected abstract void UnregisterJobObjectNotify(object jobObject);

        protected void RunJobObject(object jobObject)
        {
            Action job;
            if (_jobs.TryGetValue(jobObject, out job))
            {
                job();
            }
        }

        protected abstract void RunNotify(FileStream eventStream, string eventName, string shmName);

        void RegisterRefreshCallback(Action callback, out object jobObject, string eventName, string shmName)
        {
            jobObject = CreateJobObject();

            lock (SyncRoot)
            {
                _jobs.Add(jobObject, callback);
                RegisterJobObjectNotify(jobObject, eventName, shmName);
                Monitor.Pulse(SyncRoot);
            }
        }

        void UnregisterRefreshCallback(ref object jobObject)
        {
            lock (SyncRoot)
            {
                if (jobObject == null) { return; }
                UnregisterJobObjectNotify(jobObject);
                _jobs.Remove(jobObject); Monitor.Pulse(SyncRoot);
                jobObject = null;
            }
        }

        void RunJobThread()
        {
            _jobThreadReady.Set();

            lock (SyncRoot)
            {
                while (true)
                {
                    if (_jobs.Count == 0)
                    {
                        Monitor.Wait(SyncRoot);
                    }
                    else
                    {
                        Monitor.Exit(SyncRoot);
                        try { Thread.Sleep(5000); }
                        finally { Monitor.Enter(SyncRoot); }
                    }

                    foreach (var job in _jobs.Values.ToArray())
                    {
                        job();
                    }
                }
            }
        }

        static string GetEventDirectory(string kind)
        {
            return Path.Combine(Path.Combine(Path.Combine(Path.GetTempPath(), "HIDSharp"), "SystemEvents"), kind);
        }

        static string[] GetEventDirectoryParts(string kind)
        {
            return new[]
            {
                Path.Combine(Path.GetTempPath(), "HIDSharp"),
                Path.Combine(Path.Combine(Path.GetTempPath(), "HIDSharp"), "SystemEvents"),
                Path.Combine(Path.Combine(Path.Combine(Path.GetTempPath(), "HIDSharp"), "SystemEvents"), kind)
            };
        }

        static string GetEventFilename(string kind, string name)
        {
            string bs64 = Convert.ToBase64String(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(name))).Replace('+', '-').Replace('/', '_').Replace("=", "");
            string directory = GetEventDirectory(kind);
            string filename = Path.Combine(directory, bs64 + ".tmp");
            return filename;
        }

        static string GetSHMFilename(string kind, string name)
        {
            string shmName = string.Format("/HS.{0}.{1}", kind, Convert.ToBase64String(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(name))).Substring(0, 16).Replace('+', '-').Replace('/', '_'));
            return shmName;
        }

        int GetTickCount()
        {
            int t = NativeMethods.GetTickCount(); // Mono Environment.TickCount is not entirely consistent on MacOS. I think it differs between processes by their launch time.
            if (t == 0) { t = 1; }
            return t;
        }

        protected PosixNativeMethods NativeMethods
        {
            get;
            private set;
        }

        protected object SyncRoot
        {
            get;
            private set;
        }
    }
    #endregion

    #region Linux Implementation
    internal sealed class LinuxEventManager : PosixEventManager
    {
        sealed class JobHandle
        {
            internal int? WatchDescriptor;
        }

        int _notifyFD;
        Dictionary<int, JobHandle> _watchDescriptors;
        Thread _notifyThread;

        internal override void Start()
        {
            _watchDescriptors = new Dictionary<int, JobHandle>();
            base.Start();
        }

        protected override object CreateJobObject()
        {
            return new JobHandle();
        }

        protected override void RegisterJobObjectNotify(object jobObject, string eventName, string shmName)
        {
            var jobHandle = (JobHandle)jobObject;
            if (eventName != null)
            {
                bool hasThread = (_notifyThread != null);

                if (!hasThread)
                {
                    _notifyFD = LinuxNativeMethods.inotify_init();
                    if (_notifyFD < 0)
                    {
                        throw new InvalidOperationException("Failed to create inotify file descriptor!");
                    }
                }

                int wd = LinuxNativeMethods.inotify_add_watch(_notifyFD, eventName, LinuxNativeMethods.IN_ATTRIB);
                if (wd < 0)
                {
                    throw new InvalidOperationException("Failed to add inotify watch descriptor: " + NativeMethods.GetLastError().ToString());
                }

                _watchDescriptors.Add(wd, jobHandle); jobHandle.WatchDescriptor = wd;
                if (!hasThread)
                {
                    _notifyThread = new Thread(RunNotifyThread) { IsBackground = true, Name = "HID System Events Notification Monitor" };
                    _notifyThread.Start();
                }
            }
        }

        protected override void UnregisterJobObjectNotify(object jobObject)
        {
            var jobHandle = (JobHandle)jobObject;
            if (jobHandle.WatchDescriptor != null)
            {
                int wd = (int)jobHandle.WatchDescriptor;
                if (LinuxNativeMethods.inotify_rm_watch(_notifyFD, wd) < 0)
                {
                    throw new InvalidOperationException("Failed to remove inotify watch descriptor.");
                }

                jobHandle.WatchDescriptor = null;
                _watchDescriptors.Remove(wd);
            }
        }

        protected override void RunNotify(FileStream eventStream, string eventName, string shmName)
        {
            int ret = NativeMethods.retry(() => LinuxNativeMethods.utime(eventName, IntPtr.Zero));
            if (ret < 0) { Debug.WriteLine("inotify touch failed: " + NativeMethods.GetLastError().ToString()); }
        }

        unsafe void RunNotifyThread()
        {
            int fd = _notifyFD;
            int inotifySize = Marshal.SizeOf(typeof(LinuxNativeMethods.inotify_event));
            int bufferSize = inotifySize + LinuxNativeMethods.NAME_MAX + 1;
            var buffer = stackalloc byte[bufferSize];

            while (true)
            {
                int bytes = (int)(long)LinuxNativeMethods.read(fd, (IntPtr)buffer, (UIntPtr)bufferSize);
                if (bytes < 1) { Debug.WriteLine("inotify read error. Abandoning watch thread."); return; }

                int offset = 0;
                while (offset <= bytes - inotifySize)
                {
                    var ev = (LinuxNativeMethods.inotify_event)Marshal.PtrToStructure((IntPtr)(&buffer[offset]), typeof(LinuxNativeMethods.inotify_event));
                    offset += inotifySize + checked((int)ev.len);

                    if (0 != (ev.mask & LinuxNativeMethods.IN_ATTRIB))
                    {
                        lock (SyncRoot)
                        {
                            JobHandle jobHandle;
                            if (_watchDescriptors.TryGetValue(ev.wd, out jobHandle))
                            {
                                RunJobObject(jobHandle);
                            }
                        }
                    }
                }
            }
        }

        protected override PosixNativeMethods CreateNativeMethods()
        {
            return new LinuxNativeMethods();
        }
    }
    #endregion

    #region MacOS Implementation
    internal sealed class MacOSEventManager : PosixEventManager
    {
        sealed class JobHandle
        {
            internal int? NotifyToken;
        }

        uint _notifyFD;
        Dictionary<int, JobHandle> _notifyTokens;
        Thread _notifyThread;

        internal override void Start()
        {
            _notifyTokens = new Dictionary<int, JobHandle>();
            base.Start();
        }

        protected override PosixNativeMethods CreateNativeMethods()
        {
            return new MacOSNativeMethods();
        }

        protected override object CreateJobObject()
        {
            return new JobHandle();
        }

        protected override void RegisterJobObjectNotify(object jobObject, string eventName, string shmName)
        {
            var jobHandle = (JobHandle)jobObject;
            if (shmName != null)
            {
                bool hasThread = (_notifyThread != null);
                int token;

                if (!hasThread)
                {
                    if (MacOSNativeMethods.KERN_SUCCESS !=
                        MacOSNativeMethods.mach_port_allocate(MacOSNativeMethods.GetMachTaskSelf(), (uint)MacOSNativeMethods.MACH_PORT_RIGHT_RECEIVE, out _notifyFD))
                    {
                        throw new InvalidOperationException("Failed to create Mach port!");
                    }
                }

                //var ret = MacOSNativeMethods.notify_register_file_descriptor(notify, ref _notifyFD, hasThread ? MacOSNativeMethods.NOTIFY_REUSE : 0, out token);
                var ret = MacOSNativeMethods.notify_register_mach_port(shmName, ref _notifyFD, MacOSNativeMethods.NOTIFY_REUSE, out token);
                if (ret != MacOSNativeMethods.NOTIFY_STATUS_OK)
                {
                    throw new InvalidOperationException("Failed to register notify file descriptor! " + ret.ToString());
                }

                _notifyTokens.Add(token, jobHandle); jobHandle.NotifyToken = token;
                if (!hasThread)
                {
                    _notifyThread = new Thread(RunNotifyThread) { IsBackground = true, Name = "HID System Events Notification Monitor" };
                    _notifyThread.Start();
                }
            }
        }

        protected override void UnregisterJobObjectNotify(object jobObject)
        {
            var jobHandle = (JobHandle)jobObject;
            if (jobHandle.NotifyToken != null)
            {
                int token = (int)jobHandle.NotifyToken;
                if (MacOSNativeMethods.NOTIFY_STATUS_OK != MacOSNativeMethods.notify_cancel(token))
                {
                    throw new InvalidOperationException("Failed to cancel notify token!");
                }

                jobHandle.NotifyToken = null;
                _notifyTokens.Remove(token);
            }
        }

        protected override void RunNotify(FileStream eventStream, string eventName, string shmName)
        {
            MacOSNativeMethods.notify_post(shmName);
        }

        unsafe void RunNotifyThread()
        {
            uint fd = _notifyFD;
            byte[] bufferIn = new byte[4], bufferToken = new byte[4];

            int msgSize = Marshal.SizeOf(typeof(MacOSNativeMethods.mach_msg_t));
            while (true)
            {
                var msg = new MacOSNativeMethods.mach_msg_t();
                int ret = MacOSNativeMethods.mach_msg_overwrite(
                    IntPtr.Zero, MacOSNativeMethods.MACH_RCV_MSG,
                    0, (uint)msgSize, fd, 0,
                    0, (IntPtr)(void*)(&msg),
                    0);
                if (ret != MacOSNativeMethods.KERN_SUCCESS) { Debug.WriteLine("Mach error: " + ret.ToString()); continue; }

                int token = msg.header.msgh_id;
                lock (SyncRoot)
                {
                    JobHandle jobHandle;
                    if (_notifyTokens.TryGetValue(token, out jobHandle))
                    {
                        RunJobObject(jobHandle);
                    }
                }
            }
        }
    }
    #endregion

    #region Default Implementation
    internal class DefaultEventManager : EventManager
    {
        sealed class DefaultEvent : SystemEvent
        {
            bool _createdNew;
            EventWaitHandle _event;

            public DefaultEvent(string name)
                : base(name)
            {
                _event = new EventWaitHandle(false, EventResetMode.ManualReset, GetGlobalName(name), out _createdNew);
            }

            public override void Dispose()
            {
                try
                {
                    if (_event != null)
                    {
                        _event.Close();
                        _event = null;
                    }
                }
                catch
                {

                }
            }

            public override void Reset()
            {
                try { _event.Reset(); }
                catch { }
            }

            public override void Set()
            {
                try { _event.Set(); }
                catch { }
            }

            public override bool CreatedNew
            {
                get { return _createdNew; }
            }

            public override WaitHandle WaitHandle
            {
                get { return _event; }
            }
        }

        sealed class DefaultMutex : SystemMutex
        {
            bool _createdNew;
            Mutex _mutex;

            public DefaultMutex(string name)
                : base(name)
            {
                _mutex = new Mutex(false, GetGlobalName(name), out _createdNew);
            }

            public override void Dispose()
            {
                try
                {
                    if (_mutex != null)
                    {
                        _mutex.Close();
                        _mutex = null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }

            protected override bool WaitOne(int timeout)
            {
                if (!_mutex.WaitOne(timeout)) { return false; }
                return true;
            }

            protected override void ReleaseMutex()
            {
                if (_mutex == null) { return; }
                _mutex.ReleaseMutex();
            }

            public override bool CreatedNew
            {
                get { return _createdNew; }
            }
        }

        static string GetGlobalName(string name)
        {
            if (name == null) { throw new ArgumentNullException(); }
            if (name.Length > 240) { name = "HIDSharp Global (" + Convert.ToBase64String(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(name))) + ")"; }
            return @"Global\" + name;
        }

        internal override void Start()
        {

        }

        public override SystemEvent CreateEvent(string name)
        {
            return new DefaultEvent(name);
        }

        public override SystemMutex CreateMutex(string name)
        {
            return new DefaultMutex(name);
        }
    }
    #endregion
}
