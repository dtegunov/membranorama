using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Membranogram
{
    public class Options : DataBase
    {
        private int _SurfaceOffset = 0;
        public int SurfaceOffset
        {
            get { return _SurfaceOffset; }
            set { if (value != _SurfaceOffset) { _SurfaceOffset = value; OnPropertyChanged(); } }
        }

        private Vector3 _PixelScale = new Vector3(1);
        public Vector3 PixelScale
        {
            get { return _PixelScale; }
            set { if (value != _PixelScale) { _PixelScale = value; OnPropertyChanged(); } }
        }

        private Vector3 _VolumeOffset = new Vector3(0);
        public Vector3 VolumeOffset
        {
            get { return _VolumeOffset; }
            set { if (value != _VolumeOffset) { _VolumeOffset = value; OnPropertyChanged(); } }
        }

        private int _TraceDepthOffset = -4;
        public int TraceDepthOffset
        {
            get { return _TraceDepthOffset; }
            set { if (value != _TraceDepthOffset) { _TraceDepthOffset = value; OnPropertyChanged(); } }
        }

        private int _TraceDepth = 8;
        public int TraceDepth
        {
            get { return _TraceDepth; }
            set { if (value != _TraceDepth) { _TraceDepth = value; OnPropertyChanged(); } }
        }

        private string _PathModel = "";
        public string PathModel
        {
            get { return _PathModel; }
            set { if (value != _PathModel) { _PathModel = value; OnPropertyChanged(); } }
        }

        private string _PathTomogram = "";
        public string PathTomogram
        {
            get { return _PathTomogram; }
            set { if (value != _PathTomogram) { _PathTomogram = value; OnPropertyChanged(); } }
        }

        private decimal _OutputRangeMin = 0M;
        public decimal OutputRangeMin
        {
            get { return _OutputRangeMin; }
            set { if (value != _OutputRangeMin) { _OutputRangeMin = value; OnPropertyChanged(); } }
        }

        private decimal _OutputRangeMax = 1M;
        public decimal OutputRangeMax
        {
            get { return _OutputRangeMax; }
            set { if (value != _OutputRangeMax) { _OutputRangeMax = value; OnPropertyChanged(); } }
        }

        private int _OutputLight = 30;
        public int OutputLight
        {
            get { return _OutputLight; }
            set { if (value != _OutputLight) { _OutputLight = value; OnPropertyChanged(); } }
        }

        private int _SelectionAngle = 90;
        public int SelectionAngle
        {
            get { return _SelectionAngle; }
            set { if (value != _SelectionAngle) { _SelectionAngle = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<SurfacePoint> _SurfacePoints = new ObservableCollection<SurfacePoint>();
        public ObservableCollection<SurfacePoint> SurfacePoints
        {
            get { return _SurfacePoints; }
            set { if (value != _SurfacePoints) { _SurfacePoints = value; OnPropertyChanged(); } }
        }

        private Viewport _Viewport = null;
        public Viewport Viewport
        {
            get { return _Viewport; }
            set { if (value != _Viewport) { _Viewport = value; OnPropertyChanged(); } }
        }

        public Options()
        {
            _Viewport = new Viewport();
            _Viewport.PropertyChanged += _Viewport_PropertyChanged;
        }

        void _Viewport_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged();
        }
    }
}
