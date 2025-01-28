using Ryujinx.Common.Logging.Targets;
using Ryujinx.Common.SystemInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ryujinx.Common.Logging
{
    public static class Logger
    {
        private static readonly Stopwatch _time;

        private static readonly bool[] _enabledClasses;

        private static readonly List<ILogTarget> _logTargets;

        private static readonly StdErrAdapter _stdErrAdapter;

        public static event EventHandler<LogEventArgs> Updated;

        public readonly struct Log
        {
            private static readonly string _homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            private static readonly string _homeDirRedacted = Path.Combine(Directory.GetParent(_homeDir)!.FullName, "[redacted]");

            internal readonly LogLevel Level;

            internal Log(LogLevel level)
            {
                Level = level;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintMsg(LogClass logClass, string message)
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, string.Empty, message)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, message)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Print(LogClass logClass, string message, object data, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, message), data));
                }
            }

            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintStack(LogClass logClass, string message, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, message), new StackTrace(true)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintStub(LogClass logClass, string message = "", [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, "Stubbed. " + message)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintStub(LogClass logClass, object data, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, "Stubbed."), data));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintStub(LogClass logClass, string message, object data, [CallerMemberName] string caller = "")
            {
                if (_enabledClasses[(int)logClass])
                {
                    Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, FormatMessage(logClass, caller, "Stubbed. " + message), data));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PrintRawMsg(string message)
            {
                Updated?.Invoke(null, new LogEventArgs(Level, _time.Elapsed, Thread.CurrentThread.Name, message));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string FormatMessage(LogClass logClass, string caller, string message)
            {
                message = message.Replace(_homeDir, _homeDirRedacted);

                return $"{logClass} {caller}: {message}";
            }
        }

        public static Log? Debug { get; private set; }
        public static Log? Info { get; private set; }
        public static Log? Warning { get; private set; }
        public static Log? Error { get; private set; }
        public static Log? Guest { get; private set; }
        public static Log? AccessLog { get; private set; }
        public static Log? Stub { get; private set; }
        public static Log? Trace { get; private set; }
        public static Log Notice { get; } // Always enabled

        static Logger()
        {
            _enabledClasses = new bool[Enum.GetNames<LogClass>().Length];

            for (int index = 0; index < _enabledClasses.Length; index++)
            {
                _enabledClasses[index] = true;
            }

            _logTargets = [];

            _time = Stopwatch.StartNew();

            // Logger should log to console by default
            AddTarget(new AsyncLogTargetWrapper(
                new ConsoleLogTarget("console"),
                1000,
                AsyncLogTargetOverflowAction.Discard));

            Notice = new Log(LogLevel.Notice);

            // Enable important log levels before configuration is loaded
            Error = new Log(LogLevel.Error);
            Warning = new Log(LogLevel.Warning);
            Info = new Log(LogLevel.Info);
            Trace = new Log(LogLevel.Trace);

            _stdErrAdapter = new StdErrAdapter();
        }

        public static void RestartTime()
        {
            _time.Restart();
        }

        private static ILogTarget GetTarget(string targetName) 
            => _logTargets.FirstOrDefault(target => target.Name.Equals(targetName));

        public static void AddTarget(ILogTarget target)
        {
            if (_logTargets.Any(t => t.Name == target.Name))
            {
                return;
            }

            _logTargets.Add(target);

            Updated += target.Log;
        }

        public static void RemoveTarget(string target)
        {
            ILogTarget logTarget = GetTarget(target);

            if (logTarget != null)
            {
                Updated -= logTarget.Log;

                _logTargets.Remove(logTarget);

                logTarget.Dispose();
            }
        }

        public static void Shutdown()
        {
            Updated = null;

            _stdErrAdapter.Dispose();

            foreach (ILogTarget target in _logTargets)
            {
                target.Dispose();
            }

            _logTargets.Clear();
        }

        public static IReadOnlyCollection<LogLevel> GetEnabledLevels()
        {
            Log?[] logs = [Debug, Info, Warning, Error, Guest, AccessLog, Stub, Trace];
            List<LogLevel> levels = new(logs.Length);
            foreach (Log? log in logs)
            {
                if (log.HasValue)
                    levels.Add(log.Value.Level);
            }

            return levels;
        }

        public static void SetEnable(LogLevel logLevel, bool enabled)
        {
            switch (logLevel)
            {
#pragma warning disable IDE0055 // Disable formatting
                case LogLevel.Debug     : Debug     = enabled ? new Log(LogLevel.Debug)     : new Log?(); break;
                case LogLevel.Info      : Info      = enabled ? new Log(LogLevel.Info)      : new Log?(); break;
                case LogLevel.Warning   : Warning   = enabled ? new Log(LogLevel.Warning)   : new Log?(); break;
                case LogLevel.Error     : Error     = enabled ? new Log(LogLevel.Error)     : new Log?(); break;
                case LogLevel.Guest     : Guest     = enabled ? new Log(LogLevel.Guest)     : new Log?(); break;
                case LogLevel.AccessLog : AccessLog = enabled ? new Log(LogLevel.AccessLog) : new Log?(); break;
                case LogLevel.Stub      : Stub      = enabled ? new Log(LogLevel.Stub)      : new Log?(); break;
                case LogLevel.Trace     : Trace     = enabled ? new Log(LogLevel.Trace)     : new Log?(); break;
                case LogLevel.Notice    : break;
                default: throw new ArgumentException("Unknown Log Level", nameof(logLevel));
#pragma warning restore IDE0055
            }
        }

        public static void SetEnable(LogClass logClass, bool enabled)
        {
            _enabledClasses[(int)logClass] = enabled;
        }
    }
}
