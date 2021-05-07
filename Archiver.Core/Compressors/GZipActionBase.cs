using System;
using System.IO;
using System.Linq;
using System.Threading;
using Archiver.Core.Common;
using Archiver.Core.Exceptions;

namespace Archiver.Core.Compressors
{
    public abstract class GZipActionBase
    {
        public readonly GZipCompressorConfiguration Configuration;

        protected readonly ILogger Logger;

        protected GZipActionBase(GZipCompressorConfiguration configuration, ILogger logger)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract void Write(FileStream stream,
            SyncAwaitQueue<FileBlock> targetBlocks,
            CompressorSyncContext context,
            long sourceFileBytesCount);

        protected abstract void Read(FileStream stream,
            SyncAwaitQueue<FileBlock> sourceBlocks,
            CompressorSyncContext context);

        protected abstract FileBlock ApplyCompression(FileBlock block);


        /// <summary>
        /// Алгоритм работы одного треда сжатия.
        /// </summary>
        private void DoCompressWork(
            SyncAwaitQueue<FileBlock> sourceBlocks,
            SyncAwaitQueue<FileBlock> targetBlocks)
        {
            while (sourceBlocks.TryDequeue(out var block))
            {
                var resultBlock = ApplyCompression(block);
                targetBlocks.Enqueue(resultBlock);
            }
        }

        /// <summary>
        /// Алгоритм работы треда записи данных в файл.
        /// </summary>
        private void DoWriteWork(string targetFile,
            SyncAwaitQueue<FileBlock> targetBlocks,
            CompressorSyncContext context,
            long sourceFileBytesCount)
        {
            using FileStream targetStream = File.Create(targetFile);
            Write(targetStream, targetBlocks, context, sourceFileBytesCount);
        }

        /// <summary>
        /// Алгоритм работы треда чтения данных из файла.
        /// </summary>
        private void DoReadWork(string sourceFile,
            SyncAwaitQueue<FileBlock> sourceBlocks,
            CompressorSyncContext context)
        {
            using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open))
                Read(sourceStream, sourceBlocks, context);

            sourceBlocks.ResetAwait();
        }

        public void ApplyCompressorAction(string sourceFile, string targetFile)
        {
            FileInfo sourceFileInfo = new FileInfo(sourceFile);
            if (sourceFileInfo.Length == 0)
                throw new BusinessLogicException("Нельзя производить компрессию/декомпрессию для пустого файла");

            Logger.Info($"{nameof(ApplyCompressorAction)}: Операция запущена");

            var sourceBlocks = new SyncAwaitQueue<FileBlock>();
            var targetBlocks = new SyncAwaitQueue<FileBlock>();
            var syncContext = new CompressorSyncContext(Configuration.MaxCountDelta);

            var writeThread = new Thread(() => DoWriteWork(targetFile, targetBlocks, syncContext, sourceFileInfo.Length));
            var readThread = new Thread(() => DoReadWork(sourceFile, sourceBlocks, syncContext));
            var threads = Enumerable.Range(0, Configuration.ThreadsCount)
                .Select(_ => new Thread(() => DoCompressWork(sourceBlocks, targetBlocks)))
                .ToList();

            // Запускаем все треды.
            writeThread.Start();
            readThread.Start();
            foreach (var shell in threads)
                shell.Start();

            // Дожидаемся выполнения всех тредов.
            readThread.Join();
            Logger.Info("Файл полностью прочитан. Дожидаемся завершения потоков...");

            foreach (Thread thread in threads)
                thread.Join();

            targetBlocks.ResetAwait();
            writeThread.Join();

            Logger.Info($"{nameof(ApplyCompressorAction)}: Все потоки завершились, выполнение операции закончено");
        }
    }
}