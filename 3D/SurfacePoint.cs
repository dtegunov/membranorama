using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    public class SurfacePoint : DataBase
    {
        private string _Text = "";
        public string Text
        {
            get { return _Text; }
            set { if (value != _Text) { _Text = value; OnPropertyChanged(); } }
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { if (value != _IsSelected) { _IsSelected = value; OnPropertyChanged(); } }
        }

        public Vector3 Position;
        public Triangle Face;

        public SurfacePoint(Vector3 position, Triangle face)
        {
            Position = position;
            Face = face;
            Text = String.Format("{0:0.00}, {1:0.00}, {2:0.00} Å", Position.X, Position.Y, Position.Z);
        }
    }
}
