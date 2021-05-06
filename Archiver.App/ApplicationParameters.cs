using Archiver.Core;

namespace Archiver.App
{
    /// <summary>
    /// Параметры запуска приложения.
    /// </summary>
    public class ApplicationParameters
    {
        public string SourceFilePath { get; private set; }
        public string TargetFilePath { get; private set; }
        public CompressorActionType ActionType { get; private set; }

        private ApplicationParameters()
        {
        }

        public class Builder
        {
            private string _sourceFilePath;
            private string _targetFilePath;
            private CompressorActionType _actionType;

            public Builder()
            {
                _actionType = CompressorActionType.Unknown;
            }

            public Builder SetSourceFilePath(string path)
            {
                _sourceFilePath = path;
                return this;
            }

            public Builder SetTargetFilePath(string path)
            {
                _targetFilePath = path;
                return this;
            }

            public Builder SetCompressorActionType(CompressorActionType compressorActionType)
            {
                _actionType = compressorActionType;
                return this;
            }

            public ApplicationParameters Build() => new ApplicationParameters
            {
                SourceFilePath = _sourceFilePath,
                TargetFilePath = _targetFilePath,
                ActionType = _actionType
            };
        }
    }
}