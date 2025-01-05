using Ryujinx.HLE.HOS.Services.Account.Acc;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ryujinx.HLE.HOS.Applets.Browser
{
    class BrowserArgument
    {
        public WebArgTLVType Type { get; }
        public byte[] Value { get; }

        public BrowserArgument(WebArgTLVType type, byte[] value)
        {
            Type = type;
            Value = value;
        }

        private static readonly Dictionary<WebArgTLVType, Type> _typeRegistry = new()
        {
            { WebArgTLVType.InitialURL,                     typeof(string) },
            { WebArgTLVType.CallbackUrl,                    typeof(string) },
            { WebArgTLVType.CallbackableUrl,                typeof(string) },
            { WebArgTLVType.ApplicationId,                  typeof(ulong) },
            { WebArgTLVType.DocumentPath,                   typeof(string) },
            { WebArgTLVType.DocumentKind,                   typeof(DocumentKind) },
            { WebArgTLVType.SystemDataId,                   typeof(ulong) },
            { WebArgTLVType.Whitelist,                      typeof(string) },
            { WebArgTLVType.NewsFlag,                       typeof(bool) },
            { WebArgTLVType.UserID,                         typeof(UserId) },
            { WebArgTLVType.ScreenShotEnabled,              typeof(bool) },
            { WebArgTLVType.EcClientCertEnabled,            typeof(bool) },
            { WebArgTLVType.UnknownFlag0x14,                typeof(bool) },
            { WebArgTLVType.UnknownFlag0x15,                typeof(bool) },
            { WebArgTLVType.PlayReportEnabled,              typeof(bool) },
            { WebArgTLVType.BootDisplayKind,                typeof(BootDisplayKind) },
            { WebArgTLVType.FooterEnabled,                  typeof(bool) },
            { WebArgTLVType.PointerEnabled,                 typeof(bool) },
            { WebArgTLVType.LeftStickMode,                  typeof(LeftStickMode) },
            { WebArgTLVType.KeyRepeatFrame1,                typeof(int) },
            { WebArgTLVType.KeyRepeatFrame2,                typeof(int) },
            { WebArgTLVType.BootAsMediaPlayerInverted,      typeof(bool) },
            { WebArgTLVType.DisplayUrlKind,                 typeof(bool) },
            { WebArgTLVType.BootAsMediaPlayer,              typeof(bool) },
            { WebArgTLVType.ShopJumpEnabled,                typeof(bool) },
            { WebArgTLVType.MediaAutoPlayEnabled,           typeof(bool) },
            { WebArgTLVType.LobbyParameter,                 typeof(string) },
            { WebArgTLVType.JsExtensionEnabled,             typeof(bool) },
            { WebArgTLVType.AdditionalCommentText,          typeof(string) },
            { WebArgTLVType.TouchEnabledOnContents,         typeof(bool) },
            { WebArgTLVType.UserAgentAdditionalString,      typeof(string) },
            { WebArgTLVType.MediaPlayerAutoCloseEnabled,    typeof(bool) },
            { WebArgTLVType.PageCacheEnabled,               typeof(bool) },
            { WebArgTLVType.WebAudioEnabled,                typeof(bool) },
            { WebArgTLVType.PageFadeEnabled,                typeof(bool) },
            { WebArgTLVType.BootLoadingIconEnabled,         typeof(bool) },
            { WebArgTLVType.PageScrollIndicatorEnabled,     typeof(bool) },
            { WebArgTLVType.MediaPlayerSpeedControlEnabled, typeof(bool) },
            { WebArgTLVType.OverrideWebAudioVolume,         typeof(float) },
            { WebArgTLVType.OverrideMediaAudioVolume,       typeof(float) },
            { WebArgTLVType.MediaPlayerUiEnabled,           typeof(bool) },
        };

        public static (ShimKind, List<BrowserArgument>) ParseArguments(ReadOnlySpan<byte> data)
        {
            List<BrowserArgument> browserArguments = [];

            WebArgHeader header = IApplet.ReadStruct<WebArgHeader>(data[..8]);

            ReadOnlySpan<byte> rawTLVs = data[8..];

            for (int i = 0; i < header.Count; i++)
            {
                WebArgTLV tlv = IApplet.ReadStruct<WebArgTLV>(rawTLVs);
                ReadOnlySpan<byte> tlvData = rawTLVs.Slice(Unsafe.SizeOf<WebArgTLV>(), tlv.Size);

                browserArguments.Add(new BrowserArgument((WebArgTLVType)tlv.Type, tlvData.ToArray()));

                rawTLVs = rawTLVs[(Unsafe.SizeOf<WebArgTLV>() + tlv.Size)..];
            }

            return (header.ShimKind, browserArguments);
        }

        public object GetValue()
        {
            if (_typeRegistry.TryGetValue(Type, out Type valueType))
            {
                if (valueType == typeof(string))
                {
                    return Encoding.UTF8.GetString(Value);
                }
                else if (valueType == typeof(bool))
                {
                    return Value[0] == 1;
                }
                else if (valueType == typeof(uint))
                {
                    return BitConverter.ToUInt32(Value);
                }
                else if (valueType == typeof(int))
                {
                    return BitConverter.ToInt32(Value);
                }
                else if (valueType == typeof(ulong))
                {
                    return BitConverter.ToUInt64(Value);
                }
                else if (valueType == typeof(long))
                {
                    return BitConverter.ToInt64(Value);
                }
                else if (valueType == typeof(float))
                {
                    return BitConverter.ToSingle(Value);
                }
                else if (valueType == typeof(UserId))
                {
                    return new UserId(Value);
                }
                else if (valueType.IsEnum)
                {
                    return Enum.ToObject(valueType, BitConverter.ToInt32(Value));
                }

                return $"{valueType.Name} parsing not implemented";
            }

            return $"Unknown value format (raw length: {Value.Length})";
        }
    }
}
