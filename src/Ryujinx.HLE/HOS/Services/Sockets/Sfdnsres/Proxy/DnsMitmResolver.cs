using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Nsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Net;

namespace Ryujinx.HLE.HOS.Services.Sockets.Sfdnsres.Proxy
{
    class DnsMitmResolver
    {
        private const string HostsFilePath = "/atmosphere/hosts/default.txt";

        private static DnsMitmResolver _instance;
        public static DnsMitmResolver Instance => _instance ??= new DnsMitmResolver();

        private readonly Dictionary<string, IPAddress> _mitmHostEntries = new();

        public void ReloadEntries(ServiceCtx context)
        {
            string sdPath = FileSystem.VirtualFileSystem.GetSdCardPath();
            string filePath = FileSystem.VirtualFileSystem.GetFullPath(sdPath, HostsFilePath);

            _mitmHostEntries.Clear();

            if (File.Exists(filePath))
            {
                using FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                using StreamReader reader = new(fileStream);

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    // Ignore comments and empty lines
                    if (line.StartsWith('#') || line.Trim().Length == 0)
                    {
                        continue;
                    }

                    string[] entry = line.Split([' ', '\t'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    // Hosts file example entry:
                    // 127.0.0.1  localhost loopback

                    // 0. Check the size of the array
                    if (entry.Length < 2)
                    {
                        Logger.Warning?.PrintMsg(LogClass.ServiceBsd, $"Invalid entry in hosts file: {line}");

                        continue;
                    }

                    // 1. Parse the address
                    if (!IPAddress.TryParse(entry[0], out IPAddress address))
                    {
                        Logger.Warning?.PrintMsg(LogClass.ServiceBsd, $"Failed to parse IP address in hosts file: {entry[0]}");

                        continue;
                    }

                    // 2. Check for AMS hosts file extension: "%"
                    for (int i = 1; i < entry.Length; i++)
                    {
                        entry[i] = entry[i].Replace("%", IManager.NsdSettings.Environment);
                    }

                    // 3. Add hostname to entry dictionary (updating duplicate entries)
                    foreach (string hostname in entry[1..])
                    {
                        _mitmHostEntries[hostname] = address;
                    }
                }
            }
        }

        public IPHostEntry ResolveAddress(string host)
        {
            foreach (KeyValuePair<string, IPAddress> hostEntry in _mitmHostEntries)
            {
                // Check for AMS hosts file extension: "*"
                // NOTE: MatchesSimpleExpression also allows "?" as a wildcard
                if (FileSystemName.MatchesSimpleExpression(hostEntry.Key, host))
                {
                    Logger.Info?.PrintMsg(LogClass.ServiceBsd, $"Redirecting '{host}' to: {hostEntry.Value}");

                    return new IPHostEntry
                    {
                        AddressList = [hostEntry.Value],
                        HostName = hostEntry.Key,
                        Aliases = [],
                    };
                }
            }

            // No match has been found, resolve the host using regular dns
            return Dns.GetHostEntry(host);
        }
    }
}
