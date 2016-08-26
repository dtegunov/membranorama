using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Warp.Tools;
using Warp.Headers;

namespace Membranogram
{
    public class VolumeTexture : IDisposable
    {
        public int3 Size = new int3(1, 1, 1);
        public Vector3 Scale = new Vector3(1);
        public Vector3 Offset = new Vector3(0);
        public float[] OriginalData = new float[1];
        byte[] Data = new byte[1];

        int TextureHandle = -1;
        public int Handle { get { return TextureHandle; } }

        public void UpdateBuffers()
        {
            FreeBuffers();

            TextureHandle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture3D, TextureHandle);
            {
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, new [] { (int)TextureWrapMode.Repeat });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, new [] { (int)TextureWrapMode.Repeat });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, new [] { (int)TextureWrapMode.Repeat });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, new [] { (int)TextureMagFilter.Linear });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, new [] { (int)TextureMinFilter.Linear });
                GL.TexImage3D<byte>(TextureTarget.Texture3D, 0, PixelInternalFormat.R8, Size.X, Size.Y, Size.Z, 0, PixelFormat.Red, PixelType.UnsignedByte, Data);                
            }
            GL.BindTexture(TextureTarget.Texture3D, 0);
        }

        public void FreeBuffers()
        {
            if (TextureHandle != -1)
            {
                GL.DeleteTexture(TextureHandle);
                TextureHandle = -1;
            }
        }

        public static VolumeTexture FromMRC(string path)
        {
            VolumeTexture NewTexture = new VolumeTexture();

            HeaderMRC Header = HeaderMRC.ReadFromFile(path) as HeaderMRC;
            NewTexture.Size = Header.Dimensions;
            NewTexture.Scale = new Vector3(Header.Pixelsize.X, Header.Pixelsize.Y, Header.Pixelsize.Z);
            NewTexture.Offset = new Vector3(Header.Origin.X, Header.Origin.Y, Header.Origin.Z);

            unsafe
            {
                float[] OriginalData = IOHelper.ReadSmallMapFloat(path, new int2(1, 1), 0, typeof(float));
                float DataMin = float.MaxValue, DataMax = float.MinValue;
                fixed (float* DataPtr = OriginalData)
                {
                    float* DataP = DataPtr;
                    for (int i = 0; i < OriginalData.Length; i++)
                    {
                        DataMin = Math.Min(DataMin, *DataP);
                        DataMax = Math.Max(DataMax, *DataP++);
                    }
                    float Range = (DataMax - DataMin) / 255f;

                    byte[] DataByte = new byte[OriginalData.Length];
                    fixed (byte* DataBytePtr = DataByte)
                    {
                        byte* DataByteP = DataBytePtr;
                        DataP = DataPtr;
                        for (int i = 0; i < OriginalData.Length; i++)
                            *DataByteP++ = (byte)((*DataP++ - DataMin) / Range);
                    }
                    NewTexture.Data = DataByte;
                }

                NewTexture.OriginalData = OriginalData;
            }

            return NewTexture;
        }

        public void Dispose()
        {
            FreeBuffers();
        }
    }
}
