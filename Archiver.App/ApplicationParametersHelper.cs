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
                throw new BusinessLogicException($"Ожидалось получить 3 аргумента, но было получено {args.Length}." +
                                                 " Необходимый формат: Archiver.App.exe [compress|decompress] source_file target_file");

            var actionType = args[0];
            var sourceFilePath = args[1];
            var targetFilePath = args[2];

            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new BusinessLogicException("Не указано название исходного файла");

            if (!IsPathValid(sourceFilePath))
                throw new BusinessLogicException("Путь до исходного файла невалиден");

            if (string.IsNullOrWhiteSpace(targetFilePath))
                throw new BusinessLogicException("Не указано название исходного файла");

            if (!IsPathValid(targetFilePath))
                throw new BusinessLogicException("Путь до исходного файла невалиден");

            var builder = new ApplicationParameters.Builder()
                .SetSourceFilePath(args[1])
                .SetTargetFilePath(args[2]);

            switch (actionType.ToLower())
            {
                case "compress":
                    builder.SetCompressorActionType(CompressorActionType.Compress);
                    break;
                case "decompress":
                    builder.SetCompressorActionType(CompressorActionType.Decompress);
                    break;
                default:
                    throw new BusinessLogicException($"Неизвестный тип операции компрессора: {actionType}");
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
    }
}