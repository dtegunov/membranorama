using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    public class Edge
    {
        public Vertex Source, Target;

        public Edge(Vertex source, Vertex target)
        {
            Source = source;
            Target = target;
        }

        public Vector3 GetTangent()
        {
            return (Target.Position - Source.Position).Normalized();
        }

        public float GetLength()
        {
            return (Target.Position - Source.Position).Length;
        }

        public float GetOffsetLength(float offset)
        {
            return (Target.GetOffset(offset).Position - Source.GetOffset(offset).Position).Length;
        }

        public Vector3 GetVolumeTangent()
        {
            return (Target.VolumePosition - Source.VolumePosition).Normalized();
        }

        public float GetVolumeLength()
        {
            return (Target.VolumePosition - Source.VolumePosition).Length;
        }

        public float GetVolumeOffsetLength(float offset)
        {
            return (Target.GetVolumeOffset(offset).Position - Source.GetVolumeOffset(offset).Position).Length;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                Edge Obj = obj as Edge;
                return (Obj.Source == this.Source && Obj.Target == this.Target) || (Obj.Source == this.Target && Obj.Target == this.Source);
            }
        }

        public override int GetHashCode()
        {
            int Hash1 = Source.GetHashCode();
            int Hash2 = Target.GetHashCode();
            return Math.Min(Hash1, Hash2) ^ Math.Max(Hash1, Hash2);
        }

        public static bool operator ==(Edge a, Edge b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(Edge a, Edge b)
        {
            return !(a == b);
        }
    }
}
