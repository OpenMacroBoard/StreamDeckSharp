#region License
/* Copyright 2012-2013, 2016, 2018-2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Platform
{
    abstract class SysBleStream : Experimental.BleStream
    {
        internal SysBleStream(Experimental.BleDevice device, Experimental.BleService service)
            : base(device, service)
        {

        }

        #region Reference Counting
        SysRefCountHelper _rch;

        internal void HandleInitAndOpen()
        {
            _rch.HandleInitAndOpen();
        }

        internal bool HandleClose()
        {
            return _rch.HandleClose();
        }

        internal bool HandleAcquire()
        {
            return _rch.HandleAcquire();
        }

        internal void HandleAcquireIfOpenOrFail()
        {
            _rch.HandleAcquireIfOpenOrFail();
        }

        internal void HandleRelease()
        {
            if (_rch.HandleRelease()) { HandleFree(); }
        }

        internal abstract void HandleFree();
        #endregion
    }
}
