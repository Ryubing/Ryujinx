using Gommon;
using nietras.SeparatedValues;
using Ryujinx.Ava.Common.Locale;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ryujinx.Ava.Utilities.Compat
{
    public class CompatibilityCsv
    {
        public static CompatibilityCsv Shared { get; set; }
        
        public CompatibilityCsv(SepReader reader)
        {
            var entries = new List<CompatibilityEntry>();

            foreach (var row in reader)
            {
                entries.Add(new CompatibilityEntry(reader.Header, row));
            }

            Entries = entries.Where(x => x.Status != null)
                .OrderBy(it => it.GameName).ToArray();
        }

        public CompatibilityEntry[] Entries { get; }
    }

    public class CompatibilityEntry
    {
        public CompatibilityEntry(SepReaderHeader header, SepReader.Row row)
        {
            if (row.ColCount != header.ColNames.Count)
                throw new InvalidDataException($"CSV row {row.RowIndex} ({row.ToString()}) has mismatched column count");
            
            var titleIdRow = ColStr(row[header.IndexOf("\"title_id\"")]);
            TitleId = !string.IsNullOrEmpty(titleIdRow) 
                ? titleIdRow 
                : default(Optional<string>);
            
            GameName = ColStr(row[header.IndexOf("\"game_name\"")]).Trim().Trim('"');

            IssueLabels = ColStr(row[header.IndexOf("\"labels\"")]).Split(';');
            Status = ColStr(row[header.IndexOf("\"status\"")]).ToLower() switch
            {
                "playable" => LocaleKeys.CompatibilityListPlayable,
                "ingame" => LocaleKeys.CompatibilityListIngame,
                "menus" => LocaleKeys.CompatibilityListMenus,
                "boots" => LocaleKeys.CompatibilityListBoots,
                "nothing" => LocaleKeys.CompatibilityListNothing,
                _ => null
            };

            if (DateTime.TryParse(ColStr(row[header.IndexOf("\"last_updated\"")]), out var dt))
                LastEvent = dt;

            return;
            
            string ColStr(SepReader.Col col) => col.ToString().Trim('"');
        }
        
        public string GameName { get; }
        public Optional<string> TitleId { get; }
        public string[] IssueLabels { get; }
        public LocaleKeys? Status { get; }
        public DateTime LastEvent { get; }

        public string LocalizedStatus => LocaleManager.Instance[Status!.Value];
        public string FormattedTitleId => TitleId
            .OrElse(new string(' ', 16));

        public string FormattedIssueLabels => IssueLabels
            .Where(it => !it.StartsWithIgnoreCase("status"))
            .Select(FormatLabelName)
            .JoinToString(", ");

        public override string ToString()
        {
            var sb = new StringBuilder("CompatibilityEntry: {");
            sb.Append($"{nameof(GameName)}=\"{GameName}\", ");
            sb.Append($"{nameof(TitleId)}={TitleId}, ");
            sb.Append($"{nameof(IssueLabels)}=\"{IssueLabels}\", ");
            sb.Append($"{nameof(Status)}=\"{Status}\", ");
            sb.Append($"{nameof(LastEvent)}=\"{LastEvent}\"");
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
        
            var firstChar = value[0];
            var rest = value[1..];

            return $"{char.ToUpper(firstChar)}{rest}";
        }
    }
}
