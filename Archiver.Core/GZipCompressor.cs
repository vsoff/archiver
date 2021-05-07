using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Archiver.Core.Common;
using Archiver.Core.Exceptions;
using Archiver.Core.Extensions;
using Archiver.Core.Serializers;

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

        /// <summary>
        /// Алгоритм работы одного треда сжатия.
        /// </summary>
        private void DoCompressWork(
            SyncAwaitQueue<FileBlock> sourceBlocks,
            SyncAwaitQueue<FileBlock> targetBlocks,
            CompressorActionType compressorAction)
        {
            while (sourceBlocks.TryDequeue(out var block))
            {
                var resultBlock = ApplyCompressAction(block, compressorAction);
                targetBlocks.Enqueue(resultBlock);
            }
        }

        /// <summary>
        /// Алгоритм работы треда записи данных в файл.
        /// </summary>
        private void DoWriteWork(string targetFile,
            SyncAwaitQueue<FileBlock> targetBlocks,
            CompressorSyncContext context,
            CompressorActionType actionType,
            long sourceFileBytesCount)
        {
            using FileStream targetStream = File.Create(targetFile);
            switch (actionType)
            {
                case CompressorActionType.Compress:
                {
                    // Запишем в поток пустой заголовок, после записи данных мы его перезапишем.
                    var blocksCount = (int) Math.Ceiling((double) sourceFileBytesCount / Configuration.BlockSizeBytes);
                    var emptyArchiveHeader = ArchiveHeader.CreateEmpty(blocksCount);
                    var emptyHeaderBytes = ArchiveHeaderSerializer.Serialize(emptyArchiveHeader);
                    targetStream.WriteArray(emptyHeaderBytes);

                    // Записываем все блоки файла и запоминаем их смещения, чтобы потом записать их в заголовок.
                    var blockOffsets = new long[blocksCount];
                    while (targetBlocks.TryDequeue(out var block))
                    {
                        var serializedData = FileBlockSerializer.Serialize(block);
                        blockOffsets[block.Index] = targetStream.Position;

                        targetStream.WriteArray(serializedData);
                        context.IncrementWriteCount();
                    }

                    // Перезаписываем заголовок, чтобы знать смещения каждого блока.
                    var newHeader = new ArchiveHeader(blockOffsets);
                    var newHeaderBytes = ArchiveHeaderSerializer.Serialize(newHeader);
                    if (emptyHeaderBytes.Length != newHeaderBytes.Length)
                        throw new InvalidOperationException("Количество байт у пустого заголовка и заполненного не совпадает");

                    targetStream.Position = 0;
                    targetStream.WriteArray(newHeaderBytes);

                    break;
                }
                case CompressorActionType.Decompress:
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
                            targetStream.Write(removedBlock.Data, 0, removedBlock.Data.Length);
                            index++;
                            context.IncrementWriteCount();
                        } while (sortedBlocks.Count != 0);
                    }

                    break;
                }
                default: throw new UnknownCompressorActionTypeException(actionType);
            }
        }

        /// <summary>
        /// Алгоритм работы треда чтения данных из файла.
        /// </summary>
        private void DoReadWork(string sourceFile,
            SyncAwaitQueue<FileBlock> sourceBlocks,
            CompressorSyncContext context,
            CompressorActionType actionType)
        {
            using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open))
            {
                switch (actionType)
                {
                    case CompressorActionType.Compress:
                        int readedBytes;
                        byte[] buffer = new byte[Configuration.BlockSizeBytes];
                        int offset = 0;
                        int index = 0;
                        do
                        {
                            context.IncrementReadCount();
                            readedBytes = sourceStream.Read(buffer, offset, Configuration.BlockSizeBytes);

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

                        break;
                    case CompressorActionType.Decompress:
                        var headerBytes = sourceStream.ReadArray();
                        var header = ArchiveHeaderSerializer.Deserialize(headerBytes);

                        foreach (var blockOffset in header.BlockOffsets)
                        {
                            context.IncrementReadCount();
                            sourceStream.Position = blockOffset;
                            var blockBytes = sourceStream.ReadArray();
                            var block = FileBlockSerializer.Deserialize(blockBytes);

                            sourceBlocks.Enqueue(block);
                        }

                        break;
                    default: throw new UnknownCompressorActionTypeException(actionType);
                }
            }

            sourceBlocks.ResetAwait();
        }

        /// <summary>
        /// Выполняет компрессию/декомпрессию для одного блока файла.
        /// </summary>
        private FileBlock ApplyCompressAction(FileBlock block, CompressorActionType actionType)
        {
            byte[] newData;
            switch (actionType)
            {
                case CompressorActionType.Compress:
                    using (MemoryStream memoryStream = new MemoryStream())
                    using (GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Compress, false))
                    {
                        zipStream.Write(block.Data, 0, block.Size);
                        zipStream.Flush();
                        newData = memoryStream.ToArray();
                    }

                    break;
                case CompressorActionType.Decompress:

                    using (MemoryStream memoryStream = new MemoryStream(block.Data))
                    using (GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress, false))
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            zipStream.CopyTo(resultStream);
                            zipStream.Flush();
                            newData = resultStream.ToArray();
                        }
                    }

                    break;
                default: throw new UnknownCompressorActionTypeException(actionType);
            }

            return new FileBlock(block.Index, newData);
        }

        public void Compress(string sourceFile, string targetFile)
            => ExecuteCompressorAction(sourceFile, targetFile, CompressorActionType.Compress);

        public void Decompress(string sourceFile, string targetFile)
            => ExecuteCompressorAction(sourceFile, targetFile, CompressorActionType.Decompress);

        private void ExecuteCompressorAction(string sourceFile, string targetFile, CompressorActionType actionType)
        {
            FileInfo sourceFileInfo = new FileInfo(sourceFile);

            _logger.Info($"{nameof(ExecuteCompressorAction)}: Операция {actionType} запущена");

            var sourceBlocks = new SyncAwaitQueue<FileBlock>();
            var targetBlocks = new SyncAwaitQueue<FileBlock>();
            var syncContext = new CompressorSyncContext(Configuration.MaxCountDelta);

            var writeThread = new Thread(() => DoWriteWork(targetFile, targetBlocks, syncContext, actionType, sourceFileInfo.Length));
            var readThread = new Thread(() => DoReadWork(sourceFile, sourceBlocks, syncContext, actionType));
            var threads = Enumerable.Range(0, Configuration.ThreadsCount)
                .Select(_ => new Thread(() => DoCompressWork(sourceBlocks, targetBlocks, actionType)))
                .ToList();

            // Запускаем все треды.
            writeThread.Start();
            readThread.Start();
            foreach (var shell in threads)
                shell.Start();

            // Дожидаемся выполнения всех тредов.
            readThread.Join();
            _logger.Info("Файл полностью прочитан. Дожидаемся завершения потоков...");

            foreach (Thread thread in threads)
                thread.Join();

            targetBlocks.ResetAwait();
            writeThread.Join();

            _logger.Info($"{nameof(ExecuteCompressorAction)}: Все потоки завершились, выполнение операции {actionType} закончено");
        }
    }
}