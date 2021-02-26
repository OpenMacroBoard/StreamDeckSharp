#region License
/* Copyright 2012-2013, 2016, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HidSharp.Platform
{
    struct SysRefCountHelper
    {
        int _opened, _closed;
        int _refCount;

        internal void HandleInitAndOpen()
        {
            _opened = 1; _refCount = 1;
        }

        internal bool HandleClose()
        {
            return 0 == Interlocked.CompareExchange(ref _closed, 1, 0) && _opened != 0;
        }

        internal bool HandleAcquire()
        {
            while (true)
            {
                int refCount = _refCount;
                if (refCount == 0) { return false; }

                if (refCount == Interlocked.CompareExchange
                    (ref _refCount, refCount + 1, refCount))
                {
                    return true;
                }
            }
        }

        internal void HandleAcquireIfOpenOrFail()
        {
            if (_closed != 0 || !HandleAcquire()) { throw ExceptionForClosed(); }
        }

        internal bool HandleRelease()
        {
            if (0 == Interlocked.Decrement(ref _refCount))
            {
                if (_opened != 0) { return true; }
            }

            return false;
        }

        internal void ThrowIfClosed()
        {
            if (_closed != 0) { throw ExceptionForClosed(); }
        }

        static Exception ExceptionForClosed()
        {
            return CommonException.CreateClosedException();
        }
    }
}
