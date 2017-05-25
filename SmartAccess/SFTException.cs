using System;

namespace SmartAccess
{
    [Serializable]
    internal class SFTException : Exception
    {
        public SFTException()
        {
        }

        public SFTException(string message) : base(message)
        {
        }

        public SFTException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}