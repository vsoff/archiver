using System;

namespace Archiver.Core.Exceptions
{
    /// <summary>
    /// Представляет ошибку неизвестного типа операции компрессора.
    /// </summary>
    public class UnknownCompressorActionTypeException : Exception
    {
        public CompressorActionType ActionType { get; }

        public UnknownCompressorActionTypeException(CompressorActionType actionType) : base($"Unknown {nameof(CompressorActionType)}")
        {
            ActionType = actionType;
        }
    }
}