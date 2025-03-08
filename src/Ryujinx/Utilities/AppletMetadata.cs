using LibHac.Common;
using LibHac.Ncm;
using LibHac.Ns;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.HLE;
using Ryujinx.HLE.FileSystem;

namespace Ryujinx.Ava.Utilities
{
    public readonly struct AppletMetadata
    {
        private readonly ContentManager _contentManager;

        public string Name { get; }
        public ulong ProgramId { get; }

        public string Version { get; }

        public AppletMetadata(ContentManager contentManager, string name, ulong programId, string version = "1.0.0")
            : this(name, programId, version)
        {
            _contentManager = contentManager;
        }

        public AppletMetadata(string name, ulong programId, string version = "1.0.0")
        {
            Name = name;
            ProgramId = programId;
            Version = version;
        }

        public string GetContentPath(ContentManager contentManager)
            => (contentManager ?? _contentManager)
                ?.GetInstalledContentPath(ProgramId, StorageId.BuiltInSystem, NcaContentType.Program);

        public bool CanStart(ContentManager contentManager, out ApplicationData appData,
            out BlitStruct<ApplicationControlProperty> appControl)
        {
            contentManager ??= _contentManager;
            if (contentManager == null) 
                goto BadData;

            string contentPath = GetContentPath(contentManager);
            if (string.IsNullOrEmpty(contentPath)) 
                goto BadData;

            appData = new() { Name = Name, Id = ProgramId, Path = contentPath };
            appControl = StructHelpers.CreateCustomNacpData(Name, Version);
            return true;
            
        BadData:
            appData = null;
            appControl = new BlitStruct<ApplicationControlProperty>(0);
            return false;
        }

        public bool CanStart(out ApplicationData appData,
            out BlitStruct<ApplicationControlProperty> appControl)
            => CanStart(null, out appData, out appControl);
    }
}
