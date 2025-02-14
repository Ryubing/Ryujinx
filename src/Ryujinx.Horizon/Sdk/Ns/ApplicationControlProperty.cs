using Ryujinx.Common.Memory;
using System;
using System.Text;

namespace Ryujinx.Horizon.Sdk.Ns
{
    public struct ApplicationControlProperty
    {
        public Array16<ApplicationTitle> Title;
        public Array37<byte> Isbn;
        public StartupUserAccountValue StartupUserAccount;
        public UserAccountSwitchLockValue UserAccountSwitchLock;
        public AddOnContentRegistrationTypeValue AddOnContentRegistrationType;
        public AttributeFlagValue AttributeFlag;
        public uint SupportedLanguageFlag;
        public ParentalControlFlagValue ParentalControlFlag;
        public ScreenshotValue Screenshot;
        public VideoCaptureValue VideoCapture;
        public DataLossConfirmationValue DataLossConfirmation;
        public PlayLogPolicyValue PlayLogPolicy;
        public ulong PresenceGroupId;
        public Array32<sbyte> RatingAge;
        public Array16<byte> DisplayVersion;
        public ulong AddOnContentBaseId;
        public ulong SaveDataOwnerId;
        public long UserAccountSaveDataSize;
        public long UserAccountSaveDataJournalSize;
        public long DeviceSaveDataSize;
        public long DeviceSaveDataJournalSize;
        public long BcatDeliveryCacheStorageSize;
        public Array8<byte> ApplicationErrorCodeCategory;
        public Array8<ulong> LocalCommunicationId;
        public LogoTypeValue LogoType;
        public LogoHandlingValue LogoHandling;
        public RuntimeAddOnContentInstallValue RuntimeAddOnContentInstall;
        public RuntimeParameterDeliveryValue RuntimeParameterDelivery;
        public Array2<byte> Reserved30F4;
        public CrashReportValue CrashReport;
        public HdcpValue Hdcp;
        public ulong SeedForPseudoDeviceId;
        public Array65<byte> BcatPassphrase;
        public StartupUserAccountOptionFlagValue StartupUserAccountOption;
        public Array6<byte> ReservedForUserAccountSaveDataOperation;
        public long UserAccountSaveDataSizeMax;
        public long UserAccountSaveDataJournalSizeMax;
        public long DeviceSaveDataSizeMax;
        public long DeviceSaveDataJournalSizeMax;
        public long TemporaryStorageSize;
        public long CacheStorageSize;
        public long CacheStorageJournalSize;
        public long CacheStorageDataAndJournalSizeMax;
        public ushort CacheStorageIndexMax;
        public byte Reserved318A;
        public byte RuntimeUpgrade;
        public uint SupportingLimitedLicenses;
        public Array16<ulong> PlayLogQueryableApplicationId;
        public PlayLogQueryCapabilityValue PlayLogQueryCapability;
        public RepairFlagValue RepairFlag;
        public byte ProgramIndex;
        public RequiredNetworkServiceLicenseOnLaunchValue RequiredNetworkServiceLicenseOnLaunchFlag;
        public Array4<byte> Reserved3214;
        public ApplicationNeighborDetectionClientConfiguration NeighborDetectionClientConfiguration;
        public ApplicationJitConfiguration JitConfiguration;
        public RequiredAddOnContentsSetBinaryDescriptor RequiredAddOnContentsSetBinaryDescriptors;
        public PlayReportPermissionValue PlayReportPermission;
        public CrashScreenshotForProdValue CrashScreenshotForProd;
        public CrashScreenshotForDevValue CrashScreenshotForDev;
        public byte ContentsAvailabilityTransitionPolicy;
        public Array4<byte> Reserved3404;
        public AccessibleLaunchRequiredVersionValue AccessibleLaunchRequiredVersion;
        public ByteArray3000 Reserved3448;

        public readonly string IsbnString => Encoding.UTF8.GetString(Isbn.AsSpan()).TrimEnd('\0');
        public readonly string DisplayVersionString => Encoding.UTF8.GetString(DisplayVersion.AsSpan()).TrimEnd('\0');
        public readonly string ApplicationErrorCodeCategoryString => Encoding.UTF8.GetString(ApplicationErrorCodeCategory.AsSpan()).TrimEnd('\0');
        public readonly string BcatPassphraseString => Encoding.UTF8.GetString(BcatPassphrase.AsSpan()).TrimEnd('\0');

        public struct ApplicationTitle
        {
            public ByteArray512 Name;
            public Array256<byte> Publisher;

            public readonly string NameString => Encoding.UTF8.GetString(Name.AsSpan()).TrimEnd('\0');
            public readonly string PublisherString => Encoding.UTF8.GetString(Publisher.AsSpan()).TrimEnd('\0');
        }

