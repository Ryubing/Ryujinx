using SharpMetal.Foundation;
using SharpMetal.ObjectiveCCore;
using System.Runtime.Versioning;
// ReSharper disable InconsistentNaming

namespace Ryujinx.Graphics.Metal.SharpMetalExtensions
{
    [SupportedOSPlatform("macOS")]
    public static class NSHelper
    {
        private static readonly Selector sel_getCStringMaxLengthEncoding = "getCString:maxLength:encoding:";
        private static readonly Selector sel_stringWithUTF8String = "stringWithUTF8String:";
        
        public static unsafe string ToDotNetString(this NSString source)
        {
            char[] sourceBuffer = new char[source.Length];
            fixed (char* pSourceBuffer = sourceBuffer)
            {
                ObjectiveC.bool_objc_msgSend(source,
                    sel_getCStringMaxLengthEncoding,
                    pSourceBuffer,
                    source.MaximumLengthOfBytes(NSStringEncoding.UTF16) + 1,
                    (ulong)NSStringEncoding.UTF16);
            }

            return new string(sourceBuffer);
        }
        
        public static NSString ToNSString(this string source) 
            => new(ObjectiveC.IntPtr_objc_msgSend(new ObjectiveCClass(nameof(NSString)), sel_stringWithUTF8String, source));
    }
}
