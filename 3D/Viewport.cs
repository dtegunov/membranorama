using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class Viewport : DataBase
    {
        GLControl GLControl;

        private Camera _Camera = null;
        public Camera Camera
        {
            get { return _Camera; }
            set { if (value != _Camera) { _Camera = value; OnPropertyChanged(); } }
        }

        public Vector3 PixelScale = new Vector3(1);

        private Vector2 LastPosition = new Vector2(0);
        private bool StartedClick = false;

        public event Action Paint;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseMove;
        public event Action<Ray3, System.Windows.Forms.MouseEventArgs> MouseClick;

        public Viewport()
        {
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

            GL.CullFace(CullFaceMode.FrontAndBack);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);

            _Camera = new Camera();
            _Camera.ViewportSize = new int2(GLControl.Width, GLControl.Height);
            _Camera.PropertyChanged += _Camera_PropertyChanged;
        }

        void GLControl_MouseLeave(object sender, EventArgs e)
        {

        }

        void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta < 0)
                _Camera.Distance *= 1.25f;
            else if (e.Delta > 0)
                _Camera.Distance /= 1.25f;
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
                    Vector2 Angles = new Vector2(-Delta.X, -Delta.Y);
                    //Angles.Y = 0;
                    Angles = Angles / 180f / 4f * (float)Math.PI;
                    _Camera.Orbit(Angles);
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                {
                    _Camera.PanPixels(new Vector2(-Delta.X, Delta.Y));
                }
            }

            if (!StartedClick && MouseMove != null)
                MouseMove(_Camera.GetRayThroughPixel(NewPosition), e);

            LastPosition = NewPosition;
        }

        void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (StartedClick && MouseClick != null)
            {
                Vector2 NewPosition = new Vector2(e.X, e.Y);
                MouseClick(_Camera.GetRayThroughPixel(NewPosition), e);
            }
        }

        void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            LastPosition = new Vector2(e.X, e.Y);
            StartedClick = true;
        }

        void GLControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Redraw();
        }

        void GLControl_Resize(object sender, EventArgs e)
        {
            GLControl.MakeCurrent();
            _Camera.ViewportSize = new int2(GLControl.Width, GLControl.Height);
            GL.Viewport(0, 0, GLControl.Width, GLControl.Height);
        }

        public GLControl GetControl()
        {
            GLControl.MakeCurrent();
            return GLControl;
        }

        public void Redraw()
        {
            GLControl.MakeCurrent();
            if (Paint != null)
                Paint();
        }

        public void MakeCurrent()
        {
            GLControl.MakeCurrent();
        }

        public void SwapBuffers()
        {
            GLControl.SwapBuffers();
        }

        void _Camera_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged();
        }
    }
}
