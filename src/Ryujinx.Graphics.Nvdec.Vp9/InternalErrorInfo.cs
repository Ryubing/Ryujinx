namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal struct InternalErrorInfo
    {
        public CodecErr ErrorCode;

        public void InternalError(CodecErr error, string message)
        {
            ErrorCode = error;

            throw new InternalErrorException(message);
        }
    }
}