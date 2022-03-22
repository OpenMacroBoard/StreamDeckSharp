using System;
using System.Collections.Generic;
using System.Threading;

namespace StreamDeckSharp.Internals
{
    internal sealed class ConcurrentBufferedQueue<TKey, TValue> : IDisposable
    {
        private readonly object sync = new();

        private readonly Dictionary<TKey, TValue> valueBuffer = new();
        private readonly Queue<TKey> queue = new();

        private volatile bool isAddingCompleted;
        private volatile bool disposed;

        public int Count => queue.Count;

        public bool IsAddingCompleted
        {
            get
            {
                ThrowIfDisposed();
                return isAddingCompleted;
            }
        }

        public bool IsCompleted
        {
            get
            {
                lock (sync)
                {
                    ThrowIfDisposed();
                    return isAddingCompleted && Count == 0;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (sync)
            {
                ThrowIfDisposed();

                if (isAddingCompleted)
                {
                    throw new InvalidOperationException("Adding was already marked as completed.");
                }

                try
                {
                    valueBuffer[key] = value;

                    if (!queue.Contains(key))
                    {
                        queue.Enqueue(key);
                    }
                }
                finally
                {
                    Monitor.PulseAll(sync);
                }
            }
        }

        public (bool Success, TKey Key, TValue Value) Take()
        {
            lock (sync)
            {
                while (queue.Count < 1)
                {
                    ThrowIfDisposed();

                    if (isAddingCompleted)
                    {
                        return (false, default, default);
                    }

                    Monitor.Wait(sync);
                }

                ThrowIfDisposed();

                var key = queue.Dequeue();
                var value = valueBuffer[key];
                valueBuffer.Remove(key);

                return (true, key, value);
            }
        }

        public void CompleteAdding()
        {
            lock (sync)
            {
                if (isAddingCompleted)
                {
                    return;
                }

                isAddingCompleted = true;
                Monitor.PulseAll(sync);
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                if (!isAddingCompleted)
                {
                    CompleteAdding();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ConcurrentBufferedQueue<TKey, TValue>));
            }
        }
    }
}
