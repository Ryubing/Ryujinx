using Gommon;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Account.Acc.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    public class AccountSaveDataManager
    {
        private static readonly string _profilesJsonPath = Path.Join(AppDataManager.BaseDirPath, "system", "Profiles.json");

        private static readonly ProfilesJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public UserId LastOpened { get; set; }

        public AccountSaveDataManager(ConcurrentDictionary<string, UserProfile> profiles)
        {
            // TODO: Use 0x8000000000000010 system savedata instead of a JSON file if needed.

            if (File.Exists(_profilesJsonPath))
            {
                try
                {
                    ProfilesJson profilesJson = JsonHelper.DeserializeFromFile(_profilesJsonPath, _serializerContext.ProfilesJson);

                    foreach (var profile in profilesJson.Profiles)
                    {
                        UserProfile addedProfile = new(new UserId(profile.UserId), profile.Name, profile.Image, profile.LastModifiedTimestamp);

                        profiles.AddOrUpdate(profile.UserId, addedProfile, (key, old) => addedProfile);
                    }

                    LastOpened = new UserId(profilesJson.LastOpened);
                }
                catch (Exception ex)
                {
                    Logger.Error?.Print(LogClass.Application, $"Failed to parse {_profilesJsonPath}: {ex.Message} Loading default profile!");

                    LastOpened = AccountManager.DefaultUserId;
                }
            }
            else
            {
                LastOpened = AccountManager.DefaultUserId;
            }
        }

        public static Optional<UserProfile> GetLastUsedUser()
        {
            ProfilesJson profilesJson = JsonHelper.DeserializeFromFile(_profilesJsonPath, _serializerContext.ProfilesJson);

            return profilesJson.Profiles
                .FindFirst(profile => profile.UserId == profilesJson.LastOpened)
                .Convert(profileJson => new UserProfile(new UserId(profileJson.UserId), profileJson.Name,
                    profileJson.Image, profileJson.LastModifiedTimestamp));
        }

        public void Save(ConcurrentDictionary<string, UserProfile> profiles)
        {
            ProfilesJson profilesJson = new()
            {
                Profiles = new List<UserProfileJson>(),
                LastOpened = LastOpened.ToString(),
            };

            foreach (var profile in profiles)
            {
                profilesJson.Profiles.Add(new UserProfileJson()
                {
                    UserId = profile.Value.UserId.ToString(),
                    Name = profile.Value.Name,
                    AccountState = profile.Value.AccountState,
                    OnlinePlayState = profile.Value.OnlinePlayState,
                    LastModifiedTimestamp = profile.Value.LastModifiedTimestamp,
                    Image = profile.Value.Image,
                });
            }

            JsonHelper.SerializeToFile(_profilesJsonPath, profilesJson, _serializerContext.ProfilesJson);
        }
    }
}
