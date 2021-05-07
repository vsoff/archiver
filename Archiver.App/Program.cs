using System;
using System.Diagnostics;
using System.IO;
using Archiver.Core;
using Archiver.Core.Common;
using Archiver.Core.Compressors;
using Archiver.Core.Exceptions;

namespace Archiver.App
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var logger = new ConsoleLogger();
            logger.Info("Приложение запущено");

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                if (eventArgs.ExceptionObject is BusinessLogicException ble)
                    logger.Error(ble.Message);
                else
                    logger.Error("Произошло необработанное исключение", eventArgs.ExceptionObject as Exception);
                Environment.Exit(1);
            };

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                logger.Warning("Работа программы была прервана вручную");
                Environment.Exit(1);
            };

            bool success = true;
            try
            {
                // Читаем и валидируем входные параметры.
                var compressorParams = ApplicationParametersHelper.ParseArguments(args);

                if (!File.Exists(compressorParams.SourceFilePath))
                    throw new BusinessLogicException("Исходный файл не существует");

                logger.Info($"Будет выполнена операция {compressorParams.ActionType}" +
                            $"\nИсходный файл: {compressorParams.SourceFilePath}" +
                            $"\nВыходной файл: {compressorParams.TargetFilePath}.");

                // Делаем компрессию/декомпрессию.
                var configuration = new GZipCompressorConfiguration();
                var compressor = CreateCompressorActionType(configuration, logger, compressorParams.ActionType);
                compressor.ApplyCompressorAction(compressorParams.SourceFilePath, compressorParams.TargetFilePath);
            }
            catch (BusinessLogicException ex)
            {
                logger.Error(ex.Message);
                success = false;
            }
            catch (Exception ex)
            {
                logger.Error("Произошла необработанная ошибка", ex);
                success = false;
            }
            finally
            {
                logger.Info($"Приложение завершило свою работу за {sw.Elapsed}.");
                Environment.Exit(success ? 0 : 1);
            }
        }

        private static GZipActionBase CreateCompressorActionType(
            GZipCompressorConfiguration configuration,
            ILogger logger,
            CompressorActionType actionType)
        {
            switch (actionType)
            {
                case CompressorActionType.Compress:
                    return new GZipActionCompressor(configuration, logger);
                case CompressorActionType.Decompress:
                    return new GZipActionDecompressor(configuration, logger);
                default: throw new UnknownCompressorActionTypeException(actionType);
            }
        }
    }
}