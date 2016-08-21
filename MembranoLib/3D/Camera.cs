using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Warp;
using Warp.Tools;
using Membranogram.Helpers;

namespace Membranogram
{
    public class Camera : DataBase
    {
        private Vector3 _Target = new Vector3(0);
        public Vector3 Target
        {
            get { return _Target; }
            set { if (value != _Target) { _Target = value; OnPropertyChanged(); } }
        }

        private float _Distance = 1;
        public float Distance
        {
            get { return IsOrthogonal ? 1e4f : _Distance; }
            set { if (value != _Distance) { _Distance = value; OnPropertyChanged(); } }
        }

        public float ClipNear = 1e-1f;
        public float ClipFar = 1e6f;

        private Quaternion _Rotation = Quaternion.Identity;
        public Quaternion Rotation
        {
            get { return _Rotation; }
            set { if (value != _Rotation) { _Rotation = value; OnPropertyChanged(); } }
        }

        private decimal _FOV = (decimal)(45f / 180f * (float)Math.PI);
        public decimal FOV
        {
            get { return _FOV; }
            set { if (value != _FOV) { _FOV = value; OnPropertyChanged(); } }
        }

        private int2 _ViewportSize = new int2(100, 100);
        public int2 ViewportSize
        {
            get { return _ViewportSize; }
            set { if (value != _ViewportSize) { _ViewportSize = value; OnPropertyChanged(); OnPropertyChanged("AngstromPerPixel"); } }
        }

        private bool _IsOrthogonal = false;
        public bool IsOrthogonal
        {
            get { return _IsOrthogonal; }
            set { if (value != _IsOrthogonal) { _IsOrthogonal = value; OnPropertyChanged(); } }
        }

        private float _OrthogonalSize = 1f;
        public float OrthogonalSize
        {
            get { return _OrthogonalSize; }
            set { if (value != _OrthogonalSize) { _OrthogonalSize = value; OnPropertyChanged(); OnPropertyChanged("AngstromPerPixel"); } }
        }
        
        public float AngstromPerPixel
        {
            get
            {
                Vector3 Angstrom = Target + GetPlanarX();
                Angstrom = Vector3.Transform(Angstrom, GetViewProj());
                float PixelsPerAngstrom = Angstrom.X / 2f * ViewportSize.X;

                return 1f / PixelsPerAngstrom;
            }
            set
            {
                if (value < 0.01f)
                    return;
                if (value != AngstromPerPixel)
                {
                    float Change = value / AngstromPerPixel;
                    OrthogonalSize *= Change;
                }
            }
        }

        public Camera()
        {

        }

        public Matrix4 GetView()
        {
            return Matrix4.LookAt(GetPosition(), Target, GetPlanarY());
        }

        public float[] GetViewAsArray()
        {
            return OpenGLHelper.ToFloatArray(GetView());
        }

        public Matrix4 GetProj()
        {
            if (!IsOrthogonal)
                return Matrix4.CreatePerspectiveFieldOfView((float) FOV, (float) ViewportSize.X / (float) ViewportSize.Y, ClipNear, ClipFar);
            else
                return Matrix4.CreateOrthographic(OrthogonalSize, OrthogonalSize * ViewportSize.Y / ViewportSize.X, ClipNear, ClipFar);
        }

        public float[] GetProjAsArray()
        {
            return OpenGLHelper.ToFloatArray(GetProj());
        }

        public Matrix4 GetViewProj()
        {
            return GetView() * GetProj();
        }

        public float[] GetViewProjAsArray()
        {
            return OpenGLHelper.ToFloatArray(GetViewProj());
        }

        public Vector3 GetPosition()
        {
            Vector3 Position = Target + Vector3.Transform(new Vector3(0, 0, Distance), GetRotationMatrix());
            return Position;
        }

        public Vector3 GetDirection()
        {
            return (Target - GetPosition()).Normalized();
        }

        public Matrix4 GetRotationMatrix()
        {
            return Matrix4.CreateFromQuaternion(Rotation);
        }

        public Vector3 GetPlanarX()
        {
            return Vector3.Transform(new Vector3(1, 0, 0), GetRotationMatrix()).Normalized();
        }

        public Vector3 GetPlanarY()
        {
            return Vector3.Transform(new Vector3(0, 1, 0), GetRotationMatrix()).Normalized();
        }

        public void Orbit(Vector2 angles)
        {
            Vector3 Axis = (new Vector3(0, 1, 0)) * angles.X + (new Vector3(1, 0, 0)) * angles.Y;
            float Angle = Axis.Length;
            if (Angle == 0f)
                return;
            Axis.Normalize();

            Quaternion RotateBy = Quaternion.FromAxisAngle(Axis, Angle);
            Quaternion NewRotation = Rotation * RotateBy;
            NewRotation.Normalize();
            Rotation = NewRotation;
        }

        public void Roll(float angle)
        {
            Vector3 Axis = GetDirection();
            Quaternion RotateBy = Quaternion.FromAxisAngle(Axis, angle);
            Quaternion NewRotation = Rotation * RotateBy;
            NewRotation.Normalize();
            Rotation = NewRotation;
        }

        public void Move(Vector3 delta)
        {
            Target += delta;
        }

        public void Pan(Vector2 delta)
        {
            Target += GetPlanarX() * delta.X + GetPlanarY() * delta.Y;
        }

        public void PanPixels(Vector2 delta)
        {
            Vector3 GlobalUnitary = Target + GetPlanarX();
            Vector4 GlobalTransformed = Vector4.Transform(new Vector4(GlobalUnitary.X, GlobalUnitary.Y, GlobalUnitary.Z, 1.0f), GetViewProj());
            GlobalTransformed /= GlobalTransformed.W;
            GlobalTransformed.X *= 0.5f * ViewportSize.X;

            delta = delta / GlobalTransformed.X;

            Pan(delta);
        }

        public void CenterOn(Mesh model)
        {
            Vector3 MeshMin = model.GetMin();
            Vector3 MeshMax = model.GetMax();
            Vector3 MeshCenter = (MeshMin + MeshMax) / 2f;
            Move(MeshCenter - Target);
            float MaxExtent = Math.Max(Math.Max(MeshMax.X - MeshMin.X, MeshMax.Y - MeshMin.Y), MeshMax.Z - MeshMin.Z);
            if (!IsOrthogonal)
                Distance = MaxExtent * 1.5f;
            else
                OrthogonalSize = MaxExtent;
        }

        public Ray3 GetRayThroughPixel(Vector2 pixel)
        {
            pixel.X = pixel.X - ViewportSize.X * 0.5f;		// Window origin is upper left corner, OGL is bottom right (left handed, Y = up), so flip
            pixel.Y = ViewportSize.Y - pixel.Y - ViewportSize.Y * 0.5f;
            Vector3 PlanarX = GetPlanarX(), PlanarY = GetPlanarY();

            Vector4 TransformedRight = Vector4.Transform(new Vector4(Target + PlanarX, 1.0f), GetViewProj());
            TransformedRight /= TransformedRight.W;
            TransformedRight.X *= 0.5f * ViewportSize.X;
            float ScaleFactor = 1.0f / TransformedRight.X;

            PlanarX *= ScaleFactor * pixel.X;
            PlanarY *= ScaleFactor * pixel.Y;

            Vector3 Direction = (Target - GetPosition() + PlanarX + PlanarY).Normalized();
            return new Ray3(GetPosition(), Direction);
        }
    }
}