        public struct ApplicationNeighborDetectionClientConfiguration
        {
            public ApplicationNeighborDetectionGroupConfiguration SendGroupConfiguration;
            public Array16<ApplicationNeighborDetectionGroupConfiguration> ReceivableGroupConfigurations;
        }

        public struct ApplicationNeighborDetectionGroupConfiguration
        {
            public ulong GroupId;
            public Array16<byte> Key;
        }

        public struct ApplicationJitConfiguration
        {
            public JitConfigurationFlag Flags;
            public long MemorySize;
        }

        public struct RequiredAddOnContentsSetBinaryDescriptor
        {
            public Array32<ushort> Descriptors;
        }

        public struct AccessibleLaunchRequiredVersionValue
        {
            public Array8<ulong> ApplicationId;
        }

        public enum Language
        {
            AmericanEnglish = 0,
            BritishEnglish = 1,
            Japanese = 2,
            French = 3,
            German = 4,
            LatinAmericanSpanish = 5,
            Spanish = 6,
            Italian = 7,
            Dutch = 8,
            CanadianFrench = 9,
            Portuguese = 10,
            Russian = 11,
            Korean = 12,
            TraditionalChinese = 13,
            SimplifiedChinese = 14,
            BrazilianPortuguese = 15,
        }

        public enum Organization
        {
            CERO = 0,
            GRACGCRB = 1,
            GSRMR = 2,
            ESRB = 3,
            ClassInd = 4,
            USK = 5,
            PEGI = 6,
            PEGIPortugal = 7,
            PEGIBBFC = 8,
            Russian = 9,
            ACB = 10,
            OFLC = 11,
            IARCGeneric = 12,
        }

        public enum StartupUserAccountValue : byte
        {
            None = 0,
            Required = 1,
            RequiredWithNetworkServiceAccountAvailable = 2,
        }

        public enum UserAccountSwitchLockValue : byte
        {
            Disable = 0,
            Enable = 1,
        }

        public enum AddOnContentRegistrationTypeValue : byte
        {
            AllOnLaunch = 0,
            OnDemand = 1,
        }

        [Flags]
        public enum AttributeFlagValue
        {
            None = 0,
            Demo = 1 << 0,
            RetailInteractiveDisplay = 1 << 1,
        }

        public enum ParentalControlFlagValue
        {
            None = 0,
            FreeCommunication = 1,
        }

        public enum ScreenshotValue : byte
        {
            Allow = 0,
            Deny = 1,
        }

        public enum VideoCaptureValue : byte
        {
            Disable = 0,
            Manual = 1,
            Enable = 2,
        }

        public enum DataLossConfirmationValue : byte
        {
            None = 0,
            Required = 1,
        }

        public enum PlayLogPolicyValue : byte
        {
            Open = 0,
            LogOnly = 1,
            None = 2,
            Closed = 3,
            All = Open,
        }

        public enum LogoTypeValue : byte
        {
            LicensedByNintendo = 0,
            DistributedByNintendo = 1,
            Nintendo = 2,
        }

        public enum LogoHandlingValue : byte
        {
            Auto = 0,
            Manual = 1,
        }

        public enum RuntimeAddOnContentInstallValue : byte
        {
            Deny = 0,
            AllowAppend = 1,
            AllowAppendButDontDownloadWhenUsingNetwork = 2,
        }

        public enum RuntimeParameterDeliveryValue : byte
        {
            Always = 0,
            AlwaysIfUserStateMatched = 1,
            OnRestart = 2,
        }

        public enum CrashReportValue : byte
        {
            Deny = 0,
            Allow = 1,
        }

        public enum HdcpValue : byte
        {
            None = 0,
            Required = 1,
        }

        [Flags]
        public enum StartupUserAccountOptionFlagValue : byte
        {
            None = 0,
            IsOptional = 1 << 0,
        }

        public enum PlayLogQueryCapabilityValue : byte
        {
            None = 0,
            WhiteList = 1,
            All = 2,
        }

        [Flags]
        public enum RepairFlagValue : byte
        {
            None = 0,
            SuppressGameCardAccess = 1 << 0,
        }

        [Flags]
        public enum RequiredNetworkServiceLicenseOnLaunchValue : byte
        {
            None = 0,
            Common = 1 << 0,
        }

        [Flags]
        public enum JitConfigurationFlag : ulong
        {
            None = 0,
            Enabled = 1 << 0,
        }

        [Flags]
        public enum PlayReportPermissionValue : byte
        {
            None = 0,
            TargetMarketing = 1 << 0,
        }

        public enum CrashScreenshotForProdValue : byte
        {
            Deny = 0,
            Allow = 1,
        }

        public enum CrashScreenshotForDevValue : byte
        {
            Deny = 0,
            Allow = 1,
        }
    }
}
