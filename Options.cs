using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Warp;

namespace Membranogram
{
    public class Options : DataBase
    {
        private Membrane _Membrane;
        public Membrane Membrane
        {
            get { return _Membrane; }
            set { if (value != _Membrane) { _Membrane = value; OnPropertyChanged(); } }
        }

        private Vector3 _PixelScale = new Vector3(1);
        public Vector3 PixelScale
        {
            get { return _PixelScale; }
            set { if (value != _PixelScale) { _PixelScale = value; OnPropertyChanged(); } }
        }

        private Viewport _Viewport = null;
        public Viewport Viewport
        {
            get { return _Viewport; }
            set { if (value != _Viewport) { _Viewport = value; OnPropertyChanged(); } }
        }

        public Options()
        {
        }
    }
}
