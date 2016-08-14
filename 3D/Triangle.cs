using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;

namespace Membranogram
{
    public class Triangle
    {
        public long ID;

        public Vertex V0, V1, V2;
        public Vertex[] Vertices = new Vertex[3];
        public Edge[] Edges = new Edge[3];
        public Vector3 Normal;
        public Vector3 VolumeNormal;
        public Vector4 Color = new Vector4(0);
        public bool IsSelected = false;
        public bool IsVisible = true;
        public SurfacePatch Patch;

        public List<Triangle> Neighbors = new List<Triangle>();

        public Triangle(long id, Vertex v0, Vertex v1, Vertex v2)
        {
            ID = id;

            V0 = v0;
            V1 = v1;
            V2 = v2;

            Vertices[0] = v0;
            if (!v0.Triangles.Contains(this))
                v0.Triangles.Add(this);
            Vertices[1] = v1;
            if (!v1.Triangles.Contains(this))
                v1.Triangles.Add(this);
            Vertices[2] = v2;
            if (!v2.Triangles.Contains(this))
                v2.Triangles.Add(this);

            UpdateNormal();
            UpdateVolumeNormal();
        }

        /// <summary>
        /// Recalculates the normal based only on this triangle's tangents.
        /// </summary>
        public void UpdateNormal()
        {
            Vector3 Tangent01 = (V1.Position - V0.Position);
            Vector3 Tangent02 = (V2.Position - V0.Position);
            Normal = Vector3.Cross(Tangent01, Tangent02).Normalized();
        }

        /// <summary>
        /// Recalculates the normal used for offsetting positions within tomogram space based only on this triangle's tangents.
        /// </summary>
        public void UpdateVolumeNormal()
        {
            Vector3 Tangent01 = (V1.VolumePosition - V0.VolumePosition);
            Vector3 Tangent02 = (V2.VolumePosition - V0.VolumePosition);
            VolumeNormal = Vector3.Cross(Tangent01, Tangent02).Normalized();
        }

        /// <summary>
        /// Populates the Neighbors list with triangles sharing 2 vertices with this triangle.
        /// </summary>
        public void UpdateNeighbors()
        {
            Neighbors.Clear();

            foreach (Vertex ownV in Vertices)
                foreach (Triangle t in ownV.Triangles)
                    if (t != this && !Neighbors.Contains(t))
                    {
                        int Common = 0;
                        foreach (Vertex v in Vertices)
                            if (t.Vertices.Contains(v))
                                Common++;
                        if (Common == 2)
                            Neighbors.Add(t);
                    }
        }

        /// <summary>
        /// Populates the Edges list with edges making up this triangle, and adds the edges to the vertices' own edge lists.
        /// </summary>
        public void UpdateEdges()
        {
            for (int i = 0; i < 3; i++)
            {
                Vertex v0 = Vertices[i];
                Vertex v1 = Vertices[(i + 1) % 3];
                Edge NewEdge = new Edge(v0, v1);

                // Check if edge already exists
                bool Exists = false;
                foreach (Edge e in v0.Edges)
                    if (e == NewEdge)
                    {
                        Exists = true;
                        NewEdge = e;
                        break;
                    }

                if (!Exists)
                {
                    v0.Edges.Add(NewEdge);
                    v1.Edges.Add(NewEdge);
                }

                Edges[i] = NewEdge;
            }
        }

