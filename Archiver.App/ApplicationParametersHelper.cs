using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Archiver.Core;

namespace Archiver.App
{
    public static class ApplicationParametersHelper
    {
        public static ApplicationParameters ParseArguments(string[] args)
        {
            // TODO: сделать парсинг
            const string fileName = "harry potter 4.mp4";
            //const string fileName = "test.png";
            var source = fileName;
            var target = $"{fileName}.vzip";
            return new ApplicationParameters.Builder()
                .SetSourceFilePath(source)
                .SetTargetFilePath(target)
                .SetCompressorActionType(CompressorActionType.Compress)
                .Build();
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