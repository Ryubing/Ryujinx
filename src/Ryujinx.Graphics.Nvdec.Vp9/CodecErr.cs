namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal enum CodecErr
    {
        /*!\brief Operation completed without error */
        Ok,

        /*!\brief Unspecified error */
        Error,

        /*!\brief Memory operation failed */
        MemError,

        /*!\brief ABI version mismatch */
        AbiMismatch,

        /*!\brief Algorithm does not have required capability */
        Incapable,

        /*!\brief The given bitstream is not supported.
         *
         * The bitstream was unable to be parsed at the highest level. The decoder
         * is unable to proceed. This error \ref SHOULD be treated as fatal to the
         * stream. */
        UnsupBitstream,

        /*!\brief Encoded bitstream uses an unsupported feature
         *
         * The decoder does not implement a feature required by the encoder. This
         * return code should only be used for features that prevent future
         * pictures from being properly decoded. This error \ref MAY be treated as
         * fatal to the stream or \ref MAY be treated as fatal to the current GOP.
         */
        UnsupFeature,

        /*!\brief The coded data for this stream is corrupt or incomplete
         *
         * There was a problem decoding the current frame.  This return code
         * should only be used for failures that prevent future pictures from
         * being properly decoded. This error \ref MAY be treated as fatal to the
         * stream or \ref MAY be treated as fatal to the current GOP. If decoding
         * is continued for the current GOP, artifacts may be present.
         */
        CorruptFrame,

        /*!\brief An application-supplied parameter is not valid.
         *
         */
        InvalidParam,

        /*!\brief An iterator reached the end of list.
         *
         */
        ListEnd
    }
}