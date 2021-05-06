using System;

namespace Archiver.Core.Exceptions
{
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message)
        {
        }
    }
}