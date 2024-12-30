namespace Ryujinx.Common
{
    public class BitTricks
    {
        // Never actually written bit packing logic before, so I looked it up.
        // This code is from https://gist.github.com/Alan-FGR/04938e93e2bffdf5802ceb218a37c195
        
        public static ulong PackBitFields(uint[] values, byte[] bitFields)
        {
            ulong retVal = values[0]; //we set the first value right away
            for (int f = 1; f < values.Length; f++)
            {
                retVal <<= bitFields[f]; // we shift the previous value
                retVal += values[f];// and add our current value
            }
            return retVal;
        }

        public static uint[] UnpackBitFields(ulong packed, byte[] bitFields)
        {
            int fields = bitFields.Length - 1; // number of fields to unpack
            uint[] retArr = new uint[fields + 1]; // init return array
            int curPos = 0; // current field bit position (start)
            int lastEnd; // position where last field ended
            for (int f = fields; f >= 0; f--) // loop from last
            {
                lastEnd = curPos; // we store where the last value ended
                curPos += bitFields[f]; // we get where the current value starts
                int leftShift = 64 - curPos; // we figure how much left shift we gotta apply for the other numbers to overflow into oblivion
                retArr[f] = (uint)((packed << leftShift) >> leftShift + lastEnd); // we do magic
            }
            return retArr;
        }
    }
}
