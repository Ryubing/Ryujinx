using System;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal class InternalErrorException : Exception
    {
        public InternalErrorException(string message) : base(message)
        {
        }

        public InternalErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}