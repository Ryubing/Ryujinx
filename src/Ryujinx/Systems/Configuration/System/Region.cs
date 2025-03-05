using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Systems.Configuration.System
{
    [JsonConverter(typeof(TypedStringEnumConverter<Region>))]
    public enum Region
    {
        Japan,
        USA,
        Europe,
        Australia,
        China,
        Korea,
        Taiwan,
    }
    
    public static class RegionEnumHelper
    {
        public static Region ToUI(this HLE.HOS.SystemState.RegionCode hleRegion)
            => (Region)hleRegion;

        public static HLE.HOS.SystemState.RegionCode ToHLE(this Region uiRegion)
            => (HLE.HOS.SystemState.RegionCode)uiRegion;
    }
}
