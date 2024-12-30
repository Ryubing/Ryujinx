using SharpMetal;
using SharpMetal.Foundation;
using SharpMetal.ObjectiveCCore;
using SharpMetal.QuartzCore;
using System.Runtime.Versioning;
// ReSharper disable InconsistentNaming

namespace Ryujinx.Graphics.Metal.SharpMetalExtensions
{
    [SupportedOSPlatform("macOS")]
    public static class CAMetalLayerExtensions
    {
        private static readonly Selector sel_developerHUDProperties = "developerHUDProperties";
        private static readonly Selector sel_setDeveloperHUDProperties = "setDeveloperHUDProperties:";

        public static NSDictionary GetDeveloperHudProperties(this CAMetalLayer metalLayer)
            => new(ObjectiveCRuntime.IntPtr_objc_msgSend(metalLayer.NativePtr, sel_developerHUDProperties));

        public static void SetDeveloperHudProperties(this CAMetalLayer metalLayer, NSDictionary dictionary) 
            => ObjectiveCRuntime.objc_msgSend(metalLayer.NativePtr, sel_setDeveloperHUDProperties, dictionary);
    }
}