        /// <summary>
        /// Calculates the intersection between the triangle and a ray, if they intersect.
        /// Adapted from https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm.
        /// </summary>
        /// <param name="ray">Ray to intersect with</param>
        /// <returns>Null if no intersection; intersection data otherwise</returns>
        public Intersection Intersect(Ray3 ray)
        {
            float Epsilon = 1e-5f;

            Vector3 e1, e2;  //Edge1, Edge2
            Vector3 P, Q, T;
            float det, inv_det, u, v;
            float t;

            //Find vectors for two edges sharing V1
            e1 = V1.Position - V0.Position;
            e2 = V2.Position - V0.Position;

            //Begin calculating determinant - also used to calculate u parameter
            P = Vector3.Cross(ray.Direction, e2);
            //if determinant is near zero, ray lies in plane of triangle
            det = Vector3.Dot(e1, P);
            //NOT CULLING
            if(det > -Epsilon && det < Epsilon) 
                return null;
            inv_det = 1f / det;

            //calculate distance from V1 to ray origin
            T = ray.Origin - V0.Position;

            //Calculate u parameter and test bound
            u = Vector3.Dot(T, P) * inv_det;
            //The intersection lies outside of the triangle
            if(u < 0f || u > 1f) 
                return null;

            //Prepare to test v parameter
            Q = Vector3.Cross(T, e1);

            //Calculate V parameter and test bound
            v = Vector3.Dot(ray.Direction, Q) * inv_det;
            //The intersection lies outside of the triangle
            if(v < 0f || u + v  > 1f) 
                return null;

            t = Vector3.Dot(e2, Q) * inv_det;

            if(t > Epsilon) //ray intersection
            {
                Intersection I = new Intersection();
                I.Distance = t;
                I.Position = ray.Origin + ray.Direction * t;
                I.Ray = ray;
                I.Target = this;
                return I;
            }

            // No hit, no win
            return null;
        }

        /// <summary>
        /// Gets the area of the processed triangle, not in tomogram space.
        /// </summary>
        /// <returns></returns>
        public float GetArea()
        {
            float L0 = Edges[0].GetLength(), L1 = Edges[2].GetLength(), L2 = Edges[1].GetLength();

            float S = (L0 + L1 + L2) * 0.5f;
            return (float)Math.Sqrt(S * (S - L0) * (S - L1) * (S - L2));
        }

        /// <summary>
        /// Gets the area of the processed triangle, in tomogram space.
        /// </summary>
        /// <returns></returns>
        public float GetVolumeArea()
        {
            float L0 = Edges[0].GetVolumeLength(), L1 = Edges[2].GetVolumeLength(), L2 = Edges[1].GetVolumeLength();

            float S = (L0 + L1 + L2) * 0.5f;
            return (float)Math.Sqrt(S * (S - L0) * (S - L1) * (S - L2));
        }

        public Vector3 GetCenter()
        {
            return (V0.Position + V1.Position + V2.Position) * (1f / 3f);
        }

        public Vector3 GetVolumeCenter()
        {
            return (V0.VolumePosition + V1.VolumePosition + V2.VolumePosition) * (1f / 3f);
        }

        /// <summary>
        /// Converts a 3D position on the triangle's surface into barycentric coordinates.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 ToBarycentric(Vector3 position)
        {
            Vector3 v0 = V1.Position - V0.Position, 
                    v1 = V2.Position - V0.Position, 
                    v2 = position - V0.Position;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }

        /// <summary>
        /// Converts a 3D position on the triangle's surface in tomogram space into barycentric coordinates.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 ToVolumeBarycentric(Vector3 position)
        {
            Vector3 v0 = V1.VolumePosition - V0.VolumePosition, 
                    v1 = V2.VolumePosition - V0.VolumePosition, 
                    v2 = position - V0.VolumePosition;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }

        /// <summary>
        /// Converts a position expressed in barycentric coordinates into absolute 3D space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 FromBarycentric(Vector3 position)
        {
            return V0.Position * position.X + V1.Position * position.Y + V2.Position * position.Z;
        }

        /// <summary>
        /// Converts a position expressed in barycentric coordinates into absolute 3D tomogram space.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 FromVolumeBarycentric(Vector3 position)
        {
            return V0.VolumePosition * position.X + V1.VolumePosition * position.Y + V2.VolumePosition * position.Z;
        }

        public Vector3 ProjectPoint(Vector3 position)
        {
            position -= V0.Position;
            float AlongNormal = Vector3.Dot(position, Normal);
            position -= AlongNormal * Normal;
            position += V0.Position;

            return position;
        }

        /// <summary>
        /// Returns a matrix where each column is one of the coordinate axes defined 
        /// by this plane, x being V1 - V0, z being the normal.
        /// Note that this is the transposed version of what you would expect,
        /// because OpenTK does vector transforms as v * m.
        /// </summary>
        /// <returns></returns>
        public Matrix4 GetPlaneMatrix()
        {
            Vector3 C1 = (V1.Position - V0.Position).Normalized();
            Vector3 C2 = Vector3.Cross(Normal, C1);
            Vector3 C3 = Normal;

            return new Matrix4(new Vector4(C1.X, C2.X, C3.X, 0), 
                               new Vector4(C1.Y, C2.Y, C3.Y, 0), 
                               new Vector4(C1.Z, C2.Z, C3.Z, 0), 
                               new Vector4(   0,    0,    0, 1));
        }

