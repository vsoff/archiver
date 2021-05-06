using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Archiver.Core.Common;
using Archiver.Core.Exceptions;

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
            _logger.Info($"Поток ThreadId=={Thread.CurrentThread.ManagedThreadId} запущен");

            int iteration = 0;
            while (sourceBlocks.TryDequeue(out var block))
            {
                _logger.Info($"[{++iteration}] Hello from ThreadId=={Thread.CurrentThread.ManagedThreadId}, block={block}");
                var resultBlock = ApplyCompressAction(block, compressorAction);
                targetBlocks.Enqueue(resultBlock);
            }

            _logger.Info($"Поток ThreadId=={Thread.CurrentThread.ManagedThreadId} закончил работу. Итераций: {iteration}");
        }

        /// <summary>
        /// Алгоритм работы треда записи данных в файл.
        /// </summary>
        private void DoWriteWork(string targetFile, SyncAwaitQueue<FileBlock> targetBlocks, CompressorSyncContext context)
        {
            int index = 0;
            var sortedBlocks = new SortedList<int, FileBlock>();
            using FileStream targetStream = File.Create(targetFile);
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
                    targetStream.Write(removedBlock.Data);
                    index++;
                    context.IncrementWriteIndex();
                    _logger.Debug($"[====] В файл записан блок {removedBlock}, следующий ожидаемый индекс: {index}");
                } while (sortedBlocks.Count != 0);
            }

            _logger.Debug("Запись в файл завершена");
        }

        /// <summary>
        /// Алгоритм работы треда чтения данных из файла.
        /// </summary>
        private void DoReadWork(string sourceFile, SyncAwaitQueue<FileBlock> sourceBlocks, CompressorSyncContext context)
        {
            _logger.Debug("Начато чтение файла");
            using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open))
            {
                int readedBytes;
                byte[] buffer = new byte[Configuration.BlockSizeBytes];
                int offset = 0;
                int index = 0;
                do
                {
                    context.IncrementReadIndex();
                    readedBytes = sourceStream.Read(buffer, offset, Configuration.BlockSizeBytes);
                    _logger.Debug($"Вычитана порция в потоке Id=={Thread.CurrentThread.ManagedThreadId};" +
                                  $" Offset: {offset}; Readed bytes: {readedBytes}");

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

            sourceBlocks.ResetAwait();
            _logger.Debug("Файл полностью вычитан. Дожидаемся завершения потоков...");
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
                        newData = memoryStream.ToArray();
                        _logger.Info($"Old: {block.Data.Length}; New: {newData.Length}");
                    }

                    break;
                case CompressorActionType.Decompress:

                    using (MemoryStream memoryStream = new MemoryStream(block.Data))
                    using (GZipStream zipStream = new GZipStream(memoryStream, CompressionMode.Decompress, false))
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            zipStream.CopyTo(resultStream);
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
            _logger.Debug($"{nameof(ExecuteCompressorAction)}: Экшн {actionType} запущен");

            var sourceBlocks = new SyncAwaitQueue<FileBlock>();
            var targetBlocks = new SyncAwaitQueue<FileBlock>();
            var syncContext = new CompressorSyncContext(20);

            var writeThread = new Thread(() => DoWriteWork(targetFile, targetBlocks, syncContext));
            var readThread = new Thread(() => DoReadWork(sourceFile, sourceBlocks, syncContext));
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
            foreach (Thread thread in threads)
                thread.Join();

            targetBlocks.ResetAwait();
            writeThread.Join();

            _logger.Debug($"{nameof(ExecuteCompressorAction)}: Все потоки завершились, экшн {actionType} завершён");
        }
    }
}