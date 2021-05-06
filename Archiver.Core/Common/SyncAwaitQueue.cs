using System.Collections.Generic;
using System.Threading;

namespace Archiver.Core
{
    public class SyncAwaitQueue<T>
    {
        private readonly ManualResetEvent _noElementsManualResetEvent;
        private readonly object _enqueueLocker;
        private readonly object _dequeueLocker;

        private readonly Queue<T> _queue;

        /// <summary>
        /// Указывает, нужно ли дожидаться новых элементов.
        /// </summary>
        public bool IsNeedAwait { get; private set; }

        public SyncAwaitQueue()
        {
            _queue = new Queue<T>();
            _enqueueLocker = new object();
            _dequeueLocker = new object();
            _noElementsManualResetEvent = new ManualResetEvent(false);
            IsNeedAwait = true;
        }

        /// <summary>
        /// Освобождает всех клиентов метода <see cref="TryDequeue"/> от ожидания появления новых элементов.
        /// </summary>
        public void ResetAwait()
        {
            lock (_enqueueLocker)
            {
                IsNeedAwait = false;
                _noElementsManualResetEvent.Set();
            }
        }

        public void Enqueue(T item)
        {
            lock (_enqueueLocker)
            {
                _queue.Enqueue(item);
                _noElementsManualResetEvent.Set();
            }
        }

        public bool TryDequeue(out T result)
        {
            bool isDequeued;
            lock (_dequeueLocker)
            {
                _noElementsManualResetEvent.WaitOne();

                lock (_enqueueLocker)
                {
                    isDequeued = _queue.TryDequeue(out result);

                    if (IsNeedAwait && _queue.Count == 0)
                        _noElementsManualResetEvent.Reset();
                }
            }

            return isDequeued;
        }
    }
}