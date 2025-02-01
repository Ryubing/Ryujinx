namespace Ryujinx.Common.Configuration
{
    public enum VSyncMode
    {
        Switch,
        Unbounded,
        Custom
    }
    
    public static class VSyncModeExtensions
    {
        public static VSyncMode Next(this VSyncMode vsync, bool customEnabled = false) =>
            vsync switch
            {
                VSyncMode.Switch => customEnabled ? VSyncMode.Custom : VSyncMode.Unbounded,
                VSyncMode.Unbounded => VSyncMode.Switch,
                VSyncMode.Custom => VSyncMode.Unbounded,
                _ => VSyncMode.Switch
            };
    }
}
