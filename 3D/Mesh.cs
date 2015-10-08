using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class Mesh : IDisposable
    {
        public List<Vertex> Vertices = new List<Vertex>();
        public List<Triangle> Triangles = new List<Triangle>();
        private List<Triangle> IntersectionTriangles = new List<Triangle>();
        private Dictionary<Triangle, Triangle> IntersectionTriangleMapping = new Dictionary<Triangle, Triangle>();

        int BufferPosition = -1, BufferNormal = -1, BufferVolumePosition = -1, BufferVolumeNormal = -1;
        int BufferColor = -1;
        int VertexArray = -1;

        public void UpdateBuffers()
        {
            FreeBuffers();

            int NumVertices = Triangles.Count * 3;

            VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(VertexArray);

            BufferPosition = GL.GenBuffer();
            BufferNormal = GL.GenBuffer();
            BufferVolumePosition = GL.GenBuffer();
            BufferVolumeNormal = GL.GenBuffer();
            BufferColor = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferPosition);
            {
                Vector3[] Data = new Vector3[NumVertices];
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Data[i * 3 + 0] = Triangles[i].V0.Position;
                    Data[i * 3 + 1] = Triangles[i].V1.Position;
                    Data[i * 3 + 2] = Triangles[i].V2.Position;
                }

                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferNormal);
            {
                Vector3[] Data = new Vector3[NumVertices];
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Data[i * 3 + 0] = Triangles[i].V0.SmoothNormal;
                    Data[i * 3 + 1] = Triangles[i].V1.SmoothNormal;
                    Data[i * 3 + 2] = Triangles[i].V2.SmoothNormal;
                }

                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVolumePosition);
            {
                Vector3[] Data = new Vector3[NumVertices];
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Data[i * 3 + 0] = Triangles[i].V0.VolumePosition;
                    Data[i * 3 + 1] = Triangles[i].V1.VolumePosition;
                    Data[i * 3 + 2] = Triangles[i].V2.VolumePosition;
                }

                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(2);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVolumeNormal);
            {
                Vector3[] Data = new Vector3[NumVertices];
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Data[i * 3 + 0] = Triangles[i].V0.SmoothVolumeNormal;
                    Data[i * 3 + 1] = Triangles[i].V1.SmoothVolumeNormal;
                    Data[i * 3 + 2] = Triangles[i].V2.SmoothVolumeNormal;
                }

                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(3);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferColor);
            {
                Vector4[] Data = new Vector4[NumVertices];
                for (int i = 0; i < Triangles.Count; i++)
                {
                    Data[i * 3 + 0] = Triangles[i].Color;
                    Data[i * 3 + 1] = Triangles[i].Color;
                    Data[i * 3 + 2] = Triangles[i].Color;
                }

                GL.BufferData<Vector4>(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector4.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(4);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void FreeBuffers()
        {
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
            if (BufferNormal != -1)
            {
                GL.DeleteBuffer(BufferNormal);
                BufferNormal = -1;
            }
            if (BufferVolumePosition != -1)
            {
                GL.DeleteBuffer(BufferVolumePosition);
                BufferVolumePosition = -1;
            }
            if (BufferVolumeNormal != -1)
            {
                GL.DeleteBuffer(BufferVolumeNormal);
                BufferVolumeNormal = -1;
            }
            if (BufferColor != -1)
            {
                GL.DeleteBuffer(BufferColor);
                BufferColor = -1;
            }
        }

        public void UpdateColors()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferColor);
            {
                IntPtr DeviceColors = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(Triangles.Count * 3 * Vector4.SizeInBytes), BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit);

                unsafe
                {
                    Vector4* DeviceColorsPtr = (Vector4*)DeviceColors;
                    for (int i = 0; i < Triangles.Count; i++)
                    {
                        *DeviceColorsPtr++ = Triangles[i].Color;
                        *DeviceColorsPtr++ = Triangles[i].Color;
                        *DeviceColorsPtr++ = Triangles[i].Color;
                    }
                }

                GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Draw()
        {
            GL.BindVertexArray(VertexArray);
            GL.DrawArrays(PrimitiveType.Triangles, 0, Triangles.Count * 3);
            GL.Flush();
        }

        public void UpdateGraph()
        {
            foreach (Triangle t in Triangles)
                t.UpdateNeighbors();
        }

        public Vector3 GetMin()
        {
            Vector3 Min = new Vector3(1e10f, 1e10f, 1e10f);
            foreach (Vertex v in Vertices)
            {
                Min.X = Math.Min(Min.X, v.Position.X);
                Min.Y = Math.Min(Min.Y, v.Position.Y);
                Min.Z = Math.Min(Min.Z, v.Position.Z);
            }

            return Min;
        }

        public Vector3 GetMax()
        {
            Vector3 Max = new Vector3(-1e10f, -1e10f, -1e10f);
            foreach (Vertex v in Vertices)
            {
                Max.X = Math.Max(Max.X, v.Position.X);
                Max.Y = Math.Max(Max.Y, v.Position.Y);
                Max.Z = Math.Max(Max.Z, v.Position.Z);
            }

            return Max;
        }

        public void UpdateIntersectionGeometry(float offset)
        {
            IntersectionTriangles = new List<Triangle>(Triangles.Count);
            IntersectionTriangleMapping.Clear();

            foreach (Triangle t in Triangles)
            {
                Vertex IV0 = new Vertex(t.V0.Position + t.V0.SmoothNormal * offset, new Vector3());
                Vertex IV1 = new Vertex(t.V1.Position + t.V1.SmoothNormal * offset, new Vector3());
                Vertex IV2 = new Vertex(t.V2.Position + t.V2.SmoothNormal * offset, new Vector3());
                Triangle IT = new Triangle(IV0, IV1, IV2);
                IntersectionTriangles.Add(IT);

                IntersectionTriangleMapping.Add(IT, t);
            }
        }

        public List<Intersection> Intersect(Ray3 ray)
        {
            List<Intersection> Intersections = new List<Intersection>();

            Parallel.ForEach<Triangle>(IntersectionTriangles, (t) =>
            {
                Intersection I = t.Intersect(ray);
                if (I != null)
                {
                    I.Triangle = IntersectionTriangleMapping[I.Triangle];
                    lock (Intersections)
                        Intersections.Add(I);
                }
            });

            return Intersections;
        }

        public static Mesh FromOBJ(string path)
        {
            Mesh NewMesh = new Mesh();

            // Parse vertices
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                string Line = Reader.ReadLine();
                while (Line != null)
                {
                    string[] Parts = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Parts.Length > 0 && Parts[0] == "v")
                        NewMesh.Vertices.Add(new Vertex(new Vector3(float.Parse(Parts[1]), float.Parse(Parts[2]), float.Parse(Parts[3])), new Vector3(1, 0, 0)));

                    Line = Reader.ReadLine();
                }
            }
            
            // Parse faces
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                string Line = Reader.ReadLine();
                while (Line != null)
                {
                    string[] Parts = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Parts.Length > 0 && Parts[0] == "f")
                    {
                        string[] FaceParts0 = Parts[1].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] FaceParts1 = Parts[2].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] FaceParts2 = Parts[3].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        NewMesh.Triangles.Add(new Triangle(NewMesh.Vertices[int.Parse(FaceParts0[0]) - 1], NewMesh.Vertices[int.Parse(FaceParts1[0]) - 1], NewMesh.Vertices[int.Parse(FaceParts2[0]) - 1]));
                    }
                    Line = Reader.ReadLine();
                }
            }

            // Amira likes to put unused vertices into the OBJ
            NewMesh.Vertices.RemoveAll((v) => { return v.Triangles.Count == 0; });

            NewMesh.UpdateGraph();

            return NewMesh;
        }

        public void Dispose()
        {
            FreeBuffers();
        }
    }
}
