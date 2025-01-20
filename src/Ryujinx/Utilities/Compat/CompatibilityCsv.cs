using Gommon;
using Humanizer;
using nietras.SeparatedValues;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Common.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ryujinx.Ava.Utilities.Compat
{
    public struct ColumnIndices(Func<ReadOnlySpan<char>, int> getIndex)
    {
        public const string TitleIdCol = "\"title_id\"";
        public const string GameNameCol = "\"game_name\"";
        public const string LabelsCol = "\"labels\"";
        public const string StatusCol = "\"status\"";
        public const string LastUpdatedCol = "\"last_updated\"";
        
        public readonly int TitleId = getIndex(TitleIdCol);
        public readonly int GameName = getIndex(GameNameCol);
        public readonly int Labels = getIndex(LabelsCol);
        public readonly int Status = getIndex(StatusCol);
        public readonly int LastUpdated = getIndex(LastUpdatedCol);
    }
    
    public class CompatibilityCsv
    {
        static CompatibilityCsv() => Load();

        public static void Load()
        {
            using Stream csvStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("RyujinxGameCompatibilityList")!;
            csvStream.Position = 0;

            using SepReader reader = Sep.Reader().From(csvStream);
            ColumnIndices columnIndices = new(reader.Header.IndexOf);

            _entries = reader
                .Enumerate(row => new CompatibilityEntry(ref columnIndices, row))
                .OrderBy(it => it.GameName)
                .ToArray();
            
            Logger.Debug?.Print(LogClass.UI, "Compatibility CSV loaded.", "LoadCompatibility");
        }

        public static void Unload()
        {
            _entries = null;
        }

        private static CompatibilityEntry[] _entries;
        
        public static CompatibilityEntry[] Entries 
        {
            get
            {
                if (_entries == null)
                    Load();
                
                return _entries;
            }
        }
    }

    public class CompatibilityEntry
    {
        public CompatibilityEntry(ref ColumnIndices indices, SepReader.Row row)
        {
            string titleIdRow = ColStr(row[indices.TitleId]);
            TitleId = !string.IsNullOrEmpty(titleIdRow) 
                ? titleIdRow 
                : default(Optional<string>);
            
            GameName = ColStr(row[indices.GameName]);

            Labels = ColStr(row[indices.Labels]).Split(';');
            Status = ColStr(row[indices.Status]).ToLower() switch
            {
                "playable" => LocaleKeys.CompatibilityListPlayable,
                "ingame" => LocaleKeys.CompatibilityListIngame,
                "menus" => LocaleKeys.CompatibilityListMenus,
                "boots" => LocaleKeys.CompatibilityListBoots,
                "nothing" => LocaleKeys.CompatibilityListNothing,
                _ => null
            };

            if (DateTime.TryParse(ColStr(row[indices.LastUpdated]), out var dt))
                LastUpdated = dt;

            return;
            
            string ColStr(SepReader.Col col) => col.ToString().Trim('"');
        }
        
        public string GameName { get; }
        public Optional<string> TitleId { get; }
        public string[] Labels { get; }
        public LocaleKeys? Status { get; }
        public DateTime LastUpdated { get; }

        public string LocalizedLastUpdated =>
            LocaleManager.FormatDynamicValue(LocaleKeys.CompatibilityListLastUpdated, LastUpdated.Humanize());

        public string LocalizedStatus => LocaleManager.Instance[Status!.Value];
        public string FormattedTitleId => TitleId
            .OrElse(new string(' ', 16));

        public string FormattedIssueLabels => Labels
            .Select(FormatLabelName)
            .JoinToString(", ");

        public override string ToString()
        {
            StringBuilder sb = new("CompatibilityEntry: {");
            sb.Append($"{nameof(GameName)}=\"{GameName}\", ");
            sb.Append($"{nameof(TitleId)}={TitleId}, ");
            sb.Append($"{nameof(Labels)}={
                Labels.FormatCollection(it => $"\"{it}\"", separator: ", ", prefix: "[", suffix: "]")
            }, ");
            sb.Append($"{nameof(Status)}=\"{Status}\", ");
            sb.Append($"{nameof(LastUpdated)}=\"{LastUpdated}\"");
            sb.Append('}');

            return sb.ToString();
        }

        public static string FormatLabelName(string labelName) => labelName.ToLower() switch
        {
            "audio" => "Audio",
            "bug" => "Bug",
            "cpu" => "CPU",
            "gpu" => "GPU",
            "gui" => "GUI",
            "help wanted" => "Help Wanted",
            "horizon" => "Horizon",
            "infra" => "Project Infra",
            "invalid" => "Invalid",
            "kernel" => "Kernel",
            "ldn" => "LDN",
            "linux" => "Linux",
            "macos" => "macOS",
            "question" => "Question",
            "windows" => "Windows",
            "graphics-backend:opengl" => "Graphics: OpenGL",
            "graphics-backend:vulkan" => "Graphics: Vulkan",
            "ldn-works" => "LDN Works",
            "ldn-untested" => "LDN Untested",
            "ldn-broken" => "LDN Broken",
            "ldn-partial" => "Partial LDN",
            "nvdec" => "NVDEC",
            "services" => "NX Services",
            "services-horizon" => "Horizon OS Services",
            "slow" => "Runs Slow",
            "crash" => "Crashes",
            "deadlock" => "Deadlock",
            "regression" => "Regression",
            "opengl" => "OpenGL",
            "opengl-backend-bug" => "OpenGL Backend Bug",
            "vulkan-backend-bug" => "Vulkan Backend Bug",
            "mac-bug" => "Mac-specific Bug(s)",
            "amd-vendor-bug" => "AMD GPU Bug",
            "intel-vendor-bug" => "Intel GPU Bug",
            "loader-allocator" => "Loader Allocator",
            "audout" => "AudOut",
            "32-bit" => "32-bit Game",
            "UE4" => "Unreal Engine 4",
            "homebrew" => "Homebrew Content",
            "online-broken" => "Online Broken",
            _ => Capitalize(labelName)
        };
        
        public static string Capitalize(string value)
        {
            if (value == string.Empty)
                return string.Empty;
        
            char firstChar = value[0];
            string rest = value[1..];

            return $"{char.ToUpper(firstChar)}{rest}";
        }
    }
}
