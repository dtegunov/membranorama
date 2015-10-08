using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Membranogram
{
    public abstract class MapHeader
    {
        public int3 Dimensions = new int3(1, 1, 1);
        public abstract void Write(BinaryWriter writer);

        public abstract Type GetValueType();
        public abstract void SetValueType(Type t);

        public static MapHeader ReadFromFile(string path)
        {
            MapHeader Header = null;
            FileInfo Info = new FileInfo(path);

            using (BinaryReader Reader = new BinaryReader(File.OpenRead(path)))
            {
                Header = ReadFromFile(Reader, Info);
            }

            return Header;
        }

        public static MapHeader ReadFromFile(BinaryReader reader, FileInfo info)
        {
            MapHeader Header = null;

            if (info.Extension.ToLower() == ".mrc" || info.Extension.ToLower() == ".mrcs" || info.Extension.ToLower() == ".rec")
                Header = new HeaderMRC(reader);
            else if (info.Extension.ToLower() == ".em")
                Header = new HeaderEM(reader);
            else
                throw new Exception("File type not supported.");

            return Header;
        }
    }

    public enum MRCDataType
    {
        Byte = 0,
	    Short = 1,
	    Float = 2,
	    ShortComplex = 3,
	    FloatComplex = 4,
	    UnsignedShort = 6,
	    RGB = 16
    }

    class HeaderMRC : MapHeader
    {
        public MRCDataType Mode = MRCDataType.Float;
        public int3 StartSubImage = new int3(0, 0, 0);
        public int3 Griddimensions = new int3(1, 1, 1);
        public float3 Pixelsize = new float3(1f, 1f, 1f);
        public float3 Angles;
        public int3 MapOrder = new int3(1, 2, 3);

        public float MinValue;
        public float MaxValue;
        public float MeanValue;
        public int SpaceGroup;

        public int ExtendedBytes;
        public short CreatorID;

        public byte[] ExtraData1 = new byte[30];

        public short NInt;
        public short NReal;

        public byte[] ExtraData2 = new byte[28];

        public short IDType;
        public short Lens;
        public short ND1;
        public short ND2;
        public short VD1;
        public short VD2;

        public float3 TiltOriginal;
        public float3 TiltCurrent;
        public float3 Origin;

        public byte[] CMap = new byte[] { (byte)'M', (byte)'A', (byte)'P', (byte)' ' };
        public byte[] Stamp = new byte[] { 67, 65, 0, 0 };

        public float StdDevValue;

        public int NumLabels;
        public byte[][] Labels = new byte[10][];

        public byte[] Extended;

        public HeaderMRC()
        {
            for (int i = 0; i < Labels.Length; i++)
                Labels[i] = new byte[80];
        }

        public HeaderMRC(BinaryReader reader)
        {
            Dimensions = new int3(reader.ReadBytes(3 * sizeof(int)));
            Mode = (MRCDataType)reader.ReadInt32();
            StartSubImage = new int3(reader.ReadBytes(3 * sizeof(int)));
            Griddimensions = new int3(reader.ReadBytes(3 * sizeof(int)));
            Pixelsize = new float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Pixelsize = new float3(Pixelsize.X / (float)Dimensions.X, Pixelsize.Y / (float)Dimensions.Y, Pixelsize.Z / (float)Dimensions.Z);
            Angles = new float3(reader.ReadBytes(3 * sizeof(float)));
            MapOrder = new int3(reader.ReadBytes(3 * sizeof(int)));

            MinValue = reader.ReadSingle();
            MaxValue = reader.ReadSingle();
            MeanValue = reader.ReadSingle();
            SpaceGroup = reader.ReadInt32();

            ExtendedBytes = reader.ReadInt32();
            CreatorID = reader.ReadInt16();

            ExtraData1 = reader.ReadBytes(30);

            NInt = reader.ReadInt16();
            NReal = reader.ReadInt16();

            ExtraData2 = reader.ReadBytes(28);

            IDType = reader.ReadInt16();
            Lens = reader.ReadInt16();
            ND1 = reader.ReadInt16();
            ND2 = reader.ReadInt16();
            VD1 = reader.ReadInt16();
            VD2 = reader.ReadInt16();

            TiltOriginal = new float3(reader.ReadBytes(3 * sizeof(float)));
            TiltCurrent = new float3(reader.ReadBytes(3 * sizeof(float)));
            Origin = new float3(reader.ReadBytes(3 * sizeof(float)));

            CMap = reader.ReadBytes(4);
            Stamp = reader.ReadBytes(4);

            StdDevValue = reader.ReadSingle();

            NumLabels = reader.ReadInt32();
            for (int i = 0; i < 10; i++)
                Labels[i] = reader.ReadBytes(80);

            Extended = reader.ReadBytes(ExtendedBytes);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Dimensions);
            writer.Write((int)Mode);
            writer.Write(StartSubImage);
            writer.Write(Griddimensions);
            writer.Write(Pixelsize);
            writer.Write(Angles);
            writer.Write(MapOrder);

            writer.Write(MinValue);
            writer.Write(MaxValue);
            writer.Write(MeanValue);
            writer.Write(SpaceGroup);

            if (Extended != null)
                writer.Write(Extended.Length);
            else
                writer.Write(0);
            writer.Write(CreatorID);

            writer.Write(ExtraData1);

            writer.Write(NInt);
            writer.Write(NReal);

            writer.Write(ExtraData2);

            writer.Write(IDType);
            writer.Write(Lens);
            writer.Write(ND1);
            writer.Write(ND2);
            writer.Write(VD1);
            writer.Write(VD2);

            writer.Write(TiltOriginal);
            writer.Write(TiltCurrent);
            writer.Write(Origin);

            writer.Write(CMap);
            writer.Write(Stamp);

            writer.Write(StdDevValue);

            writer.Write(NumLabels);
            for (int i = 0; i < Labels.Length; i++)
                writer.Write(Labels[i]);

            if (Extended != null)
                writer.Write(Extended);
        }

        public override Type GetValueType()
        {
            switch (Mode)
            {
                case MRCDataType.Byte:
                    return typeof(byte);
                case MRCDataType.Float:
                    return typeof(float);
                case MRCDataType.FloatComplex:
                    return typeof(float);
                case MRCDataType.RGB:
                    return typeof(byte);
                case MRCDataType.Short:
                    return typeof(short);
                case MRCDataType.ShortComplex:
                    return typeof(short);
                case MRCDataType.UnsignedShort:
                    return typeof(ushort);
            }

            throw new Exception("Unknown data type.");
        }

        public override void SetValueType(Type t)
        {
            if (t == typeof(byte))
                Mode = MRCDataType.Byte;
            else if (t == typeof(float))
                Mode = MRCDataType.Float;
            else if (t == typeof(short))
                Mode = MRCDataType.Short;
            else if (t == typeof(ushort))
                Mode = MRCDataType.UnsignedShort;
            else
                throw new Exception("Unknown data type.");
        }
    }

    public enum EMDataType
    {
        Byte = 1,
        Short = 2,
        ShortComplex = 3,
        Long = 4,
        Single = 5,
        SingleComplex = 8,
        Double = 9,
        DoubleComplex = 10
    }

    public class HeaderEM : MapHeader
    {
        public byte MachineCoding = (byte)6;
        public byte OS9;
        public byte Invalid;
        public EMDataType Mode;

        public byte[] Comment = new byte[80];

        public int Voltage = 300000;
        public float Cs = 2.2f;
        public int Aperture;
        public int Magnification = 50000;
        public float CCDMagnification = 1f;
        public float ExposureTime = 1f;
        public float PixelSize = 1f;
        public int EMCode;
        public float CCDPixelsize = 1f;
        public float CCDArea = 1f;
        public int Defocus;
        public int Astigmatism;
        public float AstigmatismAngle;
        public float FocusIncrement;
        public float DQE;
        public float C2Intensity;
        public int SlitWidth;
        public int EnergyOffset;
        public float TiltAngle;
        public float TiltAxis;
        public int NoName1;
        public int NoName2;
        public int NoName3;
        public int2 MarkerPosition;
        public int Resolution;
        public int Density;
        public int Contrast;
        public int NoName4;
        public int3 CenterOfMass;
        public int Height;
        public int NoName5;
        public int DreiStrahlBereich;
        public int AchromaticRing;
        public int Lambda;
        public int DeltaTheta;
        public int NoName6;
        public int NoName7;

	    byte[] UserData = new byte[256];

        public HeaderEM()
        { }

        public HeaderEM(BinaryReader reader)
        {
            MachineCoding = reader.ReadByte();
            OS9 = reader.ReadByte();
            Invalid = reader.ReadByte();
            Mode = (EMDataType)reader.ReadByte();

            Dimensions = new int3(reader.ReadBytes(3 * sizeof(int)));

            Comment = reader.ReadBytes(80);

            Voltage = reader.ReadInt32();
            Cs = (float)reader.ReadInt32() / 1000f;
            Aperture = reader.ReadInt32();
            Magnification = reader.ReadInt32();
            CCDMagnification = (float)reader.ReadInt32() / 1000f;
            ExposureTime = (float)reader.ReadInt32() / 1000f;
            PixelSize = (float)reader.ReadInt32() / 1000f;
            EMCode = reader.ReadInt32();
            CCDPixelsize = (float)reader.ReadInt32() / 1000f;
            CCDArea = (float)reader.ReadInt32() / 1000f;
            Defocus = reader.ReadInt32();
            Astigmatism = reader.ReadInt32();
            AstigmatismAngle = (float)reader.ReadInt32() / 1000f;
            FocusIncrement = (float)reader.ReadInt32() / 1000f;
            DQE = (float)reader.ReadInt32() / 1000f;
            C2Intensity = (float)reader.ReadInt32() / 1000f;
            SlitWidth = reader.ReadInt32();
            EnergyOffset = reader.ReadInt32();
            TiltAngle = (float)reader.ReadInt32() / 1000f;
            TiltAxis = (float)reader.ReadInt32() / 1000f;
            NoName1 = reader.ReadInt32();
            NoName2 = reader.ReadInt32();
            NoName3 = reader.ReadInt32();
            MarkerPosition = new int2(reader.ReadBytes(2 * sizeof(int)));
            Resolution = reader.ReadInt32();
            Density = reader.ReadInt32();
            Contrast = reader.ReadInt32();
            NoName4 = reader.ReadInt32();
            CenterOfMass = new int3(reader.ReadBytes(3 * sizeof(int)));
            Height = reader.ReadInt32();
            NoName5 = reader.ReadInt32();
            DreiStrahlBereich = reader.ReadInt32();
            AchromaticRing = reader.ReadInt32();
            Lambda = reader.ReadInt32();
            DeltaTheta = reader.ReadInt32();
            NoName6 = reader.ReadInt32();
            NoName7 = reader.ReadInt32();

            UserData = reader.ReadBytes(256);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(MachineCoding);
            writer.Write(OS9);
            writer.Write(Invalid);
            writer.Write((byte)Mode);

            writer.Write(Dimensions);

            writer.Write(Comment);

            writer.Write(Voltage);
            writer.Write((int)(Cs * 1000f));
            writer.Write(Aperture);
            writer.Write(Magnification);
            writer.Write((int)(CCDMagnification * 1000f));
            writer.Write((int)(ExposureTime * 1000f));
            writer.Write((int)(PixelSize * 1000f));
            writer.Write(EMCode);
            writer.Write((int)(CCDPixelsize * 1000f));
            writer.Write((int)(CCDArea * 1000f));
            writer.Write(Defocus);
            writer.Write(Astigmatism);
            writer.Write((int)(AstigmatismAngle * 1000f));
            writer.Write((int)(FocusIncrement * 1000f));
            writer.Write((int)(DQE * 1000f));
            writer.Write((int)(C2Intensity * 1000f));
            writer.Write(SlitWidth);
            writer.Write(EnergyOffset);
            writer.Write((int)(TiltAngle * 1000f));
            writer.Write((int)(TiltAxis * 1000f));
            writer.Write(NoName1);
            writer.Write(NoName2);
            writer.Write(NoName3);
            writer.Write(MarkerPosition);
            writer.Write(Resolution);
            writer.Write(Density);
            writer.Write(Contrast);
            writer.Write(NoName4);
            writer.Write(CenterOfMass);
            writer.Write(Height);
            writer.Write(NoName5);
            writer.Write(DreiStrahlBereich);
            writer.Write(AchromaticRing);
            writer.Write(Lambda);
            writer.Write(DeltaTheta);
            writer.Write(NoName6);
            writer.Write(NoName7);

            writer.Write(UserData);
        }

        public override Type GetValueType()
        {
            switch (Mode)
            {
                case EMDataType.Byte:
                    return typeof(byte);
                case EMDataType.Double:
                    return typeof(double);
                case EMDataType.DoubleComplex:
                    return typeof(double);
                case EMDataType.Long:
                    return typeof(int);
                case EMDataType.Short:
                    return typeof(short);
                case EMDataType.ShortComplex:
                    return typeof(short);
                case EMDataType.Single:
                    return typeof(float);
                case EMDataType.SingleComplex:
                    return typeof(float);
            }

            throw new Exception("Unknown data type.");
        }

        public override void SetValueType(Type t)
        {
            if (t == typeof(byte))
                Mode = EMDataType.Byte;
            else if (t == typeof(float))
                Mode = EMDataType.Single;
            else if (t == typeof(double))
                Mode = EMDataType.Double;
            else if (t == typeof(short))
                Mode = EMDataType.Short;
            else if (t == typeof(int))
                Mode = EMDataType.Long;
            else
                throw new Exception("Unknown data type.");
        }
    }

    public class HeaderRaw : MapHeader
    {
        long OffsetBytes;

        public HeaderRaw(long offsetBytes)
        {
            OffsetBytes = offsetBytes;
        }

        public override void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override Type GetValueType()
        {
            throw new NotImplementedException();
        }

        public override void SetValueType(Type t)
        {
            throw new NotImplementedException();
        }
    }

    public enum ImageFormats
    { 
        MRC = 0,
        MRCS = 1,
        EM = 2,
        K2Raw = 3,
        FEIRaw = 4        
    }

    public static class ImageFormatsHelper
    {
        public static ImageFormats Parse(string format)
        {
            switch (format)
            {
                case "MRC":
                    return ImageFormats.MRC;
                case "MRCS":
                    return ImageFormats.MRCS;
                case "EM":
                    return ImageFormats.EM;
                case "K2Raw":
                    return ImageFormats.K2Raw;
                case "FEIRaw":
                    return ImageFormats.FEIRaw;
                default:
                    return ImageFormats.MRC;
            }
        }

        public static string ToString(ImageFormats format)
        { 
            switch (format)
            {
                case ImageFormats.MRC:
                    return "MRC";
                case ImageFormats.MRCS:
                    return "MRCS";
                case ImageFormats.EM:
                    return "EM";
                case ImageFormats.K2Raw:
                    return "K2Raw";
                case ImageFormats.FEIRaw:
                    return "FEIRaw";
                default:
                    return "";
            }
        }

        public static string GetExtension(ImageFormats format)
        {
            switch (format)
            {
                case ImageFormats.MRC:
                    return ".mrc";
                case ImageFormats.MRCS:
                    return ".mrcs";
                case ImageFormats.EM:
                    return ".em";
                case ImageFormats.K2Raw:
                    return ".dat";
                case ImageFormats.FEIRaw:
                    return ".raw";
                default:
                    return "";
            }
        }

        public static MapHeader CreateHeader(ImageFormats format)
        {
            switch (format)
            {
                case ImageFormats.MRC:
                    return new HeaderMRC();
                case ImageFormats.MRCS:
                    return new HeaderMRC();
                case ImageFormats.EM:
                    return new HeaderEM();
                case ImageFormats.K2Raw:
                    return new HeaderRaw(0);
                case ImageFormats.FEIRaw:
                    return new HeaderRaw(49);
                default:
                    return null;
            }
        }
    }
}
