using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    [StructLayout(LayoutKind.Sequential)]
    public struct float4
    {
        public float X, Y, Z, W;

        public float4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float4(byte[] value)
        {
            X = BitConverter.ToSingle(value, 0);
            Y = BitConverter.ToSingle(value, sizeof(float));
            Z = BitConverter.ToSingle(value, 2 * sizeof(float));
            W = BitConverter.ToSingle(value, 3 * sizeof(float));
        }

        public static implicit operator byte[] (float4 value)
        {
            byte[] Bytes = new byte[4 * sizeof(float)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Z), 0, Bytes, 2 * sizeof(int), sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.W), 0, Bytes, 3 * sizeof(int), sizeof(float));

            return Bytes;
        }

        public override bool Equals(Object obj)
        {
            return obj is float4 && this == (float4)obj;
        }

        public static bool operator ==(float4 o1, float4 o2)
        {
            return o1.X == o2.X && o1.Y == o2.Y && o1.Z == o2.Z;
        }

        public static bool operator !=(float4 o1, float4 o2)
        {
            return !(o1 == o2);
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}, {W}";
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

        public float3(float2 v)
        {
            X = v.X;
            Y = v.Y;
            Z = 0f;
        }

        public float3(byte[] value)
        {
            X = BitConverter.ToSingle(value, 0);
            Y = BitConverter.ToSingle(value, sizeof(float));
            Z = BitConverter.ToSingle(value, 2 * sizeof(float));
        }

        public float3(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public static implicit operator byte[] (float3 value)
        {
            byte[] Bytes = new byte[3 * sizeof(float)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Z), 0, Bytes, 2 * sizeof(int), sizeof(float));

            return Bytes;
        }

        public float3 Floor()
        {
            return new float3((float)Math.Floor(X), (float)Math.Floor(Y), (float)Math.Floor(Z));
        }

        public float3 Ceil()
        {
            return new float3((float)Math.Ceiling(X), (float)Math.Ceiling(Y), (float)Math.Ceiling(Z));
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

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public static float3 operator +(float3 o1, float3 o2)
        {
            return new float3(o1.X + o2.X, o1.Y + o2.Y, o1.Z + o2.Z);
        }

        public static float3 operator -(float3 o1, float3 o2)
        {
            return new float3(o1.X - o2.X, o1.Y - o2.Y, o1.Z - o2.Z);
        }

        public static float3 operator *(float3 o1, float3 o2)
        {
            return new float3(o1.X * o2.X, o1.Y * o2.Y, o1.Z * o2.Z);
        }

        public static float3 operator /(float3 o1, float3 o2)
        {
            return new float3(o1.X / o2.X, o1.Y / o2.Y, o1.Z / o2.Z);
        }

        public static float3 operator +(float3 o1, float o2)
        {
            return new float3(o1.X + o2, o1.Y + o2, o1.Z + o2);
        }

        public static float3 operator -(float3 o1, float o2)
        {
            return new float3(o1.X - o2, o1.Y - o2, o1.Z - o2);
        }

        public static float3 operator *(float3 o1, float o2)
        {
            return new float3(o1.X * o2, o1.Y * o2, o1.Z * o2);
        }

        public static float3 operator /(float3 o1, float o2)
        {
            return new float3(o1.X / o2, o1.Y / o2, o1.Z / o2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct float2
    {
        public float X, Y;

        public float2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float2(byte[] value)
        {
            X = BitConverter.ToSingle(value, 0);
            Y = BitConverter.ToSingle(value, sizeof(float));
        }

        public static implicit operator byte[] (float2 value)
        {
            byte[] Bytes = new byte[2 * sizeof(float)];
            Array.Copy(BitConverter.GetBytes(value.X), 0, Bytes, 0, sizeof(float));
            Array.Copy(BitConverter.GetBytes(value.Y), 0, Bytes, sizeof(int), sizeof(float));

            return Bytes;
        }

        public override bool Equals(Object obj)
        {
            return obj is float2 && this == (float2)obj;
        }

        public static bool operator ==(float2 o1, float2 o2)
        {
            return o1.X == o2.X && o1.Y == o2.Y;
        }

        public static bool operator !=(float2 o1, float2 o2)
        {
            return !(o1 == o2);
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public static float2 operator +(float2 o1, float2 o2)
        {
            return new float2(o1.X + o2.X, o1.Y + o2.Y);
        }

        public static float2 operator -(float2 o1, float2 o2)
        {
            return new float2(o1.X - o2.X, o1.Y - o2.Y);
        }

        public static float2 operator *(float2 o1, float2 o2)
        {
            return new float2(o1.X * o2.X, o1.Y * o2.Y);
        }

        public static float2 operator /(float2 o1, float2 o2)
        {
            return new float2(o1.X / o2.X, o1.Y / o2.Y);
        }

        public static float2 operator +(float2 o1, float o2)
        {
            return new float2(o1.X + o2, o1.Y + o2);
        }

        public static float2 operator -(float2 o1, float o2)
        {
            return new float2(o1.X - o2, o1.Y - o2);
        }

        public static float2 operator *(float2 o1, float o2)
        {
            return new float2(o1.X * o2, o1.Y * o2);
        }

        public static float2 operator /(float2 o1, float o2)
        {
            return new float2(o1.X / o2, o1.Y / o2);
        }
    }
}
