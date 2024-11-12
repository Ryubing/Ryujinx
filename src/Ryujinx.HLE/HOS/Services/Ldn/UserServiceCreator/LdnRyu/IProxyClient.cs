namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.LdnRyu
{
    interface IProxyClient
    {
        bool SendAsync(byte[] buffer);
    }
}
