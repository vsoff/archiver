using System;

namespace Archiver.Core
{
    /// <summary>
    /// Настройки компрессора.
    /// </summary>
    public class GZipCompressorConfiguration
    {
        public readonly int ThreadsCount;
        public readonly int BlockSizeBytes;

        /// <summary>
        /// Максимальное количество блоков, которое может находиться в оперативной памяти.
        /// </summary>
        public readonly int MaxCountDelta;

        public GZipCompressorConfiguration()
        {
            ThreadsCount = Environment.ProcessorCount * 2;
            MaxCountDelta = 20;
            BlockSizeBytes = 1 * 1024 * 1024;
        }
    }
}