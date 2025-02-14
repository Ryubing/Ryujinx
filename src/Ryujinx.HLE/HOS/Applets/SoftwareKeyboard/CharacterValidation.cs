using Ryujinx.Common.Helper;

namespace Ryujinx.HLE.HOS.Applets.SoftwareKeyboard
{
    public static class CharacterValidation
    {
        public static bool IsNumeric(char value) => Patterns.Numeric.IsMatch(value.ToString());
        public static bool IsCJK(char value) => Patterns.CJK.IsMatch(value.ToString());
    }
}
