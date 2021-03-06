// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    internal struct WriteLock : IDisposable
    {
        private readonly Lock _lock;
        private int _isDisposed;

        public WriteLock(Lock @lock)
        {
            _isDisposed = 0;
            _lock = @lock;
            _lock.EnterWriteLock();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
