using System;

namespace Archiver.Core.Exceptions
{
    public class UnknownCompressorActionTypeException : Exception
    {
        public CompressorActionType ActionType { get; }

        public UnknownCompressorActionTypeException(CompressorActionType actionType) : base($"Unknown {nameof(CompressorActionType)}")
        {
            ActionType = actionType;
        }
    }
}