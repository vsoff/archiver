﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archiver.Core;
using Archiver.Core.Exceptions;

namespace Archiver.App
{
    public static class ApplicationParametersHelper
    {
        public static ApplicationParameters ParseArguments(string[] args)
        {
            if (args.Length != 3)
                throw new BusinessLogicException($"Ожидалось получить 3 аргумента, но было получено {args.Length}");

            var builder = new ApplicationParameters.Builder()
                .SetSourceFilePath(args[1])
                .SetTargetFilePath(args[2]);

            switch (args[0])
            {
                case "compress":
                    builder.SetCompressorActionType(CompressorActionType.Compress);
                    break;
                case "decompress":
                    builder.SetCompressorActionType(CompressorActionType.Decompress);
                    break;
                default:
                    builder.SetCompressorActionType(CompressorActionType.Unknown);
                    break;
            }

            return builder.Build();
        }

        private static bool IsPathValid(string path)
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var invalidPathChars = Path.GetInvalidPathChars();

            if (invalidFileNameChars.Any(path.Contains))
                return false;

            if (invalidPathChars.Any(path.Contains))
                return false;

            return true;
        }

        public static bool IsValid(this ApplicationParameters parameters, out string[] errors)
        {
            List<string> validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(parameters.SourceFilePath))
                validationErrors.Add("Не указано название исходного файла");
            else if (!IsPathValid(parameters.SourceFilePath))
                validationErrors.Add("Путь до исходного файла невалиден");

            if (string.IsNullOrWhiteSpace(parameters.TargetFilePath))
                validationErrors.Add("Не указано название выходного файла");
            else if (!IsPathValid(parameters.TargetFilePath))
                validationErrors.Add("Путь до выходного файла невалиден");

            if (parameters.ActionType == CompressorActionType.Unknown)
                validationErrors.Add("Неизвестный тип действия");

            errors = validationErrors.ToArray();
            return errors.Length == 0;
        }
    }
}