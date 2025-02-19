using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using Ryujinx.HLE.HOS.SystemState;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.HLE.HOS.Applets.Error
{
    internal partial class ErrorApplet : IApplet
    {
        private const long ErrorMessageBinaryTitleId = 0x0100000000000801;

        private readonly Horizon _horizon;
        private AppletSession _normalSession;
        private CommonArguments _commonArguments;
        private ErrorCommonHeader _errorCommonHeader;
        private byte[] _errorStorage;

        public event EventHandler AppletStateChanged;

        public ErrorApplet(Horizon horizon)
        {
            _horizon = horizon;
        }

        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;
            _commonArguments = IApplet.ReadStruct<CommonArguments>(_normalSession.Pop());

            Logger.Info?.PrintMsg(LogClass.ServiceAm, $"ErrorApplet version: 0x{_commonArguments.AppletVersion:x8}");

            _errorStorage = _normalSession.Pop();
            _errorCommonHeader = IApplet.ReadStruct<ErrorCommonHeader>(_errorStorage);
            _errorStorage = _errorStorage.Skip(Marshal.SizeOf<ErrorCommonHeader>()).ToArray();

            switch (_errorCommonHeader.Type)
            {
                case ErrorType.ErrorCommonArg:
                    {
                        ParseErrorCommonArg();

                        break;
                    }
                case ErrorType.ApplicationErrorArg:
                    {
                        ParseApplicationErrorArg();

                        break;
                    }
                default:
                    throw new NotImplementedException($"ErrorApplet type {_errorCommonHeader.Type} is not implemented.");
            }

            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        private static (uint module, uint description) HexToResultCode(uint resultCode)
        {
            return ((resultCode & 0x1FF) + 2000, (resultCode >> 9) & 0x3FFF);
        }

        private static string SystemLanguageToLanguageKey(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
#pragma warning disable IDE0055 // Disable formatting
                SystemLanguage.Japanese             => "ja",
                SystemLanguage.AmericanEnglish      => "en-US",
                SystemLanguage.French               => "fr",
                SystemLanguage.German               => "de",
                SystemLanguage.Italian              => "it",
                SystemLanguage.Spanish              => "es",
                SystemLanguage.Chinese              => "zh-Hans",
                SystemLanguage.Korean               => "ko",
                SystemLanguage.Dutch                => "nl",
                SystemLanguage.Portuguese           => "pt",
                SystemLanguage.Russian              => "ru",
                SystemLanguage.Taiwanese            => "zh-HansT",
                SystemLanguage.BritishEnglish       => "en-GB",
                SystemLanguage.CanadianFrench       => "fr-CA",
                SystemLanguage.LatinAmericanSpanish => "es-419",
                SystemLanguage.SimplifiedChinese    => "zh-Hans",
                SystemLanguage.TraditionalChinese   => "zh-Hant",
                SystemLanguage.BrazilianPortuguese  => "pt-BR",
                _                                   => "en-US",
#pragma warning restore IDE0055
            };
        }

        private static string CleanText(string value)
        {
            return Patterns.CleanText.Replace(value, string.Empty).Replace("\0", string.Empty);
        }

        private string GetMessageText(uint module, uint description, string key)
        {
            string binaryTitleContentPath = _horizon.ContentManager.GetInstalledContentPath(ErrorMessageBinaryTitleId, StorageId.BuiltInSystem, NcaContentType.Data);

            using LibHac.Fs.IStorage ncaFileStream = new LocalStorage(FileSystem.VirtualFileSystem.SwitchPathToSystemPath(binaryTitleContentPath), FileAccess.Read, FileMode.Open);
            Nca nca = new(_horizon.Device.FileSystem.KeySet, ncaFileStream);
            IFileSystem romfs = nca.OpenFileSystem(NcaSectionType.Data, _horizon.FsIntegrityCheckLevel);
            string languageCode = SystemLanguageToLanguageKey(_horizon.State.DesiredSystemLanguage);
            string filePath = $"/{module}/{description:0000}/{languageCode}_{key}";

            if (romfs.FileExists(filePath))
            {
                using UniqueRef<IFile> binaryFile = new();

                romfs.OpenFile(ref binaryFile.Ref, filePath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                StreamReader reader = new(binaryFile.Get.AsStream(), Encoding.Unicode);

                return CleanText(reader.ReadToEnd());
            }

            return string.Empty;
        }

        private string[] GetButtonsText(uint module, uint description, string key)
        {
            string buttonsText = GetMessageText(module, description, key);

            return (buttonsText == string.Empty) ? null : buttonsText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        }

        private void ParseErrorCommonArg()
        {
            ErrorCommonArg errorCommonArg = IApplet.ReadStruct<ErrorCommonArg>(_errorStorage);

            uint module = errorCommonArg.Module;
            uint description = errorCommonArg.Description;

            if (_errorCommonHeader.MessageFlag == 0)
            {
                (module, description) = HexToResultCode(errorCommonArg.ResultCode);
            }

            string message = GetMessageText(module, description, "DlgMsg");

            if (message == string.Empty)
            {
                message = "An error has occured.\n\nPlease try again later.";
            }

            string[] buttons = GetButtonsText(module, description, "DlgBtn");

            (uint Module, uint Description) errorCodeTuple = (module, uint.Parse(description.ToString("0000")));
            
            bool showDetails = _horizon.Device.UIHandler.DisplayErrorAppletDialog($"Error Code: {module}-{description:0000}", "\n" + message, buttons, errorCodeTuple);
            if (showDetails)
            {
                message = GetMessageText(module, description, "FlvMsg");
                buttons = GetButtonsText(module, description, "FlvBtn");

                _horizon.Device.UIHandler.DisplayErrorAppletDialog($"Details: {module}-{description:0000}", "\n" + message, buttons, errorCodeTuple);
            }
        }

        private void ParseApplicationErrorArg()
        {
            ApplicationErrorArg applicationErrorArg = IApplet.ReadStruct<ApplicationErrorArg>(_errorStorage);

            byte[] messageTextBuffer = new byte[0x800];
            byte[] detailsTextBuffer = new byte[0x800];

            applicationErrorArg.MessageText.AsSpan().CopyTo(messageTextBuffer);
            applicationErrorArg.DetailsText.AsSpan().CopyTo(detailsTextBuffer);

            string messageText = Encoding.ASCII.GetString(messageTextBuffer.TakeWhile(b => !b.Equals(0)).ToArray());
            string detailsText = Encoding.ASCII.GetString(detailsTextBuffer.TakeWhile(b => !b.Equals(0)).ToArray());

            List<string> buttons = [];

            // TODO: Handle the LanguageCode to return the translated "OK" and "Details".

            if (detailsText.Trim() != string.Empty)
            {
                buttons.Add("Details");
            }

            buttons.Add("OK");

            bool showDetails = _horizon.Device.UIHandler.DisplayErrorAppletDialog($"Error Number: {applicationErrorArg.ErrorNumber}", "\n" + messageText, buttons.ToArray());
            if (showDetails)
            {
                buttons.RemoveAt(0);

                _horizon.Device.UIHandler.DisplayErrorAppletDialog($"Error Number: {applicationErrorArg.ErrorNumber} (Details)", "\n" + detailsText, buttons.ToArray());
            }
        }
    }
}
