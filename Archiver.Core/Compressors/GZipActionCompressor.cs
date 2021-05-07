using System;
using System.IO;
using System.IO.Compression;
using Archiver.Core.Common;
using Archiver.Core.Extensions;
using Archiver.Core.Serializers;

namespace Archiver.Core.Compressors
{
    public class GZipActionCompressor : GZipActionBase
    {
        public GZipActionCompressor(GZipCompressorConfiguration configuration, ILogger logger) : base(configuration, logger)
        {
        }

        protected override void Write(FileStream stream, SyncAwaitQueue<FileBlock> targetBlocks, CompressorSyncContext context, long sourceFileBytesCount)
        {
            // Запишем в поток пустой заголовок, после записи данных мы его перезапишем.
            var blocksCount = (int)Math.Ceiling((double)sourceFileBytesCount / Configuration.BlockSizeBytes);
            var emptyArchiveHeader = ArchiveHeader.CreateEmpty(blocksCount);
            var emptyHeaderBytes = ArchiveHeaderSerializer.Serialize(emptyArchiveHeader);
            stream.WriteArray(emptyHeaderBytes);

            // Записываем все блоки файла и запоминаем их смещения, чтобы потом записать их в заголовок.
            var blockOffsets = new long[blocksCount];
            while (targetBlocks.TryDequeue(out var block))
            {
                var serializedData = FileBlockSerializer.Serialize(block);
                blockOffsets[block.Index] = stream.Position;

                stream.WriteArray(serializedData);
                context.IncrementWriteCount();
            }

            // Перезаписываем заголовок, чтобы знать смещения каждого блока.
            var newHeader = new ArchiveHeader(blockOffsets);
            var newHeaderBytes = ArchiveHeaderSerializer.Serialize(newHeader);
            if (emptyHeaderBytes.Length != newHeaderBytes.Length)
                throw new InvalidOperationException("Количество байт у пустого заголовка и заполненного не совпадает");

            stream.Position = 0;
            stream.WriteArray(newHeaderBytes);
        }

        protected override void Read(FileStream stream, SyncAwaitQueue<FileBlock> sourceBlocks, CompressorSyncContext context)
        {
            int readedBytes;
            byte[] buffer = new byte[Configuration.BlockSizeBytes];
            int offset = 0;
            int index = 0;
            do
            {
                context.IncrementReadCount();
                readedBytes = stream.Read(buffer, offset, Configuration.BlockSizeBytes);

                // Обработка последней секции.
                if (readedBytes < Configuration.BlockSizeBytes)
                {
                    byte[] lastPart = new byte[readedBytes];
                    Array.Copy(buffer, 0, lastPart, 0, readedBytes);
                    var lastBlock = new FileBlock(index, lastPart);
                    sourceBlocks.Enqueue(lastBlock);
                    break;
                }

                var block = new FileBlock(index++, buffer);
                sourceBlocks.Enqueue(block);
            } while (readedBytes == Configuration.BlockSizeBytes);
        }

        protected override FileBlock ApplyCompression(FileBlock block)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Compress, false);
            zipStream.Write(block.Data, 0, block.Size);
            zipStream.Flush();
            var newData = memoryStream.ToArray();
            return new FileBlock(block.Index, newData);
        }
    }
}