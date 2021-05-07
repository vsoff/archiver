using System;

namespace Archiver.Core.Exceptions
{
    /// <summary>
    /// Представляет ошибку бизнес логики.
    /// </summary>
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message)
        {
        }
    }
}