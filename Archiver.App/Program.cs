﻿using System;
using System.Diagnostics;
using Archiver.Core;
using Archiver.Core.Common;

namespace Archiver.App
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var logger = new ConsoleLogger();
            logger.Info("Приложение запущено");

            bool success = true;
            try
            {
                // Читаем и валидируем входные параметры.
                var compressorParams = ApplicationParametersHelper.ParseArguments(args);
                if (!compressorParams.IsValid(out var errors))
                {
                    const string separator = "\n* ";
                    var problems = string.Join(separator, errors);
                    throw new BusinessLogicException($"В параметрах запуска приложения обнаружены следующие проблемы: {separator}{problems}");
                }

                logger.Info($"Будет выполнена операция {compressorParams.ActionType}" +
                            $"\nИсходный файл: {compressorParams.SourceFilePath}" +
                            $"\nВыходной файл: {compressorParams.TargetFilePath}.");

                // Делаем компрессию/декомпрессию.
                var configuration = new GZipCompressorConfiguration();
                var compressor = new GZipCompressor(configuration, logger);
                switch (compressorParams.ActionType)
                {
                    case CompressorActionType.Compress:
                        compressor.Compress(compressorParams.SourceFilePath, compressorParams.TargetFilePath);
                        break;
                    case CompressorActionType.Decompress:
                        compressor.Decompress(compressorParams.SourceFilePath, compressorParams.TargetFilePath);
                        break;
                    default: throw new InvalidOperationException($"Обнаружен неизвестный тип {nameof(CompressorActionType)}.{compressorParams.ActionType}");
                }
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
    }
}