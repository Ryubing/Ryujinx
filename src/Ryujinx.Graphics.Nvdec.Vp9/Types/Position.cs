namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Position
    {
        public int Row;
        public int Col;

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }
}