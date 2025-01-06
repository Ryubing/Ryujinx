using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x58)]
    struct CharInfo : IStoredData<CharInfo>
    {
        public CreateId CreateId;
        public Nickname Nickname;
        public FontRegion FontRegion;
        public byte FavoriteColor;
        public Gender Gender;
        public byte Height;
        public byte Build;
        public byte Type;
        public byte RegionMove;
        public FacelineType FacelineType;
        public FacelineColor FacelineColor;
        public FacelineWrinkle FacelineWrinkle;
        public FacelineMake FacelineMake;
        public HairType HairType;
        public CommonColor HairColor;
        public HairFlip HairFlip;
        public EyeType EyeType;
        public CommonColor EyeColor;
        public byte EyeScale;
        public byte EyeAspect;
        public byte EyeRotate;
        public byte EyeX;
        public byte EyeY;
        public EyebrowType EyebrowType;
        public CommonColor EyebrowColor;
        public byte EyebrowScale;
        public byte EyebrowAspect;
        public byte EyebrowRotate;
        public byte EyebrowX;
        public byte EyebrowY;
        public NoseType NoseType;
        public byte NoseScale;
        public byte NoseY;
        public MouthType MouthType;
        public CommonColor MouthColor;
        public byte MouthScale;
        public byte MouthAspect;
        public byte MouthY;
        public CommonColor BeardColor;
        public BeardType BeardType;
        public MustacheType MustacheType;
        public byte MustacheScale;
        public byte MustacheY;
        public GlassType GlassType;
        public CommonColor GlassColor;
        public byte GlassScale;
        public byte GlassY;
        public MoleType MoleType;
        public byte MoleScale;
        public byte MoleX;
        public byte MoleY;
        public byte Reserved;

        readonly byte IStoredData<CharInfo>.Type => Type;

        readonly CreateId IStoredData<CharInfo>.CreateId => CreateId;

        public readonly ResultCode InvalidData => ResultCode.InvalidCharInfo;

        public bool IsValid()
        {
            return Verify() == 0;
        }

        public uint Verify()
        {
            if (!CreateId.IsValid)
            {
                return 50;
            }
            if (!Nickname.IsValid())
            {
                return 51;
            }
            if ((byte)FontRegion > 3)
            {
                return 23;
            }
            if (FavoriteColor > 11)
            {
                return 22;
            }
            if (Gender > Gender.Max)
            {
                return 24;
            }
            if ((sbyte)Height < 0)
            {
                return 32;
            }
            if ((sbyte)Build < 0)
            {
                return 3;
            }
            if (Type > 1)
            {
                return 53;
            }
            if (RegionMove > 3)
            {
                return 49;
            }
            if (FacelineType > FacelineType.Max)
            {
                return 21;
            }
            if (FacelineColor > FacelineColor.Max)
            {
                return 18;
            }
            if (FacelineWrinkle > FacelineWrinkle.Max)
            {
                return 20;
            }
            if (FacelineMake > FacelineMake.Max)
            {
                return 19;
            }
            if (HairType > HairType.Max)
            {
                return 31;
            }
            if (HairColor > CommonColor.Max)
            {
                return 29;
            }
            if (HairFlip > HairFlip.Max)
            {
                return 30;
            }
            if (EyeType > EyeType.Max)
            {
                return 8;
            }
            if (EyeColor > CommonColor.Max)
            {
                return 5;
            }
            if (EyeScale > 7)
            {
                return 7;
            }
            if (EyeAspect > 6)
            {
                return 4;
            }
            if (EyeRotate > 7)
            {
                return 6;
            }
            if (EyeX > 12)
            {
                return 9;
            }
            if (EyeY > 18)
            {
                return 10;
            }
            if (EyebrowType > EyebrowType.Max)
            {
                return 15;
            }
            if (EyebrowColor > CommonColor.Max)
            {
                return 12;
            }
            if (EyebrowScale > 8)
            {
                return 14;
            }
            if (EyebrowAspect > 6)
            {
                return 11;
            }
            if (EyebrowRotate > 11)
            {
                return 13;
            }
            if (EyebrowX > 12)
            {
                return 16;
            }
            if (EyebrowY - 3 > 15)
            {
                return 17;
            }
            if (NoseType > NoseType.Max)
            {
                return 47;
            }
            if (NoseScale > 8)
            {
                return 46;
            }
            if (NoseY > 18)
            {
                return 48;
            }
            if (MouthType > MouthType.Max)
            {
                return 40;
            }
            if (MouthColor > CommonColor.Max)
            {
                return 38;
            }
            if (MouthScale > 8)
            {
                return 39;
            }
            if (MouthAspect > 6)
            {
                return 37;
            }
            if (MouthY > 18)
            {
                return 41;
            }
            if (BeardColor > CommonColor.Max)
            {
                return 1;
            }
            if (BeardType > BeardType.Max)
            {
                return 2;
            }
            if (MustacheType > MustacheType.Max)
            {
                return 43;
            }
            if (MustacheScale > 8)
            {
                return 42;
            }
            if (MustacheY > 16)
            {
                return 44;
            }
            if (GlassType > GlassType.Max)
            {
                return 27;
            }
            if (GlassColor > CommonColor.Max)
            {
                return 25;
            }
            if (GlassScale > 7)
            {
                return 26;
            }
            if (GlassY > 20)
            {
                return 28;
            }
            if (MoleType > MoleType.Max)
            {
                return 34;
            }
            if (MoleScale > 8)
            {
                return 33;
            }
            if (MoleX > 16)
            {
                return 35;
            }
            if (MoleY >= 31)
            {
                return 36;
            }

            return 0;
        }

        public void SetFromStoreData(StoreData storeData)
        {
            Nickname = storeData.CoreData.Nickname;
            CreateId = storeData.CreateId;
            FontRegion = storeData.CoreData.FontRegion;
            FavoriteColor = storeData.CoreData.FavoriteColor;
            Gender = storeData.CoreData.Gender;
            Height = storeData.CoreData.Height;
            Build = storeData.CoreData.Build;
            Type = storeData.CoreData.Type;
            RegionMove = storeData.CoreData.RegionMove;
            FacelineType = storeData.CoreData.FacelineType;
            FacelineColor = storeData.CoreData.FacelineColor;
            FacelineWrinkle = storeData.CoreData.FacelineWrinkle;
            FacelineMake = storeData.CoreData.FacelineMake;
            HairType = storeData.CoreData.HairType;
            HairColor = storeData.CoreData.HairColor;
            HairFlip = storeData.CoreData.HairFlip;
            EyeType = storeData.CoreData.EyeType;
            EyeColor = storeData.CoreData.EyeColor;
            EyeScale = storeData.CoreData.EyeScale;
            EyeAspect = storeData.CoreData.EyeAspect;
            EyeRotate = storeData.CoreData.EyeRotate;
            EyeX = storeData.CoreData.EyeX;
            EyeY = storeData.CoreData.EyeY;
            EyebrowType = storeData.CoreData.EyebrowType;
            EyebrowColor = storeData.CoreData.EyebrowColor;
            EyebrowScale = storeData.CoreData.EyebrowScale;
            EyebrowAspect = storeData.CoreData.EyebrowAspect;
            EyebrowRotate = storeData.CoreData.EyebrowRotate;
            EyebrowX = storeData.CoreData.EyebrowX;
            EyebrowY = storeData.CoreData.EyebrowY;
            NoseType = storeData.CoreData.NoseType;
            NoseScale = storeData.CoreData.NoseScale;
            NoseY = storeData.CoreData.NoseY;
            MouthType = storeData.CoreData.MouthType;
            MouthColor = storeData.CoreData.MouthColor;
            MouthScale = storeData.CoreData.MouthScale;
            MouthAspect = storeData.CoreData.MouthAspect;
            MouthY = storeData.CoreData.MouthY;
            BeardColor = storeData.CoreData.BeardColor;
            BeardType = storeData.CoreData.BeardType;
            MustacheType = storeData.CoreData.MustacheType;
            MustacheScale = storeData.CoreData.MustacheScale;
            MustacheY = storeData.CoreData.MustacheY;
            GlassType = storeData.CoreData.GlassType;
            GlassColor = storeData.CoreData.GlassColor;
            GlassScale = storeData.CoreData.GlassScale;
            GlassY = storeData.CoreData.GlassY;
            MoleType = storeData.CoreData.MoleType;
            MoleScale = storeData.CoreData.MoleScale;
            MoleX = storeData.CoreData.MoleX;
            MoleY = storeData.CoreData.MoleY;
            Reserved = 0;
        }
        public static StoreData BuildFromCharInfo(UtilityImpl utilImpl, CharInfo charInfo)
        {
            StoreData result = new()
            {
                CoreData = new CoreData
                {
                    Nickname = charInfo.Nickname,
                    FontRegion = charInfo.FontRegion,
                    FavoriteColor = charInfo.FavoriteColor,
                    Gender = charInfo.Gender,
                    Height = charInfo.Height,
                    Build = charInfo.Build,
                    Type = charInfo.Type,
                    RegionMove = charInfo.RegionMove,
                    FacelineType = charInfo.FacelineType,
                    FacelineColor = charInfo.FacelineColor,
                    FacelineWrinkle = charInfo.FacelineWrinkle,
                    FacelineMake = charInfo.FacelineMake,
                    HairType = charInfo.HairType,
                    HairColor = charInfo.HairColor,
                    HairFlip = charInfo.HairFlip,
                    EyeType = charInfo.EyeType,
                    EyeColor = charInfo.EyeColor,
                    EyeScale = charInfo.EyeScale,
                    EyeAspect = charInfo.EyeAspect,
                    EyeRotate = charInfo.EyeRotate,
                    EyeX = charInfo.EyeX,
                    EyeY = charInfo.EyeY,
                    EyebrowType = charInfo.EyebrowType,
                    EyebrowColor = charInfo.EyebrowColor,
                    EyebrowScale = charInfo.EyebrowScale,
                    EyebrowAspect = charInfo.EyebrowAspect,
                    EyebrowRotate = charInfo.EyebrowRotate,
                    EyebrowX = charInfo.EyebrowX,
                    EyebrowY = charInfo.EyebrowY,
                    NoseType = charInfo.NoseType,
                    NoseScale = charInfo.NoseScale,
                    NoseY = charInfo.NoseY,
                    MouthType = charInfo.MouthType,
                    MouthColor = charInfo.MouthColor,
                    MouthScale = charInfo.MouthScale,
                    MouthAspect = charInfo.MouthAspect,
                    MouthY = charInfo.MouthY,
                    BeardColor = charInfo.BeardColor,
                    BeardType = charInfo.BeardType,
                    MustacheType = charInfo.MustacheType,
                    MustacheScale = charInfo.MustacheScale,
                    MustacheY = charInfo.MustacheY,
                    GlassType = charInfo.GlassType,
                    GlassColor = charInfo.GlassColor,
                    GlassScale = charInfo.GlassScale,
                    GlassY = charInfo.GlassY,
                    MoleType = charInfo.MoleType,
                    MoleScale = charInfo.MoleScale,
                    MoleX = charInfo.MoleX,
                    MoleY = charInfo.MoleY
                }
            };
            
            result.UpdateCreateID(utilImpl);
            result.UpdateCrc();

            return result;
        }

        public readonly void SetSource(Source source)
        {
            // Only implemented for Element variants.
        }

        public static bool operator ==(CharInfo x, CharInfo y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(CharInfo x, CharInfo y)
        {
            return !x.Equals(y);
        }

        public readonly override bool Equals(object obj)
        {
            return obj is CharInfo charInfo && Equals(charInfo);
        }

        public readonly bool Equals(CharInfo cmpObj)
        {
            if (!cmpObj.IsValid())
            {
                return false;
            }

            bool result = true;

            result &= Nickname == cmpObj.Nickname;
            result &= CreateId == cmpObj.CreateId;
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

        public readonly override int GetHashCode()
        {
            HashCode hashCode = new();

            hashCode.Add(Nickname);
            hashCode.Add(CreateId);
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
    }
}
