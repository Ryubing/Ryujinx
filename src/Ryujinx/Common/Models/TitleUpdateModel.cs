namespace Ryujinx.Ava.Common.Models
{
    // NOTE: most consuming code relies on this model being value-comparable
    public record TitleUpdateModel(ulong TitleId, ulong Version, string DisplayVersion, string Path)
    {
        public bool IsBundled { get; } = System.IO.Path.GetExtension(Path)?.ToLower() == ".xci";

        public string TitleIdStr => TitleId.ToString("x16");
        public ulong TitleIdBase => TitleId & ~0x1FFFUL;
    }
}
