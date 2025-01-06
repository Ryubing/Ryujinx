using Ryujinx.HLE.HOS.Services.Mii.Types;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp;
using Ryujinx.HLE.HOS.Services.Settings;
using System;

namespace Ryujinx.HLE.HOS.Services.Mii.StaticService
{
    class DatabaseServiceImpl : IDatabaseService
    {
        private readonly DatabaseImpl _database;
        private readonly DatabaseSessionMetadata _metadata;
        private readonly bool _isSystem;

        public DatabaseServiceImpl(DatabaseImpl database, bool isSystem, SpecialMiiKeyCode miiKeyCode)
        {
            _database = database;
            _metadata = _database.CreateSessionMetadata(miiKeyCode);
            _isSystem = isSystem;
        }

        public bool IsDatabaseTestModeEnabled()
        {
            if (NxSettings.Settings.TryGetValue("mii!is_db_test_mode_enabled", out object isDatabaseTestModeEnabled))
            {
                return (bool)isDatabaseTestModeEnabled;
            }

            return false;
        }

        protected override bool IsUpdated(SourceFlag flag)
        {
            return _database.IsUpdated(_metadata, flag);
        }

        protected override bool IsFullDatabase()
        {
            return _database.IsFullDatabase();
        }

        protected override uint GetCount(SourceFlag flag)
        {
            return _database.GetCount(_metadata, flag);
        }

        protected override ResultCode Get(SourceFlag flag, out int count, Span<CharInfoElement> elements)
        {
            return _database.Get(_metadata, flag, out count, elements);
        }

        protected override ResultCode Get1(SourceFlag flag, out int count, Span<CharInfo> elements)
        {
            return _database.Get(_metadata, flag, out count, elements);
        }

        protected override ResultCode UpdateLatest(CharInfo oldCharInfo, SourceFlag flag, out CharInfo newCharInfo)
        {
            newCharInfo = default;

            return _database.UpdateLatest(_metadata, oldCharInfo, flag, newCharInfo);
        }

        protected override ResultCode BuildRandom(Age age, Gender gender, Race race, out CharInfo charInfo)
        {
            if (age > Age.All || gender > Gender.All || race > Race.All)
            {
                charInfo = default;

                return ResultCode.InvalidArgument;
            }

            _database.BuildRandom(age, gender, race, out charInfo);

            return ResultCode.Success;
        }

        protected override ResultCode BuildDefault(uint index, out CharInfo charInfo)
        {
            if (index >= DefaultMii.TableLength)
            {
                charInfo = default;

                return ResultCode.InvalidArgument;
            }

            _database.BuildDefault(index, out charInfo);

            return ResultCode.Success;
        }

        protected override ResultCode Get2(SourceFlag flag, out int count, Span<StoreDataElement> elements)
        {
            if (!_isSystem)
            {
                count = -1;

                return ResultCode.PermissionDenied;
            }

            return _database.Get(_metadata, flag, out count, elements);
        }

        protected override ResultCode Get3(SourceFlag flag, out int count, Span<StoreData> elements)
        {
            if (!_isSystem)
            {
                count = -1;

                return ResultCode.PermissionDenied;
            }

            return _database.Get(_metadata, flag, out count, elements);
        }

        protected override ResultCode UpdateLatest1(StoreData oldStoreData, SourceFlag flag, out StoreData newStoreData)
        {
            newStoreData = default;

            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.UpdateLatest(_metadata, oldStoreData, flag, newStoreData);
        }

        protected override ResultCode FindIndex(CreateId createId, bool isSpecial, out int index)
        {
            if (!_isSystem)
            {
                index = -1;

                return ResultCode.PermissionDenied;
            }

            index = _database.FindIndex(createId, isSpecial);

            return ResultCode.Success;
        }

        protected override ResultCode Move(CreateId createId, int newIndex)
        {
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            if (newIndex > 0 && _database.GetCount(_metadata, SourceFlag.Database) > newIndex)
            {
                return _database.Move(_metadata, newIndex, createId);
            }

            return ResultCode.InvalidArgument;
        }

        protected override ResultCode AddOrReplace(StoreData storeData)
        {
            if (VirtualAmiibo.VirtualAmiiboBinFile != null)
            {
                storeData = VirtualAmiibo.VirtualAmiiboBinFile.StoreData;
            }
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.AddOrReplace(_metadata, storeData);
        }

        protected override ResultCode Delete(CreateId createId)
        {
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.Delete(_metadata, createId);
        }

        protected override ResultCode DestroyFile()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            return _database.DestroyFile(_metadata);
        }

        protected override ResultCode DeleteFile()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            return _database.DeleteFile();
        }

        protected override ResultCode Format()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            _database.Format(_metadata);

            return ResultCode.Success;
        }

        protected override ResultCode Import(ReadOnlySpan<byte> data)
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            throw new NotImplementedException();
        }

        protected override ResultCode Export(Span<byte> data)
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            throw new NotImplementedException();
        }

        protected override ResultCode IsBrokenDatabaseWithClearFlag(out bool isBrokenDatabase)
        {
            if (!_isSystem)
            {
                isBrokenDatabase = false;

                return ResultCode.PermissionDenied;
            }

            isBrokenDatabase = _database.IsBrokenDatabaseWithClearFlag();

            return ResultCode.Success;
        }

        protected override ResultCode GetIndex(CharInfo charInfo, out int index)
        {
            return _database.GetIndex(_metadata, charInfo, out index);
        }

        protected override void SetInterfaceVersion(uint interfaceVersion)
        {
            _database.SetInterfaceVersion(_metadata, interfaceVersion);
        }

        protected override ResultCode Convert(Ver3StoreData ver3StoreData, out CharInfo charInfo)
        {
            throw new NotImplementedException();
        }

        protected override ResultCode ConvertCoreDataToCharInfo(CoreData coreData, out CharInfo charInfo)
        {
            return _database.ConvertCoreDataToCharInfo(coreData, out charInfo);
        }

        protected override ResultCode ConvertCharInfoToCoreData(CharInfo charInfo, out CoreData coreData)
        {
            return _database.ConvertCharInfoToCoreData(charInfo, out coreData);
        }
    }
}
