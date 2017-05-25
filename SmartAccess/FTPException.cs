using System;
using Microsoft.SPOT;

namespace SmartAccess
{
    class FTPException : Exception
    {
        public FTPException(string message) : base(message)
        {
        }
    }
}
