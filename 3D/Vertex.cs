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
        public Vector3 Position = new Vector3();
        public Vector3 Normal = new Vector3(1f, 0f, 0f);
        public Vector3 VolumePosition = new Vector3();
        public Vector3 VolumeNormal = new Vector3(1f, 0f, 0f);
        public List<Triangle> Triangles = new List<Triangle>();

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
        
        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            VolumePosition = new Vector3(position);
            Normal = normal;
            VolumeNormal = new Vector3(normal);
        }
    }
}
