using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class PointCloud
    {
        public GLControl GLContext;

        int BufferPosition = -1, BufferColor = -1, BufferSelection = -1;
        int BufferOrientationX = -1, BufferOrientationY = -1, BufferOrientationZ = -1;
        int VertexArray = -1;

        public List<SurfacePoint> Points = new List<SurfacePoint>();

        public event Action BuffersUpdated;

        public void AddPoints(IEnumerable<SurfacePoint> points)
        {
            Points.AddRange(points);
            UpdateBuffers();
        }

        public void RemovePoints(IEnumerable<SurfacePoint> points)
        {
            Points.RemoveAll(points.Contains);
            UpdateBuffers();
        }

        public void SelectPoints(IEnumerable<SurfacePoint> points)
        {
            // Set selection
            foreach (SurfacePoint point in Points)
                point.IsSelected = false;
            if (points != null)
                foreach (SurfacePoint point in points)
                    point.IsSelected = true;

            UpdateBuffers();
        }

        public void UpdateBuffers()
        {
            GLContext.MakeCurrent();

            FreeBuffers();

            int NumVertices = Points.Count;
            if (NumVertices == 0)
            {
                if (BuffersUpdated != null)
                    BuffersUpdated();
                return;
            }
            VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(VertexArray);

            BufferPosition = GL.GenBuffer();
            BufferColor = GL.GenBuffer();
            BufferSelection = GL.GenBuffer();
            BufferOrientationX = GL.GenBuffer();
            BufferOrientationY = GL.GenBuffer();
            BufferOrientationZ = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferPosition);
            {
                Vector3[] Data = new Vector3[NumVertices];
                for (int i = 0; i < Points.Count; i++)
                    Data[i] = Points[i].Position;

                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferColor);
            {
                Vector4[] Data = new Vector4[NumVertices];
                for (int i = 0; i < Points.Count; i++)
                    Data[i] = Points[i].Color;

                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector4.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferSelection);
            {
                float[] Data = new float[NumVertices];
                for (int i = 0; i < Points.Count; i++)
                    Data[i] = Points[i].IsSelected ? 1.0f : 0.0f;

                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * sizeof(float)), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, sizeof(float), 0);
                GL.EnableVertexAttribArray(2);
            }

            // Orientation matrix as columns
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferOrientationX);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < Points.Count; i++)
                        Data[i] = Points[i].TransformedMatrix.Column0;

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr) (NumVertices * Vector3.SizeInBytes), Data,
                        BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
                    GL.EnableVertexAttribArray(3);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferOrientationY);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < Points.Count; i++)
                        Data[i] = Points[i].TransformedMatrix.Column1;

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
                    GL.EnableVertexAttribArray(4);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferOrientationZ);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < Points.Count; i++)
                        Data[i] = Points[i].TransformedMatrix.Column2;

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
                    GL.EnableVertexAttribArray(5);
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            BuffersUpdated?.Invoke();
        }

        public void FreeBuffers()
        {
            GLContext.MakeCurrent();

            if (VertexArray != -1)
            {
                GL.DeleteVertexArray(VertexArray);
                VertexArray = -1;
            }
            if (BufferPosition != -1)
            {
                GL.DeleteBuffer(BufferPosition);
                BufferPosition = -1;
            }
            if (BufferColor != -1)
            {
                GL.DeleteBuffer(BufferColor);
                BufferColor = -1;
            }
            if (BufferSelection != -1)
            {
                GL.DeleteBuffer(BufferSelection);
                BufferSelection = -1;
            }
            if (BufferOrientationX != -1)
            {
                GL.DeleteBuffer(BufferOrientationX);
                BufferOrientationX = -1;
            }
            if (BufferOrientationY != -1)
            {
                GL.DeleteBuffer(BufferOrientationY);
                BufferOrientationY = -1;
            }
            if (BufferOrientationZ != -1)
            {
                GL.DeleteBuffer(BufferOrientationZ);
                BufferOrientationZ = -1;
            }
        }

        public void Draw()
        {
            if (!Points.Any())
                return;

            GL.BindVertexArray(VertexArray);
            GL.DrawArrays(PrimitiveType.Points, 0, Points.Count);
        }
    }
}