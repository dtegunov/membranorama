using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Warp;

namespace Membranogram
{
    public class SurfacePoint : DataBase
    {
        private string _PositionXText = "";
        public string PositionXText
        {
            get { return _PositionXText; }
            set { if (value != _PositionXText) { _PositionXText = value; OnPropertyChanged(); } }
        }

        private string _PositionYText = "";
        public string PositionYText
        {
            get { return _PositionYText; }
            set { if (value != _PositionYText) { _PositionYText = value; OnPropertyChanged(); } }
        }

        private string _PositionZText = "";
        public string PositionZText
        {
            get { return _PositionZText; }
            set { if (value != _PositionZText) { _PositionZText = value; OnPropertyChanged(); } }
        }

        private string _OffsetText = "";
        public string OffsetText
        {
            get { return _OffsetText; }
            set { if (value != _OffsetText) { _OffsetText = value; OnPropertyChanged(); } }
        }

        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { if (value != _IsSelected) { _IsSelected = value; OnPropertyChanged(); } }
        }

        private Vector4 _Color = new Vector4(1, 1, 0, 0.3f);
        public Vector4 Color
        {
            get { return _Color; }
            set { if (value != _Color) { _Color = value; OnPropertyChanged(); } }
        }

        private Vector3 _Position;
        public Vector3 Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                PositionXText = string.Format("{0:0.0}", _Position.X);
                PositionYText = string.Format("{0:0.0}", _Position.Y);
                PositionZText = string.Format("{0:0.0}", _Position.Z);
            }
        }

        private float _SurfaceOffset;
        public float SurfaceOffset
        {
            get { return _SurfaceOffset; }
            set
            {
                _SurfaceOffset = value;
                OffsetText = string.Format("{0:0.0}", SurfaceOffset);
            }
        }

        private float _Psi;
        public float Psi
        {
            get { return _Psi; }
            set { if (value != _Psi) { _Psi = value;  OnPropertyChanged(); } }
        }

        private Matrix3 _OriginalMatrix;
        public Matrix3 OriginalMatrix
        {
            get { return _OriginalMatrix; }
            set { if (value != _OriginalMatrix) { _OriginalMatrix = value; OnPropertyChanged(); } }
        }

        public Matrix3 TransformedMatrix => OriginalMatrix * Matrix3.CreateRotationZ(Psi);

        public Triangle Face;
        public Vector3 BarycentricCoords;

        public PointGroup Group;

        public Mesh DepictionMesh = null;

        public SurfacePoint(Vector3 position, Triangle face, Vector3 barycentricCoords, float surfaceOffset, float psi)
        {
            Position = position;
            Face = face;
            BarycentricCoords = barycentricCoords;
            SurfaceOffset = surfaceOffset;
            Psi = psi;
            OriginalMatrix = face.GetPlaneMatrix3();
        }

        public SurfacePoint(Vector3 position, Triangle face, Vector3 barycentricCoords, float surfaceOffset, Matrix3 orientationMatrix)
        {
            Position = position;
            Face = face;
            BarycentricCoords = barycentricCoords;
            SurfaceOffset = surfaceOffset;
            Psi = 0;
            OriginalMatrix = orientationMatrix;
        }

        public float DistanceFromRay(Ray3 ray)
        {
            Vector3 ToPoint = (Position - ray.Origin);
            float DotP = Vector3.Dot(ray.Direction.Normalized(), ToPoint);
            if (DotP <= 0)
                return float.PositiveInfinity;

            Vector3 Closest = ray.Origin + ray.Direction.Normalized() * DotP;
            return (Closest - Position).Length;
        }

        public void DropDepictionMesh()
        {
            if (DepictionMesh != null)
            {
                DepictionMesh.Dispose();
                DepictionMesh = null;
            }
        }
    }
}
