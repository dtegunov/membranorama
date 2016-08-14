using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class ImageTexture
    {
        public int3 Size = new int3(1, 1, 1);
        public Vector2 Scale = new Vector2(1);
        public Vector2 Offset = new Vector2(0);
        byte[] Data = new byte[1];

        int TextureHandle = -1;
        public int Handle { get { return TextureHandle; } }

        public void UpdateBuffers()
        {
            FreeBuffers();

            TextureHandle = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
            {
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, new[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMagFilter.Linear });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Linear });
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, Size.X, Size.Y, 0, PixelFormat.Red, PixelType.UnsignedByte, Data);
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void FreeBuffers()
        {
            if (TextureHandle != -1)
            {
                GL.DeleteTexture(TextureHandle);
                TextureHandle = -1;
            }
        }

        public static ImageTexture FromMRC(string path)
        {
            ImageTexture NewTexture = new ImageTexture();

            HeaderMRC Header = HeaderMRC.ReadFromFile(path) as HeaderMRC;
            NewTexture.Size = Header.Dimensions;
            NewTexture.Scale = new Vector2(Header.Pixelsize.X, Header.Pixelsize.Y);
            NewTexture.Offset = new Vector2(Header.Origin.X, Header.Origin.Y);

            float[] Data = IOHelper.ReadMapFloat(path);
            float DataMin = float.MaxValue, DataMax = -float.MaxValue;
            for (int i = 0; i < Data.Length; i++)
            {
                DataMin = Math.Min(DataMin, Data[i]);
                DataMax = Math.Max(DataMax, Data[i]);
            }
            float Range = 255f / (DataMax - DataMin);

            byte[] DataByte = new byte[Data.Length];
            for (int i = 0; i < Data.Length; i++)
                DataByte[i] = (byte)((Data[i] - DataMin) * Range);
            NewTexture.Data = DataByte;

            return NewTexture;
        }

        public void Dispose()
        {
            FreeBuffers();
        } 
    }
}