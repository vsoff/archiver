using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using Archiver.Core.Common;
using Archiver.Core.Exceptions;
using Archiver.Core.Extensions;
using Archiver.Core.Serializers;

namespace Archiver.Core.Compressors
{
    public class GZipActionDecompressor : GZipActionBase
    {
        public GZipActionDecompressor(GZipCompressorConfiguration configuration, ILogger logger) : base(configuration, logger)
        {
        }

        protected override void Write(FileStream stream, SyncAwaitQueue<FileBlock> targetBlocks, CompressorSyncContext context, long sourceFileBytesCount)
        {
            int index = 0;
            var sortedBlocks = new SortedList<int, FileBlock>();
            while (targetBlocks.TryDequeue(out var block))
            {
                sortedBlocks.Add(block.Index, block);

                do
                {
                    // Если элемент не "последовательно ожидаемый", то пропускаем обработку.
                    var firstElement = sortedBlocks.First();
                    if (firstElement.Key != index)
                        break;

                    // Ожидаемый блок пишем в поток.
                    sortedBlocks.Remove(index, out var removedBlock);
                    stream.Write(removedBlock.Data, 0, removedBlock.Data.Length);
                    index++;
                    context.IncrementWriteCount();
                } while (sortedBlocks.Count != 0);
            }
        }

        protected override void Read(FileStream stream, SyncAwaitQueue<FileBlock> sourceBlocks, CompressorSyncContext context)
        {
            ArchiveHeader header;
            try
            {
                var headerBytes = stream.ReadArray();
                header = ArchiveHeaderSerializer.Deserialize(headerBytes);
            }
            catch (SerializationException)
            {
                throw new BusinessLogicException("Произошла ошибка при чтении заголовка архива, возможно файл не является архивом");
            }

            foreach (var blockOffset in header.BlockOffsets)
            {
                context.IncrementReadCount();
                stream.Position = blockOffset;
                var blockBytes = stream.ReadArray();
                var block = FileBlockSerializer.Deserialize(blockBytes);

                sourceBlocks.Enqueue(block);
            }
        }

        protected override FileBlock ApplyCompression(FileBlock block)
        {
            using MemoryStream memoryStream = new MemoryStream(block.Data);
            using GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress, false);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            zipStream.Flush();
            var newData = resultStream.ToArray();
            return new FileBlock(block.Index, newData);
        }
    }
}