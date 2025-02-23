namespace Ryujinx.Graphics.GAL
{
    public struct ShaderInfo
    {
        public int FragmentOutputMap { get; }
        public ResourceLayout ResourceLayout { get; }
        public ProgramPipelineState? State { get; }
        public bool FromCache { get; set; }

        public ShaderInfo(int fragmentOutputMap, ResourceLayout resourceLayout, ProgramPipelineState state, bool fromCache = false)
        {
            FragmentOutputMap = fragmentOutputMap;
            ResourceLayout = resourceLayout;
            State = state;
            FromCache = fromCache;
        }

        public ShaderInfo(int fragmentOutputMap, ResourceLayout resourceLayout, bool fromCache = false)
        {
            FragmentOutputMap = fragmentOutputMap;
            ResourceLayout = resourceLayout;
            State = null;
            FromCache = fromCache;
        }
    }
}
