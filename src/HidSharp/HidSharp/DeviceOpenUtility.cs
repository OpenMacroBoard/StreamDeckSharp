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

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using HidSharp.Platform;
using HidSharp.Platform.SystemEvents;
using HidSharp.Utility;

namespace HidSharp
{
    // We run everything in the same thread, because on .NET, named mutexes must be closed by the thread that created them.
    sealed class DeviceOpenUtility
    {
        public event EventHandler InterruptRequested;

        Device _device;

        string _resourcePrefix;
        object _syncRoot;
        Thread _thread;
        ManualResetEvent _threadStartEvent;
        Exception _threadStartError;

        AutoResetEvent _closeEvent;

        OpenPriority _priority;
        bool _interruptible;
        bool _transient;
        int _timeoutIfInterruptible;
        int _timeoutIfTransient;

        public DeviceOpenUtility(Device device, string streamPath, OpenConfiguration openConfig)
        {
            _device = device;

            _syncRoot = new object();
            _resourcePrefix = GetResourcePrefix(streamPath);

            _priority = (OpenPriority)openConfig.GetOption(OpenOption.Priority);
            _interruptible = (bool)openConfig.GetOption(OpenOption.Interruptible);
            _transient = (bool)openConfig.GetOption(OpenOption.Transient);
            _timeoutIfInterruptible = (int)openConfig.GetOption(OpenOption.TimeoutIfInterruptible);
            _timeoutIfTransient = (int)openConfig.GetOption(OpenOption.TimeoutIfTransient);

            HidSharpDiagnostics.Trace("Opening a device. Our priority is {0}, our interruptible state is {1}, and our transient state is {2}.",
                                      _priority, _interruptible, _transient);
        }

        public void Open()
        {
            Close();

            lock (_syncRoot)
            {
                _closeEvent = new AutoResetEvent(false);

                _threadStartEvent = new ManualResetEvent(false);
                _threadStartError = null;

                var thread = new Thread(Run) { IsBackground = true, Name = "HID Sharing Monitor" }; _thread = thread;
                thread.Start();

                try
                {
                    _threadStartEvent.WaitOne();
                    if (_threadStartError != null) { thread.Join(); throw _threadStartError; }
                }
                finally
                {
                    _threadStartEvent = null;
                    _threadStartError = null;
                }
            }
        }

        void Run()
        {
            SystemEvent exclusiveEvent = null;
            SystemMutex exclusiveMutex = null; IDisposable exclusiveLock = null;
            SystemMutex priorityMutex = null;
            SystemMutex transientMutex = null;

            try
            {
                var em = HidSelector.Instance.EventManager;

                // *** Open the device.
                try
                {
                    string transientName = GetResourceName("Transient");

                    // Create or open the exclusive event.
                    exclusiveEvent = em.CreateEvent(GetResourceName("Event"));

                    // Try to acquire the device.
                    exclusiveMutex = em.CreateMutex(GetResourceName("Lock"));
                    if (!exclusiveMutex.TryLock(0, out exclusiveLock))
                    {
                        // We failed just locking it outright. First, can we interrupt?
                        bool lockIsInterruptible = false;
                        for (int priority = (int)OpenPriority.Idle; priority < (int)_priority; priority++)
                        {
                            if (em.MutexMayExist(GetResourceNameForPriority((OpenPriority)priority)))
                            {
                                lockIsInterruptible = true; break;
                            }
                        }

                        // Let's try again.
                        bool lockIsTransient = em.MutexMayExist(transientName);
                        using (var tryPriorityMutex = (lockIsInterruptible ? em.CreateMutex(GetResourceNameForPriorityRequest()) : null))
                        {
                            exclusiveEvent.Set();

                            int timeout;
                            if (lockIsTransient)
                            {
                                timeout = Math.Max(0, _timeoutIfTransient);
                                HidSharpDiagnostics.Trace("Failed to open the device. Luckily, it is in use by a transient process. We will wait {0} ms.", timeout);
                            }
                            else if (lockIsInterruptible)
                            {
                                timeout = Math.Max(0, _timeoutIfInterruptible);
                                HidSharpDiagnostics.Trace("Failed to open the device. Luckily, it is in use by an interruptible process. We will wait {0} ms.", timeout);
                            }
                            else
                            {
                                timeout = 0;
                            }

                            if (!exclusiveMutex.TryLock(timeout, out exclusiveLock))
                            {
                                throw DeviceException.CreateIOException(_device, "The device is in use.", Utility.HResult.SharingViolation);
                            }
                        }
                    }

                    if (_transient)
                    {
                        transientMutex = em.CreateMutex(transientName);
                    }

                    if (_interruptible)
                    {
                        priorityMutex = em.CreateMutex(GetResourceNameForPriority(_priority));
                    }
                }
                catch (Exception e)
                {
                    _threadStartError = e; return;
                }
                finally
                {
                    _threadStartEvent.Set();
                }

                // *** OK! Now run the sharing monitor.
                {
                    var handles = new WaitHandle[] { _closeEvent, exclusiveEvent.WaitHandle };
                    Exception ex = null;

                    HidSharpDiagnostics.Trace("Started the sharing monitor thread ({0}).",
                                              Thread.CurrentThread.ManagedThreadId);
                    while (true)
                    {
                        try
                        {
                            if (WaitHandle.WaitAny(handles) == 0) { break; }
                        }
                        catch (Exception e)
                        {
                            ex = e; break;
                        }

                        lock (_syncRoot)
                        {
                            // Okay. We received the request. Let's check for request priorities higher than ours.
                            exclusiveEvent.Reset();

                            HidSharpDiagnostics.Trace("Received an interrupt request ({0}).",
                                                      Thread.CurrentThread.ManagedThreadId);

                            if (em.MutexMayExist(GetResourceNameForPriorityRequest()))
                            {
                                ThreadPool.QueueUserWorkItem(_ =>
                                    {
                                        var ev = InterruptRequested;
                                        if (ev != null) { ev(this, EventArgs.Empty); }
                                    });
                            }
                        }
                    }

                    HidSharpDiagnostics.Trace("Exited its sharing monitor thread ({0}).{1}",
                                              Thread.CurrentThread.ManagedThreadId,
                                              (ex != null ? " " + ex.ToString() : ""));
                }
            }
            finally
            {
                Close(ref priorityMutex);
                Close(ref transientMutex);
                Close(ref exclusiveLock);
                Close(ref exclusiveMutex);
                Close(ref exclusiveEvent);

                _closeEvent.Close();
                _closeEvent = null;
                _thread = null;
            }
        }

        public void Close()
        {
            var thread = _thread;
            if (thread != null)
            {
                var closeEvent = _closeEvent;
                if (closeEvent != null)
                {
                    closeEvent.Set();
                }

                thread.Join();
            }
        }

        static void Close<T>(ref T obj) where T : class, IDisposable
        {
            if (obj != null) { obj.Dispose(); obj = null; }
        }

        static string GetResourcePrefix(string devicePath)
        {
            string prefix = "Device Resource : ";
            prefix += Convert.ToBase64String(Encoding.UTF8.GetBytes(devicePath));
            prefix += " : ";
            return prefix;
        }

        string GetResourceName(string property)
        {
            return _resourcePrefix + property;
        }

        string GetResourceNameForPriority(OpenPriority priority)
        {
            return GetResourceName("Priority : " + ((int)priority).ToString());
        }

        string GetResourceNameForPriorityRequest()
        {
            return GetResourceName("Priority Request");
        }
    }
}
