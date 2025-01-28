using Ryujinx.Common.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.HLE.HOS.Services.Mii.Types.RandomMiiConstants;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = Size)]
    struct CoreData : IEquatable<CoreData>
    {
        public const int Size = 0x30;

        private Array48<byte> _storage;

        public Span<byte> Storage => _storage.AsSpan();

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x18)]
        public struct ElementInfo
        {
            public int ByteOffset;
            public int BitOffset;
            public int BitWidth;
            public int MinValue;
            public int MaxValue;
            public int Unknown;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetValue(ElementInfoIndex index)
        {
            ElementInfo info = ElementInfos[(int)index];

            return ((Storage[info.ByteOffset] >> info.BitOffset) & ~(-1 << info.BitWidth)) + info.MinValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(ElementInfoIndex index, int value)
        {
            ElementInfo info = ElementInfos[(int)index];

            int newValue = Storage[info.ByteOffset] & ~(~(-1 << info.BitWidth) << info.BitOffset) | (((value - info.MinValue) & ~(-1 << info.BitWidth)) << info.BitOffset);

            Storage[info.ByteOffset] = (byte)newValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsElementValid(ElementInfoIndex index)
        {
            ElementInfo info = ElementInfos[(int)index];

            int value = GetValue(index);

            return value >= info.MinValue && value <= info.MaxValue;
        }

        public bool IsValid(bool acceptEmptyNickname = false)
        {
            if (!Nickname.IsValid() || (!acceptEmptyNickname && Nickname.IsEmpty()))
            {
                return false;
            }

            for (int i = 0; i < ElementInfos.Length; i++)
            {
                if (!IsElementValid((ElementInfoIndex)i))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetDefault()
        {
            Storage.Clear();

            Nickname = Nickname.Default;
        }

        public HairType HairType
        {
            get => (HairType)GetValue(ElementInfoIndex.HairType);
            set => SetValue(ElementInfoIndex.HairType, (int)value);
        }

        public byte Height
        {
            get => (byte)GetValue(ElementInfoIndex.Height);
            set => SetValue(ElementInfoIndex.Height, value);
        }

        public MoleType MoleType
        {
            get => (MoleType)GetValue(ElementInfoIndex.MoleType);
            set => SetValue(ElementInfoIndex.MoleType, (byte)value);
        }

        public byte Build
        {
            get => (byte)GetValue(ElementInfoIndex.Build);
            set => SetValue(ElementInfoIndex.Build, value);
        }

        public HairFlip HairFlip
        {
            get => (HairFlip)GetValue(ElementInfoIndex.HairFlip);
            set => SetValue(ElementInfoIndex.HairFlip, (byte)value);
        }

        public CommonColor HairColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.HairColor);
            set => SetValue(ElementInfoIndex.HairColor, (int)value);
        }

        public byte Type
        {
            get => (byte)GetValue(ElementInfoIndex.Type);
            set => SetValue(ElementInfoIndex.Type, value);
        }

        public CommonColor EyeColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.EyeColor);
            set => SetValue(ElementInfoIndex.EyeColor, (int)value);
        }

        public Gender Gender
        {
            get => (Gender)GetValue(ElementInfoIndex.Gender);
            set => SetValue(ElementInfoIndex.Gender, (int)value);
        }

        public CommonColor EyebrowColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.EyebrowColor);
            set => SetValue(ElementInfoIndex.EyebrowColor, (int)value);
        }

        public CommonColor MouthColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.MouthColor);
            set => SetValue(ElementInfoIndex.MouthColor, (int)value);
        }

        public CommonColor BeardColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.BeardColor);
            set => SetValue(ElementInfoIndex.BeardColor, (byte)value);
        }

        public CommonColor GlassColor
        {
            get => (CommonColor)GetValue(ElementInfoIndex.GlassColor);
            set => SetValue(ElementInfoIndex.GlassColor, (int)value);
        }

        public EyeType EyeType
        {
            get => (EyeType)GetValue(ElementInfoIndex.EyeType);
            set => SetValue(ElementInfoIndex.EyeType, (int)value);
        }

        public byte RegionMove
        {
            get => (byte)GetValue(ElementInfoIndex.RegionMove);
            set => SetValue(ElementInfoIndex.RegionMove, value);
        }

        public MouthType MouthType
        {
            get => (MouthType)GetValue(ElementInfoIndex.MouthType);
            set => SetValue(ElementInfoIndex.MouthType, (int)value);
        }

        public FontRegion FontRegion
        {
            get => (FontRegion)GetValue(ElementInfoIndex.FontRegion);
            set => SetValue(ElementInfoIndex.FontRegion, (byte)value);
        }

        public byte EyeY
        {
            get => (byte)GetValue(ElementInfoIndex.EyeY);
            set => SetValue(ElementInfoIndex.EyeY, value);
        }

        public byte GlassScale
        {
            get => (byte)GetValue(ElementInfoIndex.GlassScale);
            set => SetValue(ElementInfoIndex.GlassScale, value);
        }

        public EyebrowType EyebrowType
        {
            get => (EyebrowType)GetValue(ElementInfoIndex.EyebrowType);
            set => SetValue(ElementInfoIndex.EyebrowType, (int)value);
        }

        public MustacheType MustacheType
        {
            get => (MustacheType)GetValue(ElementInfoIndex.MustacheType);
            set => SetValue(ElementInfoIndex.MustacheType, (int)value);
        }

        public NoseType NoseType
        {
            get => (NoseType)GetValue(ElementInfoIndex.NoseType);
            set => SetValue(ElementInfoIndex.NoseType, (int)value);
        }

        public BeardType BeardType
        {
            get => (BeardType)GetValue(ElementInfoIndex.BeardType);
            set => SetValue(ElementInfoIndex.BeardType, (int)value);
        }

        public byte NoseY
        {
            get => (byte)GetValue(ElementInfoIndex.NoseY);
            set => SetValue(ElementInfoIndex.NoseY, value);
        }

        public byte MouthAspect
        {
            get => (byte)GetValue(ElementInfoIndex.MouthAspect);
            set => SetValue(ElementInfoIndex.MouthAspect, value);
        }

        public byte MouthY
        {
            get => (byte)GetValue(ElementInfoIndex.MouthY);
            set => SetValue(ElementInfoIndex.MouthY, value);
        }

        public byte EyebrowAspect
        {
            get => (byte)GetValue(ElementInfoIndex.EyebrowAspect);
            set => SetValue(ElementInfoIndex.EyebrowAspect, value);
        }

        public byte MustacheY
        {
            get => (byte)GetValue(ElementInfoIndex.MustacheY);
            set => SetValue(ElementInfoIndex.MustacheY, value);
        }

        public byte EyeRotate
        {
            get => (byte)GetValue(ElementInfoIndex.EyeRotate);
            set => SetValue(ElementInfoIndex.EyeRotate, value);
        }

        public byte GlassY
        {
            get => (byte)GetValue(ElementInfoIndex.GlassY);
            set => SetValue(ElementInfoIndex.GlassY, value);
        }

        public byte EyeAspect
        {
            get => (byte)GetValue(ElementInfoIndex.EyeAspect);
            set => SetValue(ElementInfoIndex.EyeAspect, value);
        }

        public byte MoleX
        {
            get => (byte)GetValue(ElementInfoIndex.MoleX);
            set => SetValue(ElementInfoIndex.MoleX, value);
        }

        public byte EyeScale
        {
            get => (byte)GetValue(ElementInfoIndex.EyeScale);
            set => SetValue(ElementInfoIndex.EyeScale, value);
        }

        public byte MoleY
        {
            get => (byte)GetValue(ElementInfoIndex.MoleY);
            set => SetValue(ElementInfoIndex.MoleY, value);
        }

        public GlassType GlassType
        {
            get => (GlassType)GetValue(ElementInfoIndex.GlassType);
            set => SetValue(ElementInfoIndex.GlassType, (int)value);
        }

        public byte FavoriteColor
        {
            get => (byte)GetValue(ElementInfoIndex.FavoriteColor);
            set => SetValue(ElementInfoIndex.FavoriteColor, value);
        }

        public FacelineType FacelineType
        {
            get => (FacelineType)GetValue(ElementInfoIndex.FacelineType);
            set => SetValue(ElementInfoIndex.FacelineType, (int)value);
        }

        public FacelineColor FacelineColor
        {
            get => (FacelineColor)GetValue(ElementInfoIndex.FacelineColor);
            set => SetValue(ElementInfoIndex.FacelineColor, (int)value);
        }

        public FacelineWrinkle FacelineWrinkle
        {
            get => (FacelineWrinkle)GetValue(ElementInfoIndex.FacelineWrinkle);
            set => SetValue(ElementInfoIndex.FacelineWrinkle, (int)value);
        }

        public FacelineMake FacelineMake
        {
            get => (FacelineMake)GetValue(ElementInfoIndex.FacelineMake);
            set => SetValue(ElementInfoIndex.FacelineMake, (int)value);
        }

        public byte EyeX
        {
            get => (byte)GetValue(ElementInfoIndex.EyeX);
            set => SetValue(ElementInfoIndex.EyeX, value);
        }

        public byte EyebrowScale
        {
            get => (byte)GetValue(ElementInfoIndex.EyebrowScale);
            set => SetValue(ElementInfoIndex.EyebrowScale, value);
        }

        public byte EyebrowRotate
        {
            get => (byte)GetValue(ElementInfoIndex.EyebrowRotate);
            set => SetValue(ElementInfoIndex.EyebrowRotate, value);
        }

        public byte EyebrowX
        {
            get => (byte)GetValue(ElementInfoIndex.EyebrowX);
            set => SetValue(ElementInfoIndex.EyebrowX, value);
        }

        public byte EyebrowY
        {
            get => (byte)GetValue(ElementInfoIndex.EyebrowY);
            set => SetValue(ElementInfoIndex.EyebrowY, value);
        }

        public byte NoseScale
        {
            get => (byte)GetValue(ElementInfoIndex.NoseScale);
            set => SetValue(ElementInfoIndex.NoseScale, value);
        }

        public byte MouthScale
        {
            get => (byte)GetValue(ElementInfoIndex.MouthScale);
            set => SetValue(ElementInfoIndex.MouthScale, value);
        }

        public byte MustacheScale
        {
            get => (byte)GetValue(ElementInfoIndex.MustacheScale);
            set => SetValue(ElementInfoIndex.MustacheScale, value);
        }

        public byte MoleScale
        {
            get => (byte)GetValue(ElementInfoIndex.MoleScale);
            set => SetValue(ElementInfoIndex.MoleScale, value);
        }

        public Span<byte> GetNicknameStorage()
        {
            return Storage[0x1c..];
        }

        public Nickname Nickname
        {
            get => Nickname.FromBytes(GetNicknameStorage());
            set => value.Raw[..20].CopyTo(GetNicknameStorage());
        }

        public static CoreData BuildRandom(UtilityImpl utilImpl, Age age, Gender gender, Race race)
        {
            CoreData coreData = new();

            coreData.SetDefault();

            if (gender == Gender.All)
            {
                gender = (Gender)utilImpl.GetRandom((int)gender);
            }

            if (age == Age.All)
            {
                int ageDecade = utilImpl.GetRandom(10);

                if (ageDecade >= 8)
                {
                    age = Age.Old;
                }
                else if (ageDecade >= 4)
                {
                    age = Age.Normal;
                }
                else
                {
                    age = Age.Young;
                }
            }

            if (race == Race.All)
            {
                int raceTempValue = utilImpl.GetRandom(10);

                if (raceTempValue >= 8)
                {
                    race = Race.Black;
                }
                else if (raceTempValue >= 4)
                {
                    race = Race.White;
                }
                else
                {
                    race = Race.Asian;
                }
            }

            int axisY = 0;

            if (gender == Gender.Female && age == Age.Young)
            {
                axisY = utilImpl.GetRandom(3);
            }

            int indexFor4 = 3 * (int)age + 9 * (int)gender + (int)race;

            RandomMiiData4 facelineTypeInfo = RandomMiiFacelineArray[indexFor4];
            RandomMiiData3 facelineColorInfo = RandomMiiFacelineColorArray[3 * (int)gender + (int)race];
            RandomMiiData4 facelineWrinkleInfo = RandomMiiFacelineWrinkleArray[indexFor4];
            RandomMiiData4 facelineMakeInfo = RandomMiiFacelineMakeArray[indexFor4];
            RandomMiiData4 hairTypeInfo = RandomMiiHairTypeArray[indexFor4];
            RandomMiiData3 hairColorInfo = RandomMiiHairColorArray[3 * (int)race + (int)age];
            RandomMiiData4 eyeTypeInfo = RandomMiiEyeTypeArray[indexFor4];
            RandomMiiData2 eyeColorInfo = RandomMiiEyeColorArray[(int)race];
            RandomMiiData4 eyebrowTypeInfo = RandomMiiEyebrowTypeArray[indexFor4];
            RandomMiiData4 noseTypeInfo = RandomMiiNoseTypeArray[indexFor4];
            RandomMiiData4 mouthTypeInfo = RandomMiiMouthTypeArray[indexFor4];
            RandomMiiData2 glassTypeInfo = RandomMiiGlassTypeArray[(int)age];

            // Faceline
            coreData.FacelineType = (FacelineType)facelineTypeInfo.Values[utilImpl.GetRandom(facelineTypeInfo.ValuesCount)];
            coreData.FacelineColor = (FacelineColor)Helper.Ver3FacelineColorTable[facelineColorInfo.Values[utilImpl.GetRandom(facelineColorInfo.ValuesCount)]];
            coreData.FacelineWrinkle = (FacelineWrinkle)facelineWrinkleInfo.Values[utilImpl.GetRandom(facelineWrinkleInfo.ValuesCount)];
            coreData.FacelineMake = (FacelineMake)facelineMakeInfo.Values[utilImpl.GetRandom(facelineMakeInfo.ValuesCount)];

            // Hair
            coreData.HairType = (HairType)hairTypeInfo.Values[utilImpl.GetRandom(hairTypeInfo.ValuesCount)];
            coreData.HairColor = (CommonColor)Helper.Ver3HairColorTable[hairColorInfo.Values[utilImpl.GetRandom(hairColorInfo.ValuesCount)]];
            coreData.HairFlip = (HairFlip)utilImpl.GetRandom((int)HairFlip.Max + 1);

            // Eye
            coreData.EyeType = (EyeType)eyeTypeInfo.Values[utilImpl.GetRandom(eyeTypeInfo.ValuesCount)];

            int eyeRotateKey1 = gender != Gender.Male ? 4 : 2;
            int eyeRotateKey2 = gender != Gender.Male ? 3 : 4;

            byte eyeRotateOffset = (byte)(32 - EyeRotateTable[eyeRotateKey1] + eyeRotateKey2);
            byte eyeRotate = (byte)(32 - EyeRotateTable[(int)coreData.EyeType]);

            coreData.EyeColor = (CommonColor)Helper.Ver3EyeColorTable[eyeColorInfo.Values[utilImpl.GetRandom(eyeColorInfo.ValuesCount)]];
            coreData.EyeScale = 4;
            coreData.EyeAspect = 3;
            coreData.EyeRotate = (byte)(eyeRotateOffset - eyeRotate);
            coreData.EyeX = 2;
            coreData.EyeY = (byte)(axisY + 12);

            // Eyebrow
            coreData.EyebrowType = (EyebrowType)eyebrowTypeInfo.Values[utilImpl.GetRandom(eyebrowTypeInfo.ValuesCount)];

            int eyebrowRotateKey = race == Race.Asian ? 6 : 0;
            int eyebrowY = race == Race.Asian ? 9 : 10;

            byte eyebrowRotateOffset = (byte)(32 - EyebrowRotateTable[eyebrowRotateKey] + 6);
            byte eyebrowRotate = (byte)(32 - EyebrowRotateTable[(int)coreData.EyebrowType]);

            coreData.EyebrowColor = coreData.HairColor;
            coreData.EyebrowScale = 4;
            coreData.EyebrowAspect = 3;
            coreData.EyebrowRotate = (byte)(eyebrowRotateOffset - eyebrowRotate);
            coreData.EyebrowX = 2;
            coreData.EyebrowY = (byte)(axisY + eyebrowY);

            // Nose
            int noseScale = gender == Gender.Female ? 3 : 4;

            coreData.NoseType = (NoseType)noseTypeInfo.Values[utilImpl.GetRandom(noseTypeInfo.ValuesCount)];
            coreData.NoseScale = (byte)noseScale;
            coreData.NoseY = (byte)(axisY + 9);

            // Mouth
            int mouthColor = gender == Gender.Female ? utilImpl.GetRandom(0, 4) : 0;

            coreData.MouthType = (MouthType)mouthTypeInfo.Values[utilImpl.GetRandom(mouthTypeInfo.ValuesCount)];
            coreData.MouthColor = (CommonColor)Helper.Ver3MouthColorTable[mouthColor];
            coreData.MouthScale = 4;
            coreData.MouthAspect = 3;
            coreData.MouthY = (byte)(axisY + 13);

            // Beard & Mustache
            coreData.BeardColor = coreData.HairColor;
            coreData.MustacheScale = 4;

            if (gender == Gender.Male && age != Age.Young && utilImpl.GetRandom(10) < 2)
            {
                BeardAndMustacheFlag mustacheAndBeardFlag = (BeardAndMustacheFlag)utilImpl.GetRandom(3);

                BeardType beardType = BeardType.None;
                MustacheType mustacheType = MustacheType.None;

                if ((mustacheAndBeardFlag & BeardAndMustacheFlag.Beard) == BeardAndMustacheFlag.Beard)
                {
                    beardType = (BeardType)utilImpl.GetRandom((int)BeardType.Goatee, (int)BeardType.Full);
                }

                if ((mustacheAndBeardFlag & BeardAndMustacheFlag.Mustache) == BeardAndMustacheFlag.Mustache)
                {
                    mustacheType = (MustacheType)utilImpl.GetRandom((int)MustacheType.Walrus, (int)MustacheType.Toothbrush);
                }

                coreData.MustacheType = mustacheType;
                coreData.BeardType = beardType;
                coreData.MustacheY = 10;
            }
            else
            {
                coreData.MustacheType = MustacheType.None;
                coreData.BeardType = BeardType.None;
                coreData.MustacheY = (byte)(axisY + 10);
            }

            // Glass
            int glassTypeStart = utilImpl.GetRandom(100);
            GlassType glassType = GlassType.None;

            while (glassTypeStart < glassTypeInfo.Values[(int)glassType])
            {
                glassType++;

                if ((int)glassType >= glassTypeInfo.ValuesCount)
                {
                    throw new InvalidOperationException("glassTypeStart shouldn't exceed glassTypeInfo.ValuesCount");
                }
            }

            coreData.GlassType = glassType;
            coreData.GlassColor = (CommonColor)Helper.Ver3GlassColorTable[0];
            coreData.GlassScale = 4;
            coreData.GlassY = (byte)(axisY + 10);

            // Mole
            coreData.MoleType = 0;
            coreData.MoleScale = 4;
            coreData.MoleX = 2;
            coreData.MoleY = 20;

            // Body sizing
            coreData.Height = 64;
            coreData.Build = 64;

            // Misc
            coreData.Nickname = Nickname.Default;
            coreData.Gender = gender;
            coreData.FavoriteColor = (byte)utilImpl.GetRandom(0, 11);
            coreData.RegionMove = 0;
            coreData.FontRegion = 0;
            coreData.Type = 0;

            return coreData;
        }

        public void SetFromCharInfo(CharInfo charInfo)
        {
            Nickname = charInfo.Nickname;
            FontRegion = charInfo.FontRegion;
            FavoriteColor = charInfo.FavoriteColor;
            Gender = charInfo.Gender;
            Height = charInfo.Height;
            Build = charInfo.Build;
            Type = charInfo.Type;
            RegionMove = charInfo.RegionMove;
            FacelineType = charInfo.FacelineType;
            FacelineColor = charInfo.FacelineColor;
            FacelineWrinkle = charInfo.FacelineWrinkle;
            FacelineMake = charInfo.FacelineMake;
            HairType = charInfo.HairType;
            HairColor = charInfo.HairColor;
            HairFlip = charInfo.HairFlip;
            EyeType = charInfo.EyeType;
            EyeColor = charInfo.EyeColor;
            EyeScale = charInfo.EyeScale;
            EyeAspect = charInfo.EyeAspect;
            EyeRotate = charInfo.EyeRotate;
            EyeX = charInfo.EyeX;
            EyeY = charInfo.EyeY;
            EyebrowType = charInfo.EyebrowType;
            EyebrowColor = charInfo.EyebrowColor;
            EyebrowScale = charInfo.EyebrowScale;
            EyebrowAspect = charInfo.EyebrowAspect;
            EyebrowRotate = charInfo.EyebrowRotate;
            EyebrowX = charInfo.EyebrowX;
            EyebrowY = charInfo.EyebrowY;
            NoseType = charInfo.NoseType;
            NoseScale = charInfo.NoseScale;
            NoseY = charInfo.NoseY;
            MouthType = charInfo.MouthType;
            MouthColor = charInfo.MouthColor;
            MouthScale = charInfo.MouthScale;
            MouthAspect = charInfo.MouthAspect;
            MouthY = charInfo.MouthY;
            BeardColor = charInfo.BeardColor;
            BeardType = charInfo.BeardType;
            MustacheType = charInfo.MustacheType;
            MustacheScale = charInfo.MustacheScale;
            MustacheY = charInfo.MustacheY;
            GlassType = charInfo.GlassType;
            GlassColor = charInfo.GlassColor;
            GlassScale = charInfo.GlassScale;
            GlassY = charInfo.GlassY;
            MoleType = charInfo.MoleType;
            MoleScale = charInfo.MoleScale;
            MoleX = charInfo.MoleX;
            MoleY = charInfo.MoleY;
        }

        public static bool operator ==(CoreData x, CoreData y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(CoreData x, CoreData y)
        {
            return !x.Equals(y);
        }

        public override bool Equals(object obj)
        {
            return obj is CoreData coreData && Equals(coreData);
        }

        public bool Equals(CoreData cmpObj)
        {
            if (!cmpObj.IsValid())
            {
                return false;
            }

            bool result = true;

            result &= Nickname == cmpObj.Nickname;
            result &= FontRegion == cmpObj.FontRegion;
            result &= FavoriteColor == cmpObj.FavoriteColor;
            result &= Gender == cmpObj.Gender;
            result &= Height == cmpObj.Height;
            result &= Build == cmpObj.Build;
            result &= Type == cmpObj.Type;
            result &= RegionMove == cmpObj.RegionMove;
            result &= FacelineType == cmpObj.FacelineType;
            result &= FacelineColor == cmpObj.FacelineColor;
            result &= FacelineWrinkle == cmpObj.FacelineWrinkle;
            result &= FacelineMake == cmpObj.FacelineMake;
            result &= HairType == cmpObj.HairType;
            result &= HairColor == cmpObj.HairColor;
            result &= HairFlip == cmpObj.HairFlip;
            result &= EyeType == cmpObj.EyeType;
            result &= EyeColor == cmpObj.EyeColor;
            result &= EyeScale == cmpObj.EyeScale;
            result &= EyeAspect == cmpObj.EyeAspect;
            result &= EyeRotate == cmpObj.EyeRotate;
            result &= EyeX == cmpObj.EyeX;
            result &= EyeY == cmpObj.EyeY;
            result &= EyebrowType == cmpObj.EyebrowType;
            result &= EyebrowColor == cmpObj.EyebrowColor;
            result &= EyebrowScale == cmpObj.EyebrowScale;
            result &= EyebrowAspect == cmpObj.EyebrowAspect;
            result &= EyebrowRotate == cmpObj.EyebrowRotate;
            result &= EyebrowX == cmpObj.EyebrowX;
            result &= EyebrowY == cmpObj.EyebrowY;
            result &= NoseType == cmpObj.NoseType;
            result &= NoseScale == cmpObj.NoseScale;
            result &= NoseY == cmpObj.NoseY;
            result &= MouthType == cmpObj.MouthType;
            result &= MouthColor == cmpObj.MouthColor;
            result &= MouthScale == cmpObj.MouthScale;
            result &= MouthAspect == cmpObj.MouthAspect;
            result &= MouthY == cmpObj.MouthY;
            result &= BeardColor == cmpObj.BeardColor;
            result &= BeardType == cmpObj.BeardType;
            result &= MustacheType == cmpObj.MustacheType;
            result &= MustacheScale == cmpObj.MustacheScale;
            result &= MustacheY == cmpObj.MustacheY;
            result &= GlassType == cmpObj.GlassType;
            result &= GlassColor == cmpObj.GlassColor;
            result &= GlassScale == cmpObj.GlassScale;
            result &= GlassY == cmpObj.GlassY;
            result &= MoleType == cmpObj.MoleType;
            result &= MoleScale == cmpObj.MoleScale;
            result &= MoleX == cmpObj.MoleX;
            result &= MoleY == cmpObj.MoleY;

            return result;
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new();

            hashCode.Add(Nickname);
            hashCode.Add(FontRegion);
            hashCode.Add(FavoriteColor);
            hashCode.Add(Gender);
            hashCode.Add(Height);
            hashCode.Add(Build);
            hashCode.Add(Type);
            hashCode.Add(RegionMove);
            hashCode.Add(FacelineType);
            hashCode.Add(FacelineColor);
            hashCode.Add(FacelineWrinkle);
            hashCode.Add(FacelineMake);
            hashCode.Add(HairType);
            hashCode.Add(HairColor);
            hashCode.Add(HairFlip);
            hashCode.Add(EyeType);
            hashCode.Add(EyeColor);
            hashCode.Add(EyeScale);
            hashCode.Add(EyeAspect);
            hashCode.Add(EyeRotate);
            hashCode.Add(EyeX);
            hashCode.Add(EyeY);
            hashCode.Add(EyebrowType);
            hashCode.Add(EyebrowColor);
            hashCode.Add(EyebrowScale);
            hashCode.Add(EyebrowAspect);
            hashCode.Add(EyebrowRotate);
            hashCode.Add(EyebrowX);
            hashCode.Add(EyebrowY);
            hashCode.Add(NoseType);
            hashCode.Add(NoseScale);
            hashCode.Add(NoseY);
            hashCode.Add(MouthType);
            hashCode.Add(MouthColor);
            hashCode.Add(MouthScale);
            hashCode.Add(MouthAspect);
            hashCode.Add(MouthY);
            hashCode.Add(BeardColor);
            hashCode.Add(BeardType);
            hashCode.Add(MustacheType);
            hashCode.Add(MustacheScale);
            hashCode.Add(MustacheY);
            hashCode.Add(GlassType);
            hashCode.Add(GlassColor);
            hashCode.Add(GlassScale);
            hashCode.Add(GlassY);
            hashCode.Add(MoleType);
            hashCode.Add(MoleScale);
            hashCode.Add(MoleX);
            hashCode.Add(MoleY);

            return hashCode.ToHashCode();
        }

        private readonly ReadOnlySpan<ElementInfo> ElementInfos => MemoryMarshal.Cast<byte, ElementInfo>(ElementInfoArray);

        private enum ElementInfoIndex
        {
            HairType,
            Height,
            MoleType,
            Build,
            HairFlip,
            HairColor,
            Type,
            EyeColor,
            Gender,
            EyebrowColor,
            MouthColor,
            BeardColor,
            GlassColor,
            EyeType,
            RegionMove,
            MouthType,
            FontRegion,
            EyeY,
            GlassScale,
            EyebrowType,
            MustacheType,
            NoseType,
            BeardType,
            NoseY,
            MouthAspect,
            MouthY,
            EyebrowAspect,
            MustacheY,
            EyeRotate,
            GlassY,
            EyeAspect,
            MoleX,
            EyeScale,
            MoleY,
            GlassType,
            FavoriteColor,
            FacelineType,
            FacelineColor,
            FacelineWrinkle,
            FacelineMake,
            EyeX,
            EyebrowScale,
            EyebrowRotate,
            EyebrowX,
            EyebrowY,
            NoseScale,
            MouthScale,
            MustacheScale,
            MoleScale,
        }

        #region "Element Info Array"
        private readonly ReadOnlySpan<byte> ElementInfoArray =>
        [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x83, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x09, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x23, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0a, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0b, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0c, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0d, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0e, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x0f, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x11, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x12, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x13, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1e, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x13, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x15, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x16, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x17, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x17, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0c, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x18, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0b, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x19, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x1a, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x1b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x1b, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00
        ];
        #endregion
    }
}
