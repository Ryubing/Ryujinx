using Ryujinx.Common.Utilities;
using System;
using System.IO;

namespace Ryujinx.Common.Logging.Targets
{
    public class JsonLogTarget : ILogTarget
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly string _name;

        string ILogTarget.Name { get => _name; }

        public JsonLogTarget(Stream stream, string name)
        {
            _stream = stream;
            _name = name;
        }

        public JsonLogTarget(Stream stream, bool leaveOpen)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public void Log(object sender, LogEventArgs e)
        {
            LogEventArgsJson logEventArgsJson = LogEventArgsJson.FromLogEventArgs(e);
            JsonHelper.SerializeToStream(_stream, logEventArgsJson, LogEventJsonSerializerContext.Default.LogEventArgsJson);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }
    }
}
