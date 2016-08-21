using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    public class Vertex
    {
        public int ID;

        /// <summary>
        /// Position in rendering space, i. e. what goes into gl_Position in the vertex shader.
        /// </summary>
        public Vector3 Position = new Vector3();

        /// <summary>
        /// Normal in rendering space, i. e. what will be used for lighting calculations by most shaders.
        /// </summary>
        public Vector3 Normal = new Vector3(1f, 0f, 0f);

        /// <summary>
        /// Position in UNSCALED tomogram space. The values are still in Angstrom when they are passed to the shaders, and are scaled there.
        /// </summary>
        public Vector3 VolumePosition = new Vector3();

        /// <summary>
        /// Normal in tomogram space, i. e. what will be used to compute updated tomogram-space coordinates for offset surfaces.
        /// </summary>
        public Vector3 VolumeNormal = new Vector3(1f, 0f, 0f);

        public List<Triangle> Triangles = new List<Triangle>();
        public List<Edge> Edges = new List<Edge>();
        public List<Vertex> Neighbors = new List<Vertex>(); 
        
        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            VolumePosition = new Vector3(position);
            Normal = normal;
            VolumeNormal = new Vector3(normal);
        }

        /// <summary>
        /// Gets the average of the normals of all triangles containing this vertex.
        /// </summary>
        public Vector3 SmoothNormal
        {
            get
            {
                if (Triangles.Count == 0)
                    return Normal;
                else
                {
                    Vector3 Sum = new Vector3();
                    foreach (Triangle t in Triangles)
                        Sum += t.Normal;
                    return Sum.Normalized();
                }
            }
        }

        /// <summary>
        /// Gets the average of the normals of all triangles containing this vertex in tomogram space.
        /// </summary>
        public Vector3 SmoothVolumeNormal
        {
            get
            {
                if (Triangles.Count == 0)
                    return VolumeNormal;
                else
                {
                    Vector3 Sum = new Vector3();
                    foreach (Triangle t in Triangles)
                        Sum += t.VolumeNormal;
                    return Sum.Normalized();
                }
            }
        }

        public void UpdateNeighbors()
        {
            Neighbors.Clear();
            foreach (var e in Edges)
            {
                if (e.Source == this)
                    Neighbors.Add(e.Target);
                else
                    Neighbors.Add(e.Source);
            }
        }

        public Vertex GetOffset(float offset)
        {
            return new Vertex(Position + SmoothNormal * offset, Normal);
        }

        public Vertex GetVolumeOffset(float offset)
        {
            return new Vertex(VolumePosition + SmoothVolumeNormal * offset, VolumeNormal);
        }
    }
}
