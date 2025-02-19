using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Account.Acc.AsyncContext;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Services.Account.Acc.AccountService
{
    class ManagerServer
    {
        // TODO: Determine where and how NetworkServiceAccountId is set.
        private const long NetworkServiceAccountId = 0xcafe;

#pragma warning disable IDE0052 // Remove unread private member
        private readonly UserId _userId;
#pragma warning restore IDE0052

        private byte[] _cachedTokenData;
        private DateTime _cachedTokenExpiry;

        public ManagerServer(UserId userId)
        {
            _userId = userId;
        }

        private static string GenerateIdToken()
        {
            using RSA provider = RSA.Create(2048);

            RSAParameters parameters = provider.ExportParameters(true);

            RsaSecurityKey secKey = new(parameters);

            SigningCredentials credentials = new(secKey, "RS256");

            credentials.Key.KeyId = parameters.ToString();

            byte[] rawUserId = new byte[0x10];
            RandomNumberGenerator.Fill(rawUserId);

            byte[] deviceId = new byte[0x10];
            RandomNumberGenerator.Fill(deviceId);

            byte[] deviceAccountId = new byte[0x10];
            RandomNumberGenerator.Fill(deviceId);

            SecurityTokenDescriptor descriptor = new()
            {
                Subject = new GenericIdentity(Convert.ToHexString(rawUserId).ToLower()),
                SigningCredentials = credentials,
                Audience = "ed9e2f05d286f7b8",
                Issuer = "https://e0d67c509fb203858ebcb2fe3f88c2aa.baas.nintendo.com",
                TokenType = "id_token",
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow + TimeSpan.FromHours(3),
                Claims = new Dictionary<string, object>
                {
                    { "jku", "https://e0d67c509fb203858ebcb2fe3f88c2aa.baas.nintendo.com/1.0.0/certificates" },
                    { "di", Convert.ToHexString(deviceId).ToLower() },
                    { "sn", "XAW10000000000" },
                    { "bs:did", Convert.ToHexString(deviceAccountId).ToLower() }
                }
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        public ResultCode CheckAvailability(ServiceCtx context)
        {
            // NOTE: This opens the file at "su/baas/USERID_IN_UUID_STRING.dat" where USERID_IN_UUID_STRING is formatted as "%08x-%04x-%04x-%02x%02x-%08x%04x".
            //       Then it searches the Availability of Online Services related to the UserId in this file and returns it.

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            // NOTE: Even if we try to return different error codes here, the guest still needs other calls.
            return ResultCode.Success;
        }

        public ResultCode GetAccountId(ServiceCtx context)
        {
            // NOTE: This opens the file at "su/baas/USERID_IN_UUID_STRING.dat" (where USERID_IN_UUID_STRING is formatted
            //       as "%08x-%04x-%04x-%02x%02x-%08x%04x") in the account:/ savedata.
            //       Then it searches the NetworkServiceAccountId related to the UserId in this file and returns it.

            Logger.Stub?.PrintStub(LogClass.ServiceAcc, new { NetworkServiceAccountId });

            context.ResponseData.Write(NetworkServiceAccountId);

            return ResultCode.Success;
        }

        public ResultCode EnsureIdTokenCacheAsync(ServiceCtx context, out IAsyncContext asyncContext)
        {
            KEvent asyncEvent = new(context.Device.System.KernelContext);
            AsyncExecution asyncExecution = new(asyncEvent);

            asyncExecution.Initialize(1000, EnsureIdTokenCacheAsyncImpl);

            asyncContext = new IAsyncContext(asyncExecution);

            // return ResultCode.NullObject if the IAsyncContext pointer is null. Doesn't occur in our case.

            return ResultCode.Success;
        }

        private async Task EnsureIdTokenCacheAsyncImpl(CancellationToken token)
        {
            // NOTE: This open the file at "su/baas/USERID_IN_UUID_STRING.dat" (where USERID_IN_UUID_STRING is formatted as "%08x-%04x-%04x-%02x%02x-%08x%04x")
            //       in the "account:/" savedata.
            //       Then its read data, use dauth API with this data to get the Token Id and probably store the dauth response
            //       in "su/cache/USERID_IN_UUID_STRING.dat" (where USERID_IN_UUID_STRING is formatted as "%08x-%04x-%04x-%02x%02x-%08x%04x") in the "account:/" savedata.
            //       Since we don't support online services, we can stub it.

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            // TODO: Use a real function instead, with the CancellationToken.
            await Task.CompletedTask;
        }

        public ResultCode LoadIdTokenCache(ServiceCtx context)
        {
            ulong bufferPosition = context.Request.ReceiveBuff[0].Position;
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong bufferSize = context.Request.ReceiveBuff[0].Size;
#pragma warning restore IDE0059

            // NOTE: This opens the file at "su/cache/USERID_IN_UUID_STRING.dat" (where USERID_IN_UUID_STRING is formatted as "%08x-%04x-%04x-%02x%02x-%08x%04x")
            //       in the "account:/" savedata and writes some data in the buffer.
            //       Since we don't support online services, we can stub it.

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            /*
            if (internal_object != null)
            {
                if (bufferSize > 0xC00)
                {
                    return ResultCode.InvalidIdTokenCacheBufferSize;
                }
            }
            */

            if (_cachedTokenData == null || DateTime.UtcNow > _cachedTokenExpiry)
            {
                _cachedTokenExpiry = DateTime.UtcNow + TimeSpan.FromHours(3);
                _cachedTokenData = Encoding.ASCII.GetBytes(GenerateIdToken());
            }

            byte[] tokenData = _cachedTokenData;

            context.Memory.Write(bufferPosition, tokenData);
            context.ResponseData.Write(tokenData.Length);

            return ResultCode.Success;
        }

        public ResultCode GetNintendoAccountUserResourceCacheForApplication(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAcc, new { NetworkServiceAccountId });

            context.ResponseData.Write(NetworkServiceAccountId);

            // TODO: determine and fill the output IPC buffer.

            return ResultCode.Success;
        }

        public ResultCode StoreOpenContext(ServiceCtx context)
        {
            context.Device.System.AccountManager.StoreOpenedUsers();

            return ResultCode.Success;
        }

        public ResultCode LoadNetworkServiceLicenseKindAsync(ServiceCtx context, out IAsyncNetworkServiceLicenseKindContext asyncContext)
        {
            KEvent asyncEvent = new(context.Device.System.KernelContext);
            AsyncExecution asyncExecution = new(asyncEvent);

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            // NOTE: This is an extension of the data retrieved from the id token cache.
            asyncExecution.Initialize(1000, EnsureIdTokenCacheAsyncImpl);

            asyncContext = new IAsyncNetworkServiceLicenseKindContext(asyncExecution, NetworkServiceLicenseKind.Subscribed);

            // return ResultCode.NullObject if the IAsyncNetworkServiceLicenseKindContext pointer is null. Doesn't occur in our case.

            return ResultCode.Success;
        }
    }
}
