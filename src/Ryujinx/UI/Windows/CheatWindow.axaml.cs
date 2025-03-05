using Avalonia.Collections;
using LibHac.Tools.FsSystem;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class CheatWindow : StyleableAppWindow
    {
        private readonly string _enabledCheatsPath;
        public bool NoCheatsFound { get; }

        public AvaloniaList<CheatNode> LoadedCheats { get; }

        public string Heading { get; }
        public string BuildId { get; }

        public CheatWindow()
        {
            DataContext = this;

            InitializeComponent();

            Title = RyujinxApp.FormatTitle(LocaleKeys.CheatWindowTitle);
        }

        public CheatWindow(VirtualFileSystem virtualFileSystem, string titleId, string titleName, string titlePath)
        {
            MinWidth = 500;
            MinHeight = 650;
            
            LoadedCheats = [];

            Heading = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.CheatWindowHeading, titleName, titleId.ToUpper());
            BuildId = ApplicationData.GetBuildId(virtualFileSystem, ConfigurationState.Instance.System.IntegrityCheckLevel, titlePath);

            InitializeComponent();

            string modsBasePath = ModLoader.GetModsBasePath();
            string titleModsPath = ModLoader.GetApplicationDir(modsBasePath, titleId);
            ulong titleIdValue = ulong.Parse(titleId, NumberStyles.HexNumber);

            _enabledCheatsPath = Path.Combine(titleModsPath, "cheats", "enabled.txt");

            string[] enabled = [];

            if (File.Exists(_enabledCheatsPath))
            {
                enabled = File.ReadAllLines(_enabledCheatsPath);
            }

            int cheatAdded = 0;

            ModLoader.ModCache mods = new();

            ModLoader.QueryContentsDir(mods, new DirectoryInfo(Path.Combine(modsBasePath, "contents")), titleIdValue, []);

            string currentCheatFile = string.Empty;
            string buildId = string.Empty;

            CheatNode currentGroup = null;

            foreach (ModLoader.Cheat cheat in mods.Cheats)
            {
                if (cheat.Path.FullName != currentCheatFile)
                {
                    currentCheatFile = cheat.Path.FullName;
                    string parentPath = currentCheatFile.Replace(titleModsPath, string.Empty);

                    buildId = Path.GetFileNameWithoutExtension(currentCheatFile).ToUpper();
                    currentGroup = new CheatNode(string.Empty, buildId, parentPath, true);

                    LoadedCheats.Add(currentGroup);
                }

                CheatNode model = new(cheat.Name, buildId, string.Empty, false, enabled.Contains($"{buildId}-{cheat.Name}"));
                currentGroup?.SubNodes.Add(model);

                cheatAdded++;
            }

            if (cheatAdded == 0)
            {
                NoCheatsFound = true;
            }

            DataContext = this;

            Title = RyujinxApp.FormatTitle(LocaleKeys.CheatWindowTitle);
        }

        public void Save()
        {
            if (NoCheatsFound)
                return;

            IEnumerable<string> enabledCheats = LoadedCheats.SelectMany(it => it.SubNodes)
                .Where(it => it.IsEnabled)
                .Select(it => it.BuildIdKey);

            Directory.CreateDirectory(Path.GetDirectoryName(_enabledCheatsPath)!);

            File.WriteAllLines(_enabledCheatsPath, enabledCheats);

            Close();
        }
    }
}
