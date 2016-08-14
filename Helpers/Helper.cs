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
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Data;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using OpenTK;
using OpenTK.Graphics;
using Color = System.Windows.Media.Color;
using PixelFormat = System.Windows.Media.PixelFormat;

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

        public static Func<float, float> ToRad = x => x * (float)Math.PI / 180.0f;
        public static Func<float, float> ToDeg = x => x * 180.0f / (float)Math.PI;

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

        public static bool MouseLeftDown()
        {
            return Mouse.LeftButton == MouseButtonState.Pressed;
        }

        public static bool MouseRightDown()
        {
            return Mouse.RightButton == MouseButtonState.Pressed;
        }

        public static Color SpectrumColor(int w, float alpha)
        {
            float r = 0.0f;
            float g = 0.0f;
            float b = 0.0f;

            w += 2; // Don't start with pink!
            w *= 15;
            w = w % 100;

            if (w < 17)
            {
                r = -(w - 17.0f) / 17.0f;
                b = 1.0f;
            }
            else if (w < 33)
            {
                g = (w - 17.0f) / (33.0f - 17.0f);
                b = 1.0f;
            }
            else if (w < 50)
            {
                g = 1.0f;
                b = -(w - 50.0f) / (50.0f - 33.0f);
            }
            else if (w < 67)
            {
                r = (w - 50.0f) / (67.0f - 50.0f);
                g = 1.0f;
            }
            else if (w < 83)
            {
                r = 1.0f;
                g = -(w - 83.0f) / (83.0f - 67.0f);
            }
            else
            {
                r = 1.0f;
                b = (w - 83.0f) / (100.0f - 83.0f);
            }

            return Color.FromScRgb(alpha, r, g, b);
        }

        public static Vector4 ColorToVector(Color color, bool ignoreAlpha = false)
        {
            return new Vector4(color.ScR, color.ScG, color.ScB, ignoreAlpha ? 1.0f : color.ScA);
        }

        public static Vector2 Reciprocal(Vector2 v)
        {
            return new Vector2(1f / v.X, 1f / v.Y);
        }

        public static Vector3 Reciprocal(Vector3 v)
        {
            return new Vector3(1f / v.X, 1f / v.Y, 1f / v.Z);
        }

        public static Vector4 Reciprocal(Vector4 v)
        {
            return new Vector4(1f / v.X, 1f / v.Y, 1f / v.Z, 1f / v.W);
        }

        public static Vector2 Reciprocal(int2 v)
        {
            return new Vector2(1f / v.X, 1f / v.Y);
        }

        public static Vector3 Reciprocal(int3 v)
        {
            return new Vector3(1f / v.X, 1f / v.Y, 1f / v.Z);
        }

        public static float[] Extract(float[] volume, int3 dimsvolume, int3 centerextract, int3 dimsextract)
        {
            int3 Origin = new int3(centerextract.X - dimsextract.X / 2,
                                   centerextract.Y - dimsextract.Y / 2,
                                   centerextract.Z - dimsextract.Z / 2);

            float[] Extracted = new float[dimsextract.Elements()];

            unsafe
            {
                fixed (float* volumePtr = volume)
                fixed (float* ExtractedPtr = Extracted)
                for (int z = 0; z < dimsextract.Z; z++)
                    for (int y = 0; y < dimsextract.Y; y++)
                        for (int x = 0; x < dimsextract.X; x++)
                        {
                            int3 Pos = new int3((Origin.X + x + dimsvolume.X) % dimsvolume.X,
                                                (Origin.Y + y + dimsvolume.Y) % dimsvolume.Y,
                                                (Origin.Z + z + dimsvolume.Z) % dimsvolume.Z);

                            float Val = volumePtr[(Pos.Z * dimsvolume.Y + Pos.Y) * dimsvolume.X + Pos.X];
                            ExtractedPtr[(z * dimsextract.Y + y) * dimsextract.X + x] = Val;
                        }
            }

            return Extracted;
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
            long Elements = header.Dimensions.Elements();

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
                            for (long i = 0; i < Elements; i++)
                                *BytesP++ = (byte)*DataP++;
                        }
                        else if (ValueType == typeof(short))
                        {
                            short* BytesP = (short*)BytesPtr;
                            for (long i = 0; i < Elements; i++)
                                *BytesP++ = (short)*DataP++;
                        }
                        else if (ValueType == typeof(ushort))
                        {
                            ushort* BytesP = (ushort*)BytesPtr;
                            for (long i = 0; i < Elements; i++)
                                *BytesP++ = (ushort)Math.Min(Math.Max(0f, *DataP++ * 16f), 65536f);
                        }
                        else if (ValueType == typeof(int))
                        {
                            int* BytesP = (int*)BytesPtr;
                            for (long i = 0; i < Elements; i++)
                                *BytesP++ = (int)*DataP++;
                        }
                        else if (ValueType == typeof(float))
                        {
                            float* BytesP = (float*)BytesPtr;
                            for (long i = 0; i < Elements; i++)
                                *BytesP++ = *DataP++;
                        }
                        else if (ValueType == typeof(double))
                        {
                            double* BytesP = (double*)BytesPtr;
                            for (long i = 0; i < Elements; i++)
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
        public static float[] ToFloatArray(OpenTK.Matrix3 m)
        {
            return new float[]{ m.M11, m.M12, m.M13,
                                m.M21, m.M22, m.M23,
                                m.M31, m.M32, m.M33 };
        }
    }
}