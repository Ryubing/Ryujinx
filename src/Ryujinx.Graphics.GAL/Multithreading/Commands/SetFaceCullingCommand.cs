namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetFaceCullingCommand : IGALCommand, IGALCommand<SetFaceCullingCommand>
    {
        public readonly CommandType CommandType => CommandType.SetFaceCulling;
        private Face _face;

        public void Set(Face face)
        {
            _face = face;
        }

        public static void Run(ref SetFaceCullingCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetFaceCulling(command._face);
        }
    }
}
