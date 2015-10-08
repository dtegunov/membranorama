using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Windows.Threading;
using System.Globalization;
using System.Windows;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Membranogram
{
    static class Helper
    {
        public static IFormatProvider NativeFormat = CultureInfo.InvariantCulture.NumberFormat;
        public static IFormatProvider NativeDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
        public static float ParseFloat(string value)
        {
            return float.Parse(value, NativeFormat);
        }
        public static double ParseDouble(string value)
        {
            return double.Parse(value, NativeFormat);
        }
        public static int ParseInt(string value)
        {
            return int.Parse(value, NativeFormat);
        }
        public static Int64 ParseInt64(string value)
        {
            return Int64.Parse(value, NativeFormat);
        }
        public static decimal ParseDecimal(string value)
        {
            return decimal.Parse(value, NativeFormat);
        }
        public static DateTime ParseDateTime(string value)
        {
            return DateTime.Parse(value, NativeDateTimeFormat);
        }

        public static float ToRad = (float)Math.PI / 180.0f;
        public static float ToDeg = 180.0f / (float)Math.PI;

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static bool CtrlDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        public static bool ShiftDown()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        public static bool AltDown()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct int3
    {
        public int X, Y, Z;

        public int3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int3(byte[] value)
        {
            X = BitConverter.ToInt32(value, 0);
            Y = BitConverter.ToInt32(value, sizeof(float));
            Z = BitConverter.ToInt32(value, 2 * sizeof(float));
        }

        public ulong Elements()
        {
            return (ulong)X * (ulong)Y * (ulong)Z;
        }

        public uint ElementN(int3 position)
        {
            return ((uint)position.Z * (uint)Y + (uint)position.Y) * (uint)X + (uint)position.X;
        }

        public ulong ElementNLong(int3 position)
        {
            return ((ulong)position.Z * (ulong)Y + (ulong)position.Y) * (ulong)X + (ulong)position.X;
        }

        public static implicit operator byte[](int3 value)
        {
            byte[] Bytes = new byte[3 * sizeof(int)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(int));
            Array.Copy(BitConverter.GetBytes(value.Z), 0, Bytes, 2 * sizeof(int), sizeof(int));

            return Bytes;
        }

        public override bool Equals(Object obj)
        {
            return obj is int3 && this == (int3)obj;
        }

        public static bool operator ==(int3 o1, int3 o2)
        {
            return o1.X == o2.X && o1.Y == o2.Y && o1.Z == o2.Z;
        }

        public static bool operator !=(int3 o1, int3 o2)
        {
            return !(o1 == o2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct int2
    {
        public int X, Y;

        public int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int2(byte[] value)
        {
            X = BitConverter.ToInt32(value, 0);
            Y = BitConverter.ToInt32(value, sizeof(float));
        }

        public ulong Elements()
        {
            return (ulong)X * (ulong)Y;
        }

        public uint ElementN(int2 position)
        {
            return (uint)position.Y * (uint)X + (uint)position.X;
        }

        public ulong ElementNLong(int2 position)
        {
            return (ulong)position.Y * (ulong)X + (ulong)position.X;
        }

        public static implicit operator byte[](int2 value)
        {
            byte[] Bytes = new byte[2 * sizeof(int)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(int));

            return Bytes;
        }

        public override bool Equals(Object obj)
        {
            return obj is int2 && this == (int2)obj;
        }

        public static bool operator ==(int2 o1, int2 o2)
        {
            return o1.X == o2.X && o1.Y == o2.Y;
        }

        public static bool operator !=(int2 o1, int2 o2)
        {
            return !(o1 == o2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct float3
    {
        public float X, Y, Z;

        public float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float3(byte[] value)
        {
            X = BitConverter.ToSingle(value, 0);
            Y = BitConverter.ToSingle(value, sizeof(float));
            Z = BitConverter.ToSingle(value, 2 * sizeof(float));
        }

        public static implicit operator byte[](float3 value)
        {
            byte[] Bytes = new byte[3 * sizeof(float)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Z), 0, Bytes, 2 * sizeof(int), sizeof(float));

            return Bytes;
        }

        public override bool Equals(Object obj)
        {
            return obj is float3 && this == (float3)obj;
        }

        public static bool operator ==(float3 o1, float3 o2)
        {
            return o1.X == o2.X && o1.Y == o2.Y && o1.Z == o2.Z;
        }

        public static bool operator !=(float3 o1, float3 o2)
        {
            return !(o1 == o2);
        }
    }

    public static class IOHelper
    {
        public static int3 GetMapDimensions(string path)
        {
            int3 Dims = new int3(1, 1, 1);
            FileInfo Info = new FileInfo(path);

            using (BinaryReader Reader = new BinaryReader(File.OpenRead(path)))
            {
                if (Info.Extension.ToLower() == ".mrc" || Info.Extension.ToLower() == ".mrcs")
                {
                    HeaderMRC Header = new HeaderMRC(Reader);
                    Dims = Header.Dimensions;
                }
                else if (Info.Extension.ToLower() == ".em")
                {
                    HeaderEM Header = new HeaderEM(Reader);
                    Dims = Header.Dimensions;
                }
                else
                    throw new Exception("Format not supported.");
            }

            return Dims;
        }

        public static float[] ReadMapFloat(string path)
        {
            MapHeader Header = null;
            Type ValueType = null;
            byte[] Bytes = null;
            FileInfo Info = new FileInfo(path);

            using (BinaryReader Reader = new BinaryReader(File.OpenRead(path)))
            {
                Header = MapHeader.ReadFromFile(Reader, Info);
                ValueType = Header.GetValueType();

                if (ValueType == typeof(byte))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(byte));
                else if (ValueType == typeof(short))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(short));
                else if (ValueType == typeof(ushort))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(ushort));
                else if (ValueType == typeof(int))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(int));
                else if (ValueType == typeof(float))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(float));
                else if (ValueType == typeof(double))
                    Bytes = Reader.ReadBytes((int)Header.Dimensions.Elements() * sizeof(double));
            }

            float[] Data = new float[Header.Dimensions.Elements()];

            unsafe
            {
                fixed(byte* BytesPtr = Bytes)
                fixed(float* DataPtr = Data)
                {
                    float* DataP = DataPtr;

                    if (ValueType == typeof(byte))
                    {
                        byte* BytesP = BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = (float)*BytesP++;
                    }
                    else if (ValueType == typeof(short))
                    {
                        short* BytesP = (short*)BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = (float)*BytesP++;
                    }
                    else if (ValueType == typeof(ushort))
                    {
                        ushort* BytesP = (ushort*)BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = (float)*BytesP++;
                    }
                    else if (ValueType == typeof(int))
                    {
                        int* BytesP = (int*)BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = (float)*BytesP++;
                    }
                    else if (ValueType == typeof(float))
                    {
                        float* BytesP = (float*)BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = *BytesP++;
                    }
                    else if (ValueType == typeof(double))
                    {
                        double* BytesP = (double*)BytesPtr;
                        for (int i = 0; i < Data.Length; i++)
                            *DataP++ = (float)*BytesP++;
                    }
                }
            }

            return Data;
        }

        public static void WriteMapFloat(string path, MapHeader header, float[] data)
        {
            Type ValueType = header.GetValueType();
            ulong Elements = header.Dimensions.Elements();

            using(BinaryWriter Writer = new BinaryWriter(File.Create(path)))
            {
                header.Write(Writer);
                byte[] Bytes = null;

                if (ValueType == typeof(byte))
                    Bytes = new byte[Elements * sizeof(byte)];
                else if (ValueType == typeof(short))
                    Bytes = new byte[Elements * sizeof(short)];
                else if (ValueType == typeof(ushort))
                    Bytes = new byte[Elements * sizeof(ushort)];
                else if (ValueType == typeof(int))
                    Bytes = new byte[Elements * sizeof(int)];
                else if (ValueType == typeof(float))
                    Bytes = new byte[Elements * sizeof(float)];
                else if (ValueType == typeof(double))
                    Bytes = new byte[Elements * sizeof(double)];

                unsafe
                {
                    fixed(float* DataPtr = data)
                    fixed(byte* BytesPtr = Bytes)
                    {
                        float* DataP = DataPtr;

                        if (ValueType == typeof(byte))
                        {
                            byte* BytesP = BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = (byte)*DataP++;
                        }
                        else if (ValueType == typeof(short))
                        {
                            short* BytesP = (short*)BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = (short)*DataP++;
                        }
                        else if (ValueType == typeof(ushort))
                        {
                            ushort* BytesP = (ushort*)BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = (ushort)Math.Min(Math.Max(0f, *DataP++ * 16f), 65536f);
                        }
                        else if (ValueType == typeof(int))
                        {
                            int* BytesP = (int*)BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = (int)*DataP++;
                        }
                        else if (ValueType == typeof(float))
                        {
                            float* BytesP = (float*)BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = *DataP++;
                        }
                        else if (ValueType == typeof(double))
                        {
                            double* BytesP = (double*)BytesPtr;
                            for (ulong i = 0; i < Elements; i++)
                                *BytesP++ = (double)*DataP++;
                        }
                    }
                }

                Writer.Write(Bytes);
            }
        }
    }

    public static class OpenGLHelper
    { 
        public static float[] ToFloatArray(OpenTK.Matrix4 m)
        {
            return new float[]{ m.M11, m.M12, m.M13, m.M14,
                                m.M21, m.M22, m.M23, m.M24,
                                m.M31, m.M32, m.M33, m.M34,
                                m.M41, m.M42, m.M43, m.M44 };
        }
    }
}