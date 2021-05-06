using System;

namespace Archiver.Core.Common
{
    public interface ILogger
    {
        public void Info(string message);
        public void Debug(string message);
        public void Warning(string message);
        public void Error(string message, Exception ex = null);
    }
}