using System;

namespace Archiver.Core
{
    public class GZipCompressorConfiguration
    {
        public readonly int ThreadsCount;
        public readonly int BlockSizeBytes;

        public GZipCompressorConfiguration()
        {
            ThreadsCount = Environment.ProcessorCount * 2;
            BlockSizeBytes = 1 * 1024 * 1024;
        }
    }
}