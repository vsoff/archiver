using System;
using System.IO;
using System.IO.Compression;
using Archiver.Core.Common;

namespace Archiver.Core
{
    public class GZipCompressor
    {
        public readonly GZipCompressorConfiguration Configuration;

        private readonly ILogger _logger;

        public GZipCompressor(GZipCompressorConfiguration configuration, ILogger logger)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Compress(string sourceFile, string targetFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open);
            using FileStream targetStream = File.Create(targetFile);
            using var gzipStream = new GZipStream(targetStream, CompressionMode.Compress);
            sourceStream.CopyTo(gzipStream, Configuration.BlockSizeBytes);
        }

        public void Decompress(string sourceFile, string targetFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open);
            using FileStream targetStream = File.Create(targetFile);
            using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
            gzipStream.CopyTo(targetStream, Configuration.BlockSizeBytes);
        }
    }
}