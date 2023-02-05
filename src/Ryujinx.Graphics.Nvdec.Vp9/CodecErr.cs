namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal enum CodecErr
    {
        /// <summary>
        /// Operation completed without error
        /// </summary>
        Ok,

        /// <summary>
        /// Unspecified error
        /// </summary>
        Error,

        /// <summary>
        /// Memory operation failed
        /// </summary>
        MemError,

        /// <summary>
        /// ABI version mismatch
        /// </summary>
        AbiMismatch,

        /// <summary>
        /// Algorithm does not have required capability
        /// </summary>
        Incapable,

        /// <summary>
        /// The given bitstream is not supported.
        /// </summary>
        /// <remarks>
        /// The bitstream was unable to be parsed at the highest level.<br/>
        /// The decoder is unable to proceed.<br/>
        /// This error SHOULD be treated as fatal to the stream.
        /// </remarks>
        UnsupBitstream,

        /// <summary>
        /// Encoded bitstream uses an unsupported feature
        /// </summary>
        /// <remarks>
        /// The decoder does not implement a feature required by the encoder.<br/>
        /// This return code should only be used for features that prevent future
        /// pictures from being properly decoded.<br/>
        /// <br/>
        /// This error MAY be treated as fatal to the stream or MAY be treated as fatal to the current GOP.
        /// </remarks>
        UnsupFeature,

         /// <summary>
         /// The coded data for this stream is corrupt or incomplete.
         /// </summary>
         /// <remarks>
         /// There was a problem decoding the current frame.<br/>
         /// This return code should only be used
         /// for failures that prevent future pictures from being properly decoded.<br/>
         /// <br/>
         /// This error MAY be treated as fatal to the stream or MAY be treated as fatal to the current GOP.<br/>
         /// If decoding is continued for the current GOP, artifacts may be present.
         /// </remarks>
         CorruptFrame,

        /// <summary>
        /// An application-supplied parameter is not valid.
        /// </summary>
        InvalidParam,

        /// <summary>
        /// An iterator reached the end of list.
        /// </summary>
        ListEnd
    }
}