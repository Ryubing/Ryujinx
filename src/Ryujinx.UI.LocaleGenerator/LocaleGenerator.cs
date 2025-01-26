using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ryujinx.UI.LocaleGenerator
{
    [Generator]
    public class LocaleGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> localeFile = context.AdditionalTextsProvider.Where(static x => x.Path.EndsWith("locales.json"));

            IncrementalValuesProvider<string> contents = localeFile.Select((text, cancellationToken) => text.GetText(cancellationToken)!.ToString());

            context.RegisterSourceOutput(contents, (spc, content) =>
            {
                IEnumerable<string> lines = content.Split('\n').Where(x => x.Trim().StartsWith("\"ID\":")).Select(x => x.Split(':')[1].Trim().Replace("\"", string.Empty).Replace(",", string.Empty));

                StringBuilder enumSourceBuilder = new();
                enumSourceBuilder.AppendLine("namespace Ryujinx.Ava.Common.Locale;");
                enumSourceBuilder.AppendLine("public enum LocaleKeys");
                enumSourceBuilder.AppendLine("{");
                foreach (string? line in lines)
                {
                    enumSourceBuilder.AppendLine($"    {line},");
                }
                enumSourceBuilder.AppendLine("}");

                spc.AddSource("LocaleKeys", enumSourceBuilder.ToString());
            });
        }
    }
}
