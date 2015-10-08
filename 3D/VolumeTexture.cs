using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class VolumeTexture : IDisposable
    {
        public int3 Size = new int3(1, 1, 1);
        public Vector3 Scale = new Vector3(1);
        public Vector3 Offset = new Vector3(0);
        byte[] Data = new byte[1];

        int TextureHandle = -1;
        public int Handle { get { return TextureHandle; } }

        public void UpdateBuffers()
        {
            FreeBuffers();

            TextureHandle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture3D, TextureHandle);
            {
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, new int[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Linear });
                GL.TexParameterI(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Linear });
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

            float[] Data = IOHelper.ReadMapFloat(path);
            float DataMin = float.MaxValue, DataMax = float.MinValue;
            for (int i = 0; i < Data.Length; i++)
            {
                DataMin = Math.Min(DataMin, Data[i]);
                DataMax = Math.Max(DataMax, Data[i]);
            }
            float Range = (DataMax - DataMin) / 255f;

            byte[] DataByte = new byte[Data.Length];
            for (int i = 0; i < Data.Length; i++)
                DataByte[i] = (byte)((Data[i] - DataMin) / Range);
            NewTexture.Data = DataByte;

            return NewTexture;
        }

        public void Dispose()
        {
            FreeBuffers();
        }
    }
}
