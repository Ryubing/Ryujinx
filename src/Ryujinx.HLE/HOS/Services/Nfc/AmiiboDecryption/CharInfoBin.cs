using Humanizer;
using Ryujinx.HLE.HOS.Services.Mii;
using Ryujinx.HLE.HOS.Services.Mii.Types;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption
{
    internal class CharInfoBin
    {
        public byte MiiVersion { get; private set; }
        public bool AllowCopying { get; private set; }
        public bool ProfanityFlag { get; private set; }
        public int RegionLock { get; private set; }
        public int CharacterSet { get; private set; }
        public int PageIndex { get; private set; }
        public int SlotIndex { get; private set; }
        public int DeviceOrigin { get; private set; }
        public ulong SystemId { get; private set; }
        public uint MiiId { get; private set; }
        public string CreatorMac { get; private set; }
        public bool IsMale { get; private set; }
        public int BirthdayMonth { get; private set; }
        public int BirthdayDay { get; private set; }
        public int FavoriteColor { get; private set; }
        public bool FavoriteMii { get; private set; }
        public string MiiName { get; private set; }
        public string AuthorName { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool DisableSharing { get; private set; }
        public int FaceShape { get; private set; }
        public int SkinColor { get; private set; }
        public int Wrinkles { get; private set; }
        public int Makeup { get; private set; }
        public int HairStyle { get; private set; }
        public int HairColor { get; private set; }
        public bool FlipHair { get; private set; }
        public int EyeStyle { get; private set; }
        public int EyeColor { get; private set; }
        public int EyeScale { get; private set; }
        public int EyeYScale { get; private set; }
        public int EyeRotation { get; private set; }
        public int EyeXSpacing { get; private set; }
        public int EyeYPosition { get; private set; }
        public int EyebrowStyle { get; private set; }
        public int EyebrowColor { get; private set; }
        public int EyebrowScale { get; private set; }
        public int EyebrowYScale { get; private set; }
        public int EyebrowRotation { get; private set; }
        public int EyebrowXSpacing { get; private set; }
        public int EyebrowYPosition { get; private set; }
        public int NoseStyle { get; private set; }
        public int NoseScale { get; private set; }
        public int NoseYPosition { get; private set; }
        public int MouthStyle { get; private set; }
        public int MouthColor { get; private set; }
        public int MouthScale { get; private set; }
        public int MouthYScale { get; private set; }
        public int MouthYPosition { get; private set; }
        public int MustacheStyle { get; private set; }
        public int BeardStyle { get; private set; }
        public int BeardColor { get; private set; }
        public int MustacheScale { get; private set; }
        public int MustacheYPosition { get; private set; }
        public int GlassesStyle { get; private set; }
        public int GlassesColor { get; private set; }
        public int GlassesScale { get; private set; }
        public int GlassesYPosition { get; private set; }
        public bool MoleEnabled { get; private set; }
        public int MoleScale { get; private set; }
        public int MoleXPosition { get; private set; }
        public int MoleYPosition { get; private set; }
        public static CharInfoBin Parse(byte[] data)
        {
            if (data.Length < 0x5C)
                throw new ArgumentException("Data too short to parse Mii data.");

            var mii = new CharInfoBin();

            mii.MiiVersion = data[0x0];

            byte flags1 = data[0x1];
            mii.AllowCopying = (flags1 & 0x1) != 0;
            mii.ProfanityFlag = (flags1 & 0x2) != 0;
            mii.RegionLock = (flags1 >> 2) & 0x3;
            mii.CharacterSet = (flags1 >> 4) & 0x3;
            byte position = data[0x2];
            mii.PageIndex = position & 0xF;
            mii.SlotIndex = (position >> 4) & 0xF;

            byte deviceInfo = data[0x3];
            mii.DeviceOrigin = (deviceInfo >> 4) & 0x7;

            mii.SystemId = BitConverter.ToUInt64(data, 0x4);

            mii.MiiId = BitConverter.ToUInt32(data, 0xC);

            mii.CreatorMac = BitConverter.ToString(data, 0x10, 0x6).Replace("-", ":");

            ushort flags2 = BitConverter.ToUInt16(data, 0x18);
            mii.IsMale = (flags2 & 0x1) == 0;
            mii.BirthdayMonth = (flags2 >> 1) & 0xF;
            mii.BirthdayDay = (flags2 >> 5) & 0x1F;
            mii.FavoriteColor = (flags2 >> 10) & 0xF;
            mii.FavoriteMii = (flags2 & 0x4000) != 0;

            mii.MiiName = ParseUtf16String(data, 0x1A, 20);

            ushort dimensions = BitConverter.ToUInt16(data, 0x2E);
            mii.Width = dimensions & 0xFF;
            mii.Height = (dimensions >> 8) & 0xFF;

            byte flags3 = data[0x30];
            mii.DisableSharing = (flags3 & 0x1) != 0;
            mii.FaceShape = (flags3 >> 1) & 0xF;
            mii.SkinColor = (flags3 >> 5) & 0x7;

            byte flags4 = data[0x31];
            mii.Wrinkles = flags4 & 0xF;
            mii.Makeup = (flags4 >> 4) & 0xF;

            mii.HairStyle = data[0x32];

            byte hairInfo = data[0x33];
            mii.HairColor = hairInfo & 0x7;
            mii.FlipHair = (hairInfo & 0x8) != 0;

            uint eyeInfo = BitConverter.ToUInt32(data, 0x34);
            mii.EyeStyle = (int)(eyeInfo & 0x3F);
            mii.EyeColor = (int)((eyeInfo >> 6) & 0x7);
            mii.EyeScale = (int)((eyeInfo >> 9) & 0xF);
            mii.EyeYScale = (int)((eyeInfo >> 13) & 0x7);
            mii.EyeRotation = (int)((eyeInfo >> 16) & 0x1F);
            mii.EyeXSpacing = (int)((eyeInfo >> 21) & 0xF);
            mii.EyeYPosition = (int)((eyeInfo >> 25) & 0x1F);

            uint eyebrowInfo = BitConverter.ToUInt32(data, 0x38);
            mii.EyebrowStyle = (int)(eyebrowInfo & 0x1F);
            mii.EyebrowColor = (int)((eyebrowInfo >> 5) & 0x7);
            mii.EyebrowScale = (int)((eyebrowInfo >> 8) & 0xF);
            mii.EyebrowYScale = (int)((eyebrowInfo >> 12) & 0x7);
            mii.EyebrowRotation = (int)((eyebrowInfo >> 16) & 0xF);
            mii.EyebrowXSpacing = (int)((eyebrowInfo >> 21) & 0xF);
            mii.EyebrowYPosition = (int)((eyebrowInfo >> 25) & 0x1F);

            ushort noseInfo = BitConverter.ToUInt16(data, 0x3C);
            mii.NoseStyle = noseInfo & 0x1F;
            mii.NoseScale = (noseInfo >> 5) & 0xF;
            mii.NoseYPosition = (noseInfo >> 9) & 0x1F;

            ushort mouthInfo = BitConverter.ToUInt16(data, 0x3E);
            mii.MouthStyle = mouthInfo & 0x3F;
            mii.MouthColor = (mouthInfo >> 6) & 0x7;
            mii.MouthScale = (mouthInfo >> 9) & 0xF;
            mii.MouthYScale = (mouthInfo >> 13) & 0x7;

            ushort miscInfo = BitConverter.ToUInt16(data, 0x40);
            mii.MouthYPosition = miscInfo & 0x1F;
            mii.MustacheStyle = (miscInfo >> 5) & 0x7;

            ushort beardInfo = BitConverter.ToUInt16(data, 0x42);
            mii.BeardStyle = beardInfo & 0x7;
            mii.BeardColor = (beardInfo >> 3) & 0x7;

            ushort mustacheInfo = BitConverter.ToUInt16(data, 0x44);
            mii.MustacheScale = mustacheInfo & 0xF;
            mii.MustacheYPosition = (mustacheInfo >> 4) & 0x1F;

            ushort glassesInfo = BitConverter.ToUInt16(data, 0x46);
            mii.GlassesStyle = glassesInfo & 0xF;
            mii.GlassesColor = (glassesInfo >> 4) & 0x7;
            mii.GlassesScale = (glassesInfo >> 7) & 0xF;
            mii.GlassesYPosition = (glassesInfo >> 11) & 0x1F;

            byte moleFlags = data[0x48];
            mii.MoleEnabled = (moleFlags & 0x1) != 0;
            mii.MoleScale = (moleFlags >> 1) & 0xF;

            ushort moleInfo = BitConverter.ToUInt16(data, 0x46);
            mii.MoleEnabled = (moleInfo & 0x1) != 0;
            mii.MoleScale = (moleInfo >> 1) & 0xF; // 4 bits for scale
            mii.MoleXPosition = (moleInfo >> 5) & 0x1F; // 5 bits for X position
            mii.MoleYPosition = (moleInfo >> 10) & 0x1F; // 5 bits for Y position


            return mii;
        }

        private static string ParseUtf16String(byte[] data, int offset, int maxLength)
        {
            int length = 0;
            for (int i = 0; i < maxLength * 2; i += 2)
            {
                if (data[offset + i] == 0 && data[offset + i + 1] == 0)
                    break;
                length++;
            }

            return Encoding.Unicode.GetString(data, offset, length * 2);
        }

        public CharInfo ConvertToCharInfo(UtilityImpl utilImpl, CharInfo Info)
        {
            Info.CreateId = utilImpl.MakeCreateId();
            Info.Nickname = Nickname.FromString(MiiName);
            Info.FavoriteColor = (byte)FavoriteColor;
            Info.Gender = IsMale ? Gender.Male : Gender.Female;
            Info.Height = (byte)Height;
            Info.FacelineType = (FacelineType)FaceShape;
            Info.FacelineColor = (FacelineColor)SkinColor;
            Info.FacelineWrinkle = (FacelineWrinkle)Wrinkles;
            Info.FacelineMake = (FacelineMake)Makeup;
            Info.HairType = (HairType)HairStyle;
            Info.HairColor = (CommonColor)HairColor;
            Info.HairFlip = FlipHair ? HairFlip.Left : HairFlip.Right;
            Info.EyeType = (EyeType)EyeStyle;
            Info.EyeColor = (CommonColor)EyeColor;
            Info.EyeScale = (byte)EyeScale;
            Info.EyeAspect = (byte)EyeYScale;
            Info.EyeRotate = (byte)EyeRotation;
            Info.EyeX = (byte)EyeXSpacing;
            Info.EyeY = (byte)EyeYPosition;
            Info.EyebrowType = (EyebrowType)EyebrowStyle;
            Info.EyebrowColor = (CommonColor)EyebrowColor;
            Info.EyebrowScale = (byte)EyebrowScale;
            Info.EyebrowAspect = (byte)EyebrowYScale;
            Info.EyebrowRotate = (byte)EyebrowRotation;
            Info.EyebrowX = (byte)EyebrowXSpacing;
            Info.EyebrowY = (byte)EyebrowYPosition;
            Info.NoseType = (NoseType)NoseStyle;
            Info.NoseScale = (byte)NoseScale;
            Info.NoseY = (byte)NoseYPosition;
            Info.MouthType = (MouthType)MouthStyle;
            Info.MouthColor = (CommonColor)MouthColor;
            Info.MouthScale = (byte)MouthScale;
            Info.MouthAspect = (byte)MouthYScale;
            Info.MouthY = (byte)MouthYPosition;
            Info.BeardType = (BeardType)BeardStyle;
            Info.BeardColor = (CommonColor)BeardColor;
            Info.MustacheType = (MustacheType)MustacheStyle;
            Info.MustacheScale = (byte)MustacheScale;
            Info.MustacheY = (byte)MustacheYPosition;
            Info.GlassType = (GlassType)GlassesStyle;
            Info.GlassColor = (CommonColor)GlassesColor;
            Info.GlassScale = (byte)GlassesScale;
            Info.GlassY = (byte)GlassesYPosition;
            Info.MoleType = MoleEnabled ? MoleType.OneDot : MoleType.None;
            Info.MoleScale = (byte)MoleScale;
            Info.MoleX = (byte)MoleXPosition;
            Info.MoleY = (byte)MoleYPosition;
            return Info;
        }
    }
}
