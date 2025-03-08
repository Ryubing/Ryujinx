using Gommon;
using LibHac.Fs;
using LibHac.Ncm;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.HLE.FileSystem;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.Models
{
    public class SaveModel : BaseModel
    {
        private long _size;

        public ulong SaveId { get; }
        public ProgramId TitleId { get; }
        public string TitleIdString => TitleId.ToString();
        public UserId UserId { get; }
        public bool InGameList { get; }
        public string Title { get; }
        public byte[] Icon { get; }

        public long Size
        {
            get => _size; set
            {
                _size = value;
                SizeAvailable = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeString));
                OnPropertyChanged(nameof(SizeAvailable));
            }
        }

        public bool SizeAvailable { get; set; }

        public string SizeString => ValueFormatUtils.FormatFileSize(Size);

        public SaveModel(SaveDataInfo info)
        {
            SaveId = info.SaveDataId;
            TitleId = info.ProgramId;
            UserId = info.UserId;

            ApplicationData appData = RyujinxApp.MainWindow.ViewModel.Applications.FirstOrDefault(x => x.IdString.EqualsIgnoreCase(TitleIdString));

            InGameList = appData != null;

            if (InGameList)
            {
                Icon = appData.Icon;
                Title = appData.Name;
            }
            else
            {
                ApplicationMetadata appMetadata = ApplicationLibrary.LoadAndSaveMetaData(TitleIdString);
                Title = appMetadata.Title ?? TitleIdString;
            }

            Task.Run(() =>
            {
                string saveRoot = Path.Combine(VirtualFileSystem.GetNandPath(), $"user/save/{info.SaveDataId:x16}");

                long totalSize = GetDirectorySize(saveRoot);

                static long GetDirectorySize(string path)
                {
                    long size = 0;
                    if (Directory.Exists(path))
                    {
                        string[] directories = Directory.GetDirectories(path);
                        foreach (string directory in directories)
                        {
                            size += GetDirectorySize(directory);
                        }

                        string[] files = Directory.GetFiles(path);
                        foreach (string file in files)
                        {
                            size += new FileInfo(file).Length;
                        }
                    }

                    return size;
                }

                Size = totalSize;
            });

        }
    }
}
