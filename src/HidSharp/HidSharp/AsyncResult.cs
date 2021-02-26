#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Threading;

namespace HidSharp
{
    sealed class AsyncResult<T> : IAsyncResult
    {
        volatile bool _isCompleted;
        ManualResetEvent _waitHandle;

        AsyncResult(AsyncCallback callback, object state)
        {
            AsyncCallback = callback; AsyncState = state;
        }

        void Complete()
        {
            lock (this)
            {
                if (_isCompleted) { return; } _isCompleted = true;
                if (_waitHandle != null) { _waitHandle.Set(); }
            }

            if (AsyncCallback != null) { AsyncCallback(this); }
        }

        internal delegate T OperationCallback();

        internal static IAsyncResult BeginOperation(OperationCallback operation,
            AsyncCallback callback, object state)
        {
            var ar = new AsyncResult<T>(callback, state);
            ThreadPool.QueueUserWorkItem(delegate(object self)
            {
                try { ar.Result = operation(); }
                catch (Exception e) { ar.Exception = e; }
                ar.Complete();
            }, ar);
            return ar;
        }

        internal T EndOperation()
        {
            while (true)
            {
                if (IsCompleted)
                {
                    if (Exception != null) { throw Exception; }
                    return Result;
                }
                AsyncWaitHandle.WaitOne();
            }
        }

        internal static T EndOperation(IAsyncResult asyncResult)
        {
            Throw.If.Null(asyncResult);
            return ((AsyncResult<T>)asyncResult).EndOperation();
        }

        public AsyncCallback AsyncCallback
        {
            get;
            private set;
        }

        public object AsyncState
        {
            get;
            private set;
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (this)
                {
                    if (_waitHandle == null)
                    {
                        _waitHandle = new ManualResetEvent(_isCompleted);
                    }
                }

                return _waitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        Exception Exception
        {
            get;
            set;
        }

        T Result
        {
            get;
            set;
        }
    }
}