namespace Archiver.Core
{
    public class GZipCompressorConfiguration
    {
        public readonly int ThreadsCount;
        public readonly int BlockSizeBytes;

        public GZipCompressorConfiguration()
        {
            //ThreadsCount = Environment.ProcessorCount * 2;
            ThreadsCount = 1;
            BlockSizeBytes = 1 * 1024 * 1024;
        }
    }
}