using System;
using System.Threading;

namespace Archiver.Core
{
    /// <summary>
    /// Контекст синхронизации количества чтений и записи.
    /// </summary>
    /// <remarks>Нужен для ограничения одновременного нахождения в оперативной памяти большого кол-ва блоков.</remarks>
    public class CompressorSyncContext
    {
        private readonly int _maxCountDelta;
        private readonly ManualResetEvent _readResetEvent;

        private readonly object _readLocker;
        private readonly object _writeLocker;

        private int _lastWriteCount;
        private int _lastReadCount;

        public CompressorSyncContext(int maxCountDelta)
        {
            if (maxCountDelta <= 0) throw new ArgumentOutOfRangeException(nameof(maxCountDelta));
            _maxCountDelta = maxCountDelta;
            _writeLocker = new object();
            _readLocker = new object();
            _readResetEvent = new ManualResetEvent(false);
            _lastReadCount = 0;
            _lastWriteCount = 0;
        }

        public void IncrementReadCount()
        {
            lock (_readLocker)
            {
                if (_lastReadCount - _lastWriteCount >= _maxCountDelta)
                {
                    _readResetEvent.WaitOne();
                    _readResetEvent.Reset();
                }

                Interlocked.Increment(ref _lastReadCount);
            }
        }

        public void IncrementWriteCount()
        {
            lock (_writeLocker)
            {
                _readResetEvent.Set();
                Interlocked.Increment(ref _lastWriteCount);
            }
        }
    }
}