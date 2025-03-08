using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using System;
using System.IO;

namespace Ryujinx.Ava.Systems.Configuration
{
    public static class LoggerModule
    {
        public static void Initialize()
        {
            ConfigurationState.Instance.Logger.EnableDebug.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Debug, e.NewValue);
            ConfigurationState.Instance.Logger.EnableStub.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Stub, e.NewValue);
            ConfigurationState.Instance.Logger.EnableInfo.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Info, e.NewValue);
            ConfigurationState.Instance.Logger.EnableWarn.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Warning, e.NewValue);
            ConfigurationState.Instance.Logger.EnableError.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Error, e.NewValue);
            ConfigurationState.Instance.Logger.EnableTrace.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Trace, e.NewValue);
            ConfigurationState.Instance.Logger.EnableGuest.Event += 
                (_, e) => Logger.SetEnable(LogLevel.Guest, e.NewValue);
            ConfigurationState.Instance.Logger.EnableFsAccessLog.Event +=
                (_, e) => Logger.SetEnable(LogLevel.AccessLog, e.NewValue);
            
            ConfigurationState.Instance.Logger.FilteredClasses.Event += (_, e) =>
            {
                bool noFilter = e.NewValue.Length == 0;

                foreach (LogClass logClass in Enum.GetValues<LogClass>())
                {
                    Logger.SetEnable(logClass, noFilter);
                }

                foreach (LogClass logClass in e.NewValue)
                {
                    Logger.SetEnable(logClass, true);
                }
            };

            ConfigurationState.Instance.Logger.EnableFileLog.Event += (_, e) =>
            {
                if (e.NewValue)
                {
                    string logDir = AppDataManager.LogsDirPath;
                    FileStream logFile = null;

                    if (!string.IsNullOrEmpty(logDir))
                    {
                        logFile = FileLogTarget.PrepareLogFile(logDir);
                    }

                    if (logFile == null)
                    {
                        Logger.Error?.Print(LogClass.Application,
                            "No writable log directory available. Make sure either the Logs directory, Application Data, or the Ryujinx directory is writable.");
                        Logger.RemoveTarget("file");

                        return;
                    }

                    Logger.AddTarget(new AsyncLogTargetWrapper(
                        new FileLogTarget("file", logFile),
                        1000
                    ));
                }
                else
                {
                    Logger.RemoveTarget("file");
                }
            };
        }
    }
}
