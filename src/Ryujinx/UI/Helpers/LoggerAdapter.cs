using Avalonia.Logging;
using Avalonia.Utilities;
using Gommon;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Common.Logging;
using System;
using System.Text;

namespace Ryujinx.Ava.UI.Helpers
{
    using AvaLogger = Avalonia.Logging.Logger;
    using AvaLogLevel = LogEventLevel;
    using RyuLogClass = LogClass;
    using RyuLogger = Ryujinx.Common.Logging.Logger;

    internal class LoggerAdapter : ILogSink
    {
        private static bool _avaloniaLogsEnabled = ConfigurationState.Instance.Logger.EnableAvaloniaLog; 
        
        public static void Register()
        {
            AvaLogger.Sink = new LoggerAdapter();
            ConfigurationState.Instance.Logger.EnableAvaloniaLog.Event 
                += (_, e) => _avaloniaLogsEnabled = e.NewValue;
        }

        private static RyuLogger.Log? GetLog(AvaLogLevel level, string area)
        {
            if (!_avaloniaLogsEnabled) return null;
            
            return level switch
            {
                AvaLogLevel.Verbose => RyuLogger.Debug,
                AvaLogLevel.Debug => RyuLogger.Debug,
                AvaLogLevel.Information => RyuLogger.Debug,
                AvaLogLevel.Warning => RyuLogger.Debug,
                AvaLogLevel.Error => area is "IME" ? RyuLogger.Debug : RyuLogger.Error,
                AvaLogLevel.Fatal => RyuLogger.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
            };
        }

        public bool IsEnabled(AvaLogLevel level, string area)
        {
            return GetLog(level, area) != null;
        }

        public void Log(AvaLogLevel level, string area, object source, string messageTemplate)
        {
            GetLog(level, area)?.PrintMsg(RyuLogClass.UI, Format(level, area, messageTemplate, source, null));
        }

        public void Log(AvaLogLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            GetLog(level, area)?.PrintMsg(RyuLogClass.UI, Format(level, area, messageTemplate, source, propertyValues));
        }

        private static string Format(AvaLogLevel level, string area, string template, object source, object[] v)
        {
            StringBuilder result = new();
            CharacterReader r = new(template.AsSpan());
            int i = 0;

            result.Append('[');
            result.Append(level);
            result.Append("] ");

            result.Append('[');
            result.Append(area);
            result.Append("] ");

            while (!r.End)
            {
                char c = r.Take();

                if (c != '{')
                {
                    result.Append(c);
                }
                else
                {
                    if (r.Peek != '{')
                    {
                        result.Append('\'');
                        result.Append(i < v.Length ? v[i++] : null);
                        result.Append('\'');
                        r.TakeUntil('}');
                        r.Take();
                    }
                    else
                    {
                        result.Append('{');
                        r.Take();
                    }
                }
            }

            if (source != null)
            {
                result.Append(" (");
                result.Append(source.GetType().AsFullNamePrettyString());
                result.Append(" #");
                result.Append(source.GetHashCode());
                result.Append(')');
            }

            return result.ToString();
        }
    }
}
