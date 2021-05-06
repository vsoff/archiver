using System;
using System.Threading;

namespace Archiver.Core.Common
{
    public class ThreadShell
    {
        public readonly Thread Thread;
        public readonly AutoResetEvent ResetEvent;

        public ThreadShell(Thread thread, AutoResetEvent resetEvent)
        {
            Thread = thread ?? throw new ArgumentNullException(nameof(thread));
            ResetEvent = resetEvent ?? throw new ArgumentNullException(nameof(resetEvent));
        }
    }
}