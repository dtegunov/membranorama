using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    public class Triangle
    {
        public Vertex V0, V1, V2;
        public Vector3 Normal;
        public Vector3 VolumeNormal;
        public Vector4 Color;

        public List<Triangle> Neighbors = new List<Triangle>();

        public Triangle(Vertex v0, Vertex v1, Vertex v2)
        {
            V0 = v0;
            if (!v0.Triangles.Contains(this))
                v0.Triangles.Add(this);
            V1 = v1;
            if (!v1.Triangles.Contains(this))
                v1.Triangles.Add(this);
            V2 = v2;
            if (!v2.Triangles.Contains(this))
                v2.Triangles.Add(this);

            Color = new Vector4(0);

            UpdateNormal();
            UpdateVolumeNormal();
        }

        public void UpdateNormal()
        {
            Vector3 Tangent01 = (V1.Position - V0.Position);
            Vector3 Tangent02 = (V2.Position - V0.Position);
            Normal = Vector3.Cross(Tangent01, Tangent02).Normalized();
        }

        public void UpdateVolumeNormal()
        {
            Vector3 Tangent01 = (V1.VolumePosition - V0.VolumePosition);
            Vector3 Tangent02 = (V2.VolumePosition - V0.VolumePosition);
            VolumeNormal = Vector3.Cross(Tangent01, Tangent02).Normalized();
        }

        public void UpdateNeighbors()
        {
            Neighbors.Clear();

            foreach (Triangle t in V0.Triangles)
                if (t != this && !Neighbors.Contains(t))
                    Neighbors.Add(t);
            foreach (Triangle t in V1.Triangles)
                if (t != this && !Neighbors.Contains(t))
                    Neighbors.Add(t);
            foreach (Triangle t in V2.Triangles)
                if (t != this && !Neighbors.Contains(t))
                    Neighbors.Add(t);
        }

        /// <summary>
        /// Adapted from https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
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
                I.Triangle = this;
                return I;
            }

            // No hit, no win
            return null;
        }

        public float GetArea()
        {
            Vector3 E0 = V1.Position - V0.Position;
            Vector3 E1 = V2.Position - V0.Position;
            Vector3 E2 = V1.Position - V2.Position;

            float L0 = E0.Length, L1 = E1.Length, L2 = E2.Length;

            float S = (L0 + L1 + L2) * 0.5f;
            return (float)Math.Sqrt(S * (S - L0) * (S - L1) * (S - L2));
        }
    }

    public class Intersection
    {
        public Vector3 Position;
        public float Distance;
        public Ray3 Ray;
        public Triangle Triangle;
    }
}
