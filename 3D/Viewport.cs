using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Color = System.Windows.Media.Color;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Membranogram
{
    public class Viewport : DataBase
    {
        private static object Sync = new object();
        GLControl GLControl;
        Window ParentWindow;

        private Color _BackgroundColor = Colors.White;
        public Color BackgroundColor
        {
            get { return _BackgroundColor; }
            set { if (value != _BackgroundColor) { _BackgroundColor = value; OnPropertyChanged(); Redraw(); } }
        }

        private Camera _Camera = null;
        public Camera Camera
        {
            get { return _Camera; }
            set { if (value != _Camera) { _Camera = value; OnPropertyChanged(); } }
        }

        public bool IsRollOnly = false;

        private Vector2 LastPosition = new Vector2(0);
        private bool StartedClick = false;

        public bool AreUpdatesDisabled = false;

        public event Action Paint;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseMove;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseClick;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseWheel;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseDown;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseUp;

        public Viewport(Window parentWindow)
        {
            ParentWindow = parentWindow;

            OpenTK.Graphics.GraphicsMode Mode = new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(8, 8, 8, 8), 24);
            GLControl = new GLControl(Mode, 4, 4, OpenTK.Graphics.GraphicsContextFlags.Default);
            GLControl.MakeCurrent();
            GLControl.Paint += GLControl_Paint;
            GLControl.Resize += GLControl_Resize;
            GLControl.MouseDown += GLControl_MouseDown;
            GLControl.MouseUp += GLControl_MouseUp;
            GLControl.MouseMove += GLControl_MouseMove;
            GLControl.MouseWheel += GLControl_MouseWheel;
            GLControl.MouseLeave += GLControl_MouseLeave;
            GLControl.Dock = System.Windows.Forms.DockStyle.Fill;

            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            _Camera = new Camera();
            _Camera.ViewportSize = new int2(GLControl.Width, GLControl.Height);
            _Camera.PropertyChanged += _Camera_PropertyChanged;
        }

        void GLControl_MouseLeave(object sender, EventArgs e)
        {

        }

        void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!Helper.CtrlDown())
            {
                if (e.Delta < 0)
                    if (!Camera.IsOrthogonal)
                        Camera.Distance *= 1.25f;
                    else
                        Camera.OrthogonalSize *= 1.25f;
                else if (e.Delta > 0)
                    if (!Camera.IsOrthogonal)
                        Camera.Distance /= 1.25f;
                    else
                        Camera.OrthogonalSize *= 1f / 1.25f;
            }
            else
            {
                Vector2 NewPosition = new Vector2(e.X, e.Y);
                MouseWheel?.Invoke(_Camera.GetRayThroughPixel(NewPosition), e);
            }
        }

        void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 NewPosition = new Vector2(e.X, e.Y);
            Vector2 Delta = NewPosition - LastPosition;
            if (Delta.Length > 0f)
                StartedClick = false;

            if (!Helper.CtrlDown() && !Helper.ShiftDown() && !Helper.AltDown())
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (!IsRollOnly)
                    {
                        Vector2 Angles = new Vector2(-Delta.X, -Delta.Y);
                        //Angles.Y = 0;
                        Angles = Angles / 180f / 4f * (float)Math.PI;
                        Camera.Orbit(Angles);
                    }
                    else
                    {
                        float Angle = -(Delta.X + Delta.Y) / 180f / 4f * (float)Math.PI;
                        Camera.Roll(Angle);
                    }
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                {
                    Camera.PanPixels(new Vector2(-Delta.X, Delta.Y));
                }
            }

            if (!StartedClick)
                MouseMove?.Invoke(Camera.GetRayThroughPixel(NewPosition), e);

            LastPosition = NewPosition;
        }

        void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 NewPosition = new Vector2(e.X, e.Y);

            if (StartedClick && MouseClick != null)
                MouseClick(_Camera.GetRayThroughPixel(NewPosition), e);

            MouseUp?.Invoke(_Camera.GetRayThroughPixel(NewPosition), e);
        }

        void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            LastPosition = new Vector2(e.X, e.Y);
            StartedClick = true;

            MouseDown?.Invoke(_Camera.GetRayThroughPixel(LastPosition), e);
        }

        void GLControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Redraw();
        }

        void GLControl_Resize(object sender, EventArgs e)
        {
            _Camera.ViewportSize = new int2(GLControl.Width, GLControl.Height);
            GLControl.MakeCurrent();
            GL.Viewport(0, 0, GLControl.Width, GLControl.Height);
        }

        public GLControl GetControl()
        {
            GLControl.MakeCurrent();
            return GLControl;
        }

        public void Redraw()
        {
            if (!AreUpdatesDisabled)
                lock (Sync)
                {
                    GLControl.MakeCurrent();

                    GL.ClearColor(new OpenTK.Graphics.Color4(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, 255));
                    GL.ClearDepth(1.0);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    Paint?.Invoke();

                    GL.Finish();
                    GLControl.SwapBuffers();
                }
        }

        public void MakeCurrent()
        {
            GLControl.MakeCurrent();
        }
        
        public void GrabScreenshot(string path)
        {
            MakeCurrent();

            bool IsMRC = path.ToLower().Substring(path.Length - 3) == "mrc";
            Color ScreenshotBackground = IsMRC ? Colors.Black : Colors.White;

            Color OldBackground = BackgroundColor;
            if (IsMRC)
            {
                BackgroundColor = ScreenshotBackground;
                Redraw();
            }

            Bitmap bmp = new Bitmap(GLControl.ClientSize.Width, GLControl.ClientSize.Height);
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(GLControl.ClientRectangle, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, GLControl.ClientSize.Width, GLControl.ClientSize.Height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            if (!IsMRC)
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                using (Stream ImageStream = File.Create(path))
                {
                    bmp.Save(ImageStream, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            else
            {
                using (BinaryWriter Writer = new BinaryWriter(File.Create(path)))
                {
                    HeaderMRC Header = new HeaderMRC
                    {
                        Dimensions = new int3(bmp.Width, bmp.Height, 1),
                        Mode = MRCDataType.Byte
                    };
                    Header.Write(Writer);

                    int Elements = bmp.Width * bmp.Height;
                    byte[] Data = new byte[Elements];
                    System.Drawing.Imaging.BitmapData BitmapData = bmp.LockBits(GLControl.ClientRectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        fixed (byte* DataPtr = Data)
                        {
                            byte* BitmapDataP = (byte*)BitmapData.Scan0;

                            for (int i = 0; i < Elements; i++)
                                DataPtr[i] = BitmapDataP[i * 4 + 1];
                        }
                    }
                    bmp.UnlockBits(BitmapData);

                    Writer.Write(Data);
                }

                BackgroundColor = OldBackground;
            }
        }

        void _Camera_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Redraw();
        }
    }
}
