using System;

namespace Ryujinx.Common
{
    public class RyujinxException : Exception
    {
        public RyujinxException(string message) : base(message)
        { }
    }
}
