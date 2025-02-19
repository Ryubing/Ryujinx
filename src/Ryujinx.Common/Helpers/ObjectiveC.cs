using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Helper
{
    [SupportedOSPlatform("macos")]
    public static partial class ObjectiveC
    {
        private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";

        [LibraryImport(ObjCRuntime, StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint sel_getUid(string name);

        [LibraryImport(ObjCRuntime, StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint objc_getClass(string name);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(nint receiver, Selector selector);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(nint receiver, Selector selector, byte value);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(nint receiver, Selector selector, nint value);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(nint receiver, Selector selector, NSRect point);

        [LibraryImport(ObjCRuntime)]
        private static partial void objc_msgSend(nint receiver, Selector selector, double value);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static partial nint nint_objc_msgSend(nint receiver, Selector selector);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static partial nint nint_objc_msgSend(nint receiver, Selector selector, nint param);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
        private static partial nint nint_objc_msgSend(nint receiver, Selector selector, string param);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool bool_objc_msgSend(nint receiver, Selector selector, nint param);

        public readonly struct Object
        {
            public readonly nint ObjPtr;

            private Object(nint pointer)
            {
                ObjPtr = pointer;
            }

            public Object(string name)
            {
                ObjPtr = objc_getClass(name);
            }

            public void SendMessage(Selector selector)
            {
                objc_msgSend(ObjPtr, selector);
            }

            public void SendMessage(Selector selector, byte value)
            {
                objc_msgSend(ObjPtr, selector, value);
            }

            public void SendMessage(Selector selector, Object obj)
            {
                objc_msgSend(ObjPtr, selector, obj.ObjPtr);
            }

            public void SendMessage(Selector selector, NSRect point)
            {
                objc_msgSend(ObjPtr, selector, point);
            }

            public void SendMessage(Selector selector, double value)
            {
                objc_msgSend(ObjPtr, selector, value);
            }

            public Object GetFromMessage(Selector selector)
            {
                return new Object(nint_objc_msgSend(ObjPtr, selector));
            }

            public Object GetFromMessage(Selector selector, Object obj)
            {
                return new Object(nint_objc_msgSend(ObjPtr, selector, obj.ObjPtr));
            }

            public Object GetFromMessage(Selector selector, NSString nsString)
            {
                return new Object(nint_objc_msgSend(ObjPtr, selector, nsString.StrPtr));
            }

            public Object GetFromMessage(Selector selector, string param)
            {
                return new Object(nint_objc_msgSend(ObjPtr, selector, param));
            }

            public bool GetBoolFromMessage(Selector selector, Object obj)
            {
                return bool_objc_msgSend(ObjPtr, selector, obj.ObjPtr);
            }
        }

        public readonly struct Selector
        {
            public readonly nint SelPtr;

            private Selector(string name)
            {
                SelPtr = sel_getUid(name);
            }

            public static implicit operator Selector(string value) => new(value);
        }

        public readonly struct NSString
        {
            public readonly nint StrPtr;

            public NSString(string aString)
            {
                nint nsString = objc_getClass("NSString");
                StrPtr = nint_objc_msgSend(nsString, "stringWithUTF8String:", aString);
            }

            public static implicit operator nint(NSString nsString) => nsString.StrPtr;
        }

        public readonly struct NSPoint
        {
            public readonly double X;
            public readonly double Y;

            public NSPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public readonly struct NSRect
        {
            public readonly NSPoint Pos;
            public readonly NSPoint Size;

            public NSRect(double x, double y, double width, double height)
            {
                Pos = new NSPoint(x, y);
                Size = new NSPoint(width, height);
            }
        }
    }
}