        /// <summary>
        /// Returns a matrix where each column is one of the coordinate axes defined 
        /// by this plane, x being V1 - V0, z being the normal.
        /// Note that this is the transposed version of what you would expect,
        /// because OpenTK does vector transforms as v * m.
        /// </summary>
        /// <returns></returns>
        public Matrix3 GetPlaneMatrix3()
        {
            Vector3 C1 = (V1.Position - V0.Position).Normalized();
            Vector3 C2 = Vector3.Cross(Normal, C1);
            Vector3 C3 = Normal;

            return new Matrix3(new Vector3(C1.X, C2.X, C3.X),
                               new Vector3(C1.Y, C2.Y, C3.Y),
                               new Vector3(C1.Z, C2.Z, C3.Z));
        }

        /// <summary>
        /// Returns a matrix where each column is one of the coordinate axes defined 
        /// by this plane, x being V1 - V0, z being the normal.
        /// Note that this is the transposed version of what you would expect,
        /// because OpenTK does vector transforms as v * m.
        /// </summary>
        /// <returns></returns>
        public Matrix4 GetVolumePlaneMatrix()
        {
            Vector3 C1 = (V1.VolumePosition - V0.VolumePosition).Normalized();
            Vector3 C2 = Vector3.Cross(VolumeNormal, C1);
            Vector3 C3 = VolumeNormal;

            return new Matrix4(new Vector4(C1.X, C2.X, C3.X, 0),
                               new Vector4(C1.Y, C2.Y, C3.Y, 0),
                               new Vector4(C1.Z, C2.Z, C3.Z, 0),
                               new Vector4(0, 0, 0, 1));
        }

        /// <summary>
        /// Returns a matrix where each column is one of the coordinate axes defined 
        /// by this plane, x being V1 - V0, z being the normal.
        /// Note that this is the transposed version of what you would expect,
        /// because OpenTK does vector transforms as v * m.
        /// </summary>
        /// <returns></returns>
        public Matrix3 GetVolumePlaneMatrix3()
        {
            Vector3 C1 = (V1.VolumePosition - V0.VolumePosition).Normalized();
            Vector3 C2 = Vector3.Cross(VolumeNormal, C1);
            Vector3 C3 = VolumeNormal;

            return new Matrix3(new Vector3(C1.X, C2.X, C3.X),
                               new Vector3(C1.Y, C2.Y, C3.Y),
                               new Vector3(C1.Z, C2.Z, C3.Z));
        }

        public static HashSet<Triangle> Grow(HashSet<Triangle> set)
        {
            HashSet<Triangle> Grown = new HashSet<Triangle>(set);
            foreach (var t in set)
                foreach (var n in t.Neighbors.Where(i => !Grown.Contains(i)))
                    Grown.Add(n);

            return Grown;
        }

        public static HashSet<Triangle> Shrink(HashSet<Triangle> set)
        {
            HashSet<Triangle> Shrunk = new HashSet<Triangle>(set);
            Shrunk.RemoveWhere(t => t.Neighbors.Any(i => !set.Contains(i)));

            return Shrunk;
        }

        public static Triangle ShrinkCompletely(HashSet<Triangle> set)
        {
            HashSet<Triangle> PreviousSet = new HashSet<Triangle>(set);
            while (PreviousSet.Count - Shrink(PreviousSet).Count > 0)
            {
                HashSet<Triangle> Next = Shrink(PreviousSet);
                if (Next.Count > 0)
                    PreviousSet = Next;
                else
                    break;
            }

            Vector3 SetCenter = new Vector3();
            foreach (var t in PreviousSet)
                SetCenter += t.GetCenter();
            SetCenter *= 1f / PreviousSet.Count;

            List<Triangle> Sorted = PreviousSet.ToList();
            Sorted.Sort((a, b) => (a.GetCenter() - SetCenter).Length.CompareTo((b.GetCenter() - SetCenter).Length));

            return Sorted[0];
        }
    }

    public class Intersection
    {
        public Vector3 Position;
        public float Distance;
        public Ray3 Ray;
        public object Target;
    }
}
