using LibHac;
using LibHac.Bcat;
using LibHac.Common;
using LibHac.FsSrv.Impl;
using LibHac.Loader;
using LibHac.Ncm;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Arp;
using System;

namespace Ryujinx.HLE.HOS
{
    public class LibHacHorizonManager
    {
        private LibHac.Horizon Server { get; set; }

        public HorizonClient RyujinxClient { get; private set; }
        public HorizonClient ApplicationClient { get; private set; }
        public HorizonClient AccountClient { get; private set; }
        public HorizonClient AmClient { get; private set; }
        public HorizonClient BcatClient { get; private set; }
        public HorizonClient FsClient { get; private set; }
        public HorizonClient NsClient { get; private set; }
        public HorizonClient PmClient { get; private set; }
        public HorizonClient SdbClient { get; private set; }

        private SharedRef<LibHacIReader> _arpIReader;
        internal LibHacIReader ArpIReader => _arpIReader.Get;

        public LibHacHorizonManager()
        {
            InitializeServer();
        }

        private void InitializeServer()
        {
            Server = new LibHac.Horizon(new HorizonConfiguration());

            RyujinxClient = Server.CreatePrivilegedHorizonClient();
        }

        public void InitializeArpServer()
        {
            _arpIReader.Reset(new LibHacIReader());
            RyujinxClient.Sm.RegisterService(new LibHacArpServiceObject(ref _arpIReader), "arp:r").ThrowIfFailure();
        }

        public void InitializeBcatServer()
        {
            BcatClient = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Bcat, StorageId.BuiltInSystem), BcatFsPermissions);

            _ = new BcatServer(BcatClient);
        }

        public void InitializeFsServer(VirtualFileSystem virtualFileSystem)
        {
            virtualFileSystem.InitializeFsServer(Server, out HorizonClient fsClient);

            FsClient = fsClient;
        }

        public void InitializeSystemClients()
        {
#pragma warning disable IDE0055 // Disable formatting
            PmClient      = Server.CreatePrivilegedHorizonClient();
            AccountClient = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Account, StorageId.BuiltInSystem), AccountFsPermissions);
            AmClient      = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Am,      StorageId.BuiltInSystem), AmFsPermissions);
            NsClient      = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Ns,      StorageId.BuiltInSystem), NsFsPermissions);
            SdbClient     = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Sdb,     StorageId.BuiltInSystem), SdbFacData, SdbFacDescriptor);
#pragma warning restore IDE0055
        }

        public void InitializeApplicationClient(ProgramId programId, in Npdm npdm)
        {
            ApplicationClient = Server.CreateHorizonClient(new ProgramLocation(programId, StorageId.BuiltInUser), npdm.FsAccessControlData, npdm.FsAccessControlDescriptor);
        }

        private static AccessControlBits.Bits AccountFsPermissions => AccessControlBits.Bits.SystemSaveData |
                                                                      AccessControlBits.Bits.GameCard |
                                                                      AccessControlBits.Bits.SaveDataMeta |
                                                                      AccessControlBits.Bits.GetRightsId;

        private static AccessControlBits.Bits AmFsPermissions => AccessControlBits.Bits.SaveDataManagement |
                                                                 AccessControlBits.Bits.CreateSaveData |
                                                                 AccessControlBits.Bits.SystemData;
        private static AccessControlBits.Bits BcatFsPermissions => AccessControlBits.Bits.SystemSaveData;

        private static AccessControlBits.Bits NsFsPermissions => AccessControlBits.Bits.ApplicationInfo |
                                                                 AccessControlBits.Bits.SystemSaveData |
                                                                 AccessControlBits.Bits.GameCard |
                                                                 AccessControlBits.Bits.SaveDataManagement |
                                                                 AccessControlBits.Bits.ContentManager |
                                                                 AccessControlBits.Bits.ImageManager |
                                                                 AccessControlBits.Bits.SystemSaveDataManagement |
                                                                 AccessControlBits.Bits.SystemUpdate |
                                                                 AccessControlBits.Bits.SdCard |
                                                                 AccessControlBits.Bits.FormatSdCard |
                                                                 AccessControlBits.Bits.GetRightsId |
                                                                 AccessControlBits.Bits.RegisterProgramIndexMapInfo |
                                                                 AccessControlBits.Bits.MoveCacheStorage;

        // Sdb has save data access control info so we can't store just its access control bits
        private static ReadOnlySpan<byte> SdbFacData =>
        [
            0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
            0x03, 0x03, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x09, 0x10, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01
        ];

        private static ReadOnlySpan<byte> SdbFacDescriptor =>
        [
            0x01, 0x00, 0x02, 0x00, 0x08, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x09, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
        ];
    }
}
