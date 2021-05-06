using System;
using System.Threading;

namespace Archiver.Core
{
    public class CompressorSyncContext
    {
        private readonly int _maxIndexesDelta;
        private readonly ManualResetEvent _readResetEvent;

        private readonly object _readLocker;
        private readonly object _writeLocker;

        private int _lastWriteIndex;
        private int _lastReadIndex;

        public CompressorSyncContext(int maxIndexesDelta)
        {
            if (maxIndexesDelta <= 0) throw new ArgumentOutOfRangeException(nameof(maxIndexesDelta));
            _maxIndexesDelta = maxIndexesDelta;
            _writeLocker = new object();
            _readLocker = new object();
            _readResetEvent = new ManualResetEvent(false);
            _lastReadIndex = 0;
            _lastWriteIndex = 0;
        }

        public void IncrementReadIndex()
        {
            lock (_readLocker)
            {
                if (_lastReadIndex - _lastWriteIndex >= _maxIndexesDelta)
                {
                    _readResetEvent.WaitOne();
                    _readResetEvent.Reset();
                }

                Interlocked.Increment(ref _lastReadIndex);
            }
        }

        public void IncrementWriteIndex()
        {
            lock (_writeLocker)
            {
                _readResetEvent.Set();
                Interlocked.Increment(ref _lastWriteIndex);
            }
        }
    }
}