namespace Ryujinx.Ava.Common.Models
{
    // NOTE: most consuming code relies on this model being value-comparable
    public record DownloadableContentModel(ulong TitleId, string ContainerPath, string FullPath)
    {
        public bool IsBundled { get; } = System.IO.Path.GetExtension(ContainerPath)?.ToLower() == ".xci";

        public string FileName => System.IO.Path.GetFileName(ContainerPath);
        public string TitleIdStr => TitleId.ToString("x16").ToUpper();
        public ulong TitleIdBase => TitleId & ~0x1FFFUL;
    }
}
