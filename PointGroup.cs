using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OpenTK;

namespace Membranogram
{
    public class PointGroup : DataBase, IDisposable
    {
        private ObservableCollection<SurfacePoint> _Points = new ObservableCollection<SurfacePoint>();
        public ObservableCollection<SurfacePoint> Points
        {
            get { return _Points; }
            set { if (value != _Points) { _Points = value; OnPropertyChanged(); } }
        }

        private Color _Color = Colors.CornflowerBlue;
        public Color Color
        {
            get { return _Color; }
            set
            {
                if (value != _Color)
                {
                    _Color = value; OnPropertyChanged();
                    foreach (SurfacePoint point in _Points)
                        point.Color = new Vector4(_Color.ScR, _Color.ScG, _Color.ScB, _Color.ScA);
                    if (_Points.Count > 0)
                        PointCloud.UpdateBuffers();
                }
            }
        }

        private string _Name = "";
        public string Name
        {
            get { return _Name; }
            set { if (value != _Name) { _Name = value; OnPropertyChanged(); } }
        }

        private bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set { if (value != _IsVisible) { _IsVisible = value; OnPropertyChanged(); } }
        }

        private decimal _Size = 3;
        public decimal Size
        {
            get { return _Size; }
            set { if (value != _Size) { _Size = value; OnPropertyChanged(); } }
        }

        private int _PointCount = 0;
        public int PointCount
        {
            get { return _PointCount; }
            set { if (value != _PointCount) { _PointCount = value; OnPropertyChanged(); } }
        }

        public PointCloud PointCloud = new PointCloud();

        private PointDepiction _Depiction = PointDepiction.Box;
        public PointDepiction Depiction
        {
            get { return _Depiction; }
            set
            {
                if (value != _Depiction)
                {
                    _Depiction = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsDepictionBox");
                    OnPropertyChanged("IsDepictionMesh");
                    OnPropertyChanged("IsDepictionLocalSurface");
                    UpdateDepictionString();
                }
            }
        }

        public bool IsDepictionBox
        {
            get { return Depiction == PointDepiction.Box; }
            set { if (value) Depiction = PointDepiction.Box; }
        }

        public bool IsDepictionMesh
        {
            get { return Depiction == PointDepiction.Mesh; }
            set { if (value) Depiction = PointDepiction.Mesh; }
        }

        public bool IsDepictionLocalSurface
        {
            get { return Depiction == PointDepiction.LocalSurface; }
            set { if (value) Depiction = PointDepiction.LocalSurface; }
        }

        private string _DepictionString = "Box";
        public string DepictionString
        {
            get { return _DepictionString; }
            set { if (value != _DepictionString) { _DepictionString = value; OnPropertyChanged(); } }
        }
        private void UpdateDepictionString()
        {
            if (Depiction == PointDepiction.Box)
                DepictionString = "Box";
            else if (Depiction == PointDepiction.Mesh)
                DepictionString = _DepictionMeshPath.Length > 0 ? _DepictionMeshPath.Substring(_DepictionMeshPath.LastIndexOf("\\") + 1) : "Mesh";
            else if (Depiction == PointDepiction.LocalSurface)
                DepictionString = "Isosurface";
        }

        private string _DepictionMeshPath = "";
        public string DepictionMeshPath
        {
            get { return !string.IsNullOrEmpty(_DepictionMeshPath) ? _DepictionMeshPath : "Select File..."; }
            set { if (value != _DepictionMeshPath) { _DepictionMeshPath = value; OnPropertyChanged(); UpdateDepictionString(); } }
        }

        private Mesh _DepictionMesh = null;
        public Mesh DepictionMesh
        {
            get { return _DepictionMesh; }
            set { if (value != _DepictionMesh) { _DepictionMesh = value; OnPropertyChanged(); } }
        }

        private decimal _DepictionMeshLevel = 0.02M;
        public decimal DepictionMeshLevel
        {
            get { return _DepictionMeshLevel; }
            set { if (value != _DepictionMeshLevel) { _DepictionMeshLevel = value; OnPropertyChanged(); } }
        }

        private decimal _DepictionMeshOffset = 0;
        public decimal DepictionMeshOffset
        {
            get { return _DepictionMeshOffset; }
            set { if (value != _DepictionMeshOffset) { _DepictionMeshOffset = value; OnPropertyChanged(); } }
        }

        private decimal _DepictionLocalSurfaceLevel = 0.02M;
        public decimal DepictionLocalSurfaceLevel
        {
            get { return _DepictionLocalSurfaceLevel; }
            set { if (value != _DepictionLocalSurfaceLevel) { _DepictionLocalSurfaceLevel = value; OnPropertyChanged(); } }
        }

        private bool _DepictionLocalSurfaceInvert = true;
        public bool DepictionLocalSurfaceInvert
        {
            get { return _DepictionLocalSurfaceInvert; }
            set { if (value != _DepictionLocalSurfaceInvert) { _DepictionLocalSurfaceInvert = value; OnPropertyChanged(); } }
        }

        private bool _DepictionLocalSurfaceOnlyCenter = true;
        public bool DepictionLocalSurfaceOnlyCenter
        {
            get { return _DepictionLocalSurfaceOnlyCenter; }
            set { if (value != _DepictionLocalSurfaceOnlyCenter) { _DepictionLocalSurfaceOnlyCenter = value; OnPropertyChanged(); } }
        }

        public PointGroup()
        {
            _Points.CollectionChanged += _Points_CollectionChanged;
            PropertyChanged += PointGroup_PropertyChanged;
        }

        private void PointGroup_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Size" ||
                e.PropertyName == "Depiction" ||
                e.PropertyName == "DepictionMeshPath" ||
                e.PropertyName == "DepictionMesh" ||
                e.PropertyName == "DepictionMeshLevel" ||
                e.PropertyName == "DepictionLocalSurfaceLevel" ||
                e.PropertyName == "DepictionLocalSurfaceInvert" ||
                e.PropertyName == "DepictionLocalSurfaceOnlyCenter")
                UpdateDepiction();
            else if (e.PropertyName == "DepictionMeshOffset")
                MainWindow.Options.Viewport.Redraw();
        }

        void _Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                if (Depiction == PointDepiction.LocalSurface)
                    foreach (var oldItem in e.OldItems)
                        ((SurfacePoint)oldItem).DepictionMesh?.Dispose();
                PointCloud.RemovePoints(e.OldItems.Cast<SurfacePoint>());
            }

            if (e.NewItems != null)
            {
                foreach (SurfacePoint point in e.NewItems)
                {
                    point.Group = this;
                    point.Color = new Vector4(_Color.ScR, _Color.ScG, _Color.ScB, _Color.ScA);
                }
                PointCloud.AddPoints(e.NewItems.Cast<SurfacePoint>());
            }

            PointCount = Points.Count;

            if (Depiction == PointDepiction.LocalSurface)
                UpdateDepiction();
        }

        public void CopyPropertiesFrom(PointGroup g)
        {
            Depiction = g.Depiction;
            DepictionMeshPath = g.DepictionMeshPath;
            DepictionMeshLevel = g.DepictionMeshLevel;
            DepictionMeshOffset = g.DepictionMeshOffset;
            DepictionLocalSurfaceLevel = g.DepictionLocalSurfaceLevel;
            DepictionLocalSurfaceInvert = g.DepictionLocalSurfaceInvert;
            DepictionLocalSurfaceOnlyCenter = g.DepictionLocalSurfaceOnlyCenter;

            Color = g.Color;
            Size = g.Size;
        }

        public void Dispose()
        {
            PointCloud.FreeBuffers();
        }

        public List<Intersection> Intersect(Ray3 ray)
        {
            List<Intersection> Intersections = new List<Intersection>();
            float ScaledSize = (float)Size * MainWindow.Options.PixelScale.X / 2f;
            if (Depiction == PointDepiction.Mesh && DepictionMesh != null)
            {
                ScaledSize = DepictionMesh.BoundingBoxCorners[7].X;
                ScaledSize = Math.Max(ScaledSize, DepictionMesh.BoundingBoxCorners[7].Y);
                ScaledSize = Math.Max(ScaledSize, DepictionMesh.BoundingBoxCorners[7].Z);
            }

            Parallel.ForEach(Points, p =>
            {
                float Distance = p.DistanceFromRay(ray);
                if (Distance <= ScaledSize)
                    lock (Intersections)
                        Intersections.Add(new Intersection { Position = p.Position, Distance = Distance, Ray = ray, Target = p });
            });

            return Intersections;
        }

        public void UpdateDepiction()
        {
            if (DepictionMesh != null)
            {
                DepictionMesh.Dispose();
                DepictionMesh = null;
            }
            foreach (var point in Points)
                point.DropDepictionMesh();

            //if (Points.Count == 0)
                //return;

            if (Depiction == PointDepiction.Mesh && File.Exists(DepictionMeshPath))
            {
                FileInfo Info = new FileInfo(DepictionMeshPath);
                if (Info.Extension.ToLower().Contains("mrc"))
                {
                    HeaderMRC VolumeHeader = (HeaderMRC)MapHeader.ReadFromFile(DepictionMeshPath);
                    float[] VolumeData = IOHelper.ReadMapFloat(DepictionMeshPath);

                    Mesh NewMesh = Mesh.FromVolume(VolumeData, VolumeHeader.Dimensions, VolumeHeader.Pixelsize.X, (float)DepictionMeshLevel);
                    NewMesh.UsedComponents = MeshVertexComponents.Position | MeshVertexComponents.Normal;
                    NewMesh.GLContext = MainWindow.Options.Viewport.GetControl();
                    NewMesh.UpdateBuffers();

                    _DepictionMesh = NewMesh;
                }
                else if (Info.Extension.ToLower().Contains("obj"))
                {
                    Mesh NewMesh = Mesh.FromOBJ(DepictionMeshPath, true);
                    NewMesh.UsedComponents = MeshVertexComponents.Position | MeshVertexComponents.Normal;
                    NewMesh.GLContext = MainWindow.Options.Viewport.GetControl();
                    NewMesh.UpdateBuffers();

                    _DepictionMesh = NewMesh;
                }
            }
            else if (Depiction == PointDepiction.LocalSurface && MainWindow.Options.Membrane.TomogramTexture != null)
            {
                int3 DimsExtract = new int3((int)Size + 2, (int)Size + 2, (int)Size + 2);

                Parallel.ForEach(Points, point =>
                {
                    Vector3 TomoPos = (point.Position - MainWindow.Options.Membrane.TomogramTexture.Offset) / MainWindow.Options.PixelScale.X;
                    int3 TomoPosInt = new int3((int)Math.Round(TomoPos.X), (int)Math.Round(TomoPos.Y), (int)Math.Round(TomoPos.Z));

                    float[] LocalVol = Helper.Extract(MainWindow.Options.Membrane.TomogramTexture.OriginalData,
                                                      MainWindow.Options.Membrane.TomogramTexture.Size,
                                                      TomoPosInt,
                                                      DimsExtract);

                    if (DepictionLocalSurfaceInvert)
                        for (int i = 0; i < LocalVol.Length; i++)
                            LocalVol[i] = -LocalVol[i];

                    for (int z = 0; z < DimsExtract.Z; z++)
                        for (int y = 0; y < DimsExtract.Y; y++)
                            for (int x = 0; x < DimsExtract.X; x++)
                                if (z == 0 || y == 0 || x == 0 || z == DimsExtract.Z - 1 || y == DimsExtract.Y - 1 || x == DimsExtract.X - 1)
                                    LocalVol[(z * DimsExtract.Y + y) * DimsExtract.X + x] = -99999;

                    bool[] Mask = new bool[LocalVol.Length];
                    float Threshold = (float)DepictionLocalSurfaceLevel;
                    if (DepictionLocalSurfaceOnlyCenter)
                    {
                        int MostCentralID = -1;
                        float MostCentralDist = DimsExtract.X * DimsExtract.X;

                        // Find most central valid pixel in the local window to start mask expansion from there.
                        for (int z = 1; z < DimsExtract.Z - 1; z++)
                        {
                            int zz = z - DimsExtract.Z / 2;
                            zz *= zz;
                            for (int y = 1; y < DimsExtract.Y - 1; y++)
                            {
                                int yy = y - DimsExtract.Y / 2;
                                yy *= yy;
                                for (int x = 1; x < DimsExtract.X - 1; x++)
                                {
                                    if (LocalVol[(z * DimsExtract.Y + y) * DimsExtract.X + x] >= Threshold)
                                    {
                                        int xx = x - DimsExtract.X / 2;
                                        xx *= xx;
                                        float r = xx + yy + zz;
                                        if (r < MostCentralDist)
                                        {
                                            MostCentralDist = r;
                                            MostCentralID = (z * DimsExtract.Y + y) * DimsExtract.X + x;
                                        }
                                    }
                                }
                            }
                        }
                        if (MostCentralID < 0) // Volume doesn't contain voxels above threshold
                            return;

                        Mask[MostCentralID] = true;

                        for (int mi = 0; mi < Size / 2; mi++)
                        {
                            bool[] NextMask = new bool[Mask.Length];

                            for (int z = 1; z < DimsExtract.Z - 1; z++)
                                for (int y = 1; y < DimsExtract.Y - 1; y++)
                                    for (int x = 1; x < DimsExtract.X - 1; x++)
                                    {
                                        int ID = (z * DimsExtract.Y + y) * DimsExtract.X + x;
                                        if (LocalVol[ID] >= Threshold)
                                            if (Mask[ID] ||
                                                Mask[ID + 1] ||
                                                Mask[ID - 1] ||
                                                Mask[ID + DimsExtract.X] ||
                                                Mask[ID - DimsExtract.X] ||
                                                Mask[ID + DimsExtract.Y * DimsExtract.X] ||
                                                Mask[ID - DimsExtract.Y * DimsExtract.X])
                                                NextMask[ID] = true;
                                    }

                            Mask = NextMask;
                        }
                    }
                    else
                        for (int i = 0; i < Mask.Length; i++)
                            Mask[i] = true;

                    // Apply spherical mask
                    int Size2 = (int)(Size * Size / 4);
                    for (int z = 1; z < DimsExtract.Z - 1; z++)
                    {
                        int zz = z - DimsExtract.Z / 2;
                        zz *= zz;
                        for (int y = 1; y < DimsExtract.Y - 1; y++)
                        {
                            int yy = y - DimsExtract.Y / 2;
                            yy *= yy;
                            for (int x = 1; x < DimsExtract.X - 1; x++)
                            {
                                int xx = x - DimsExtract.X / 2;
                                xx *= xx;
                                int r2 = xx + yy + zz;

                                Mask[(z * DimsExtract.Y + y) * DimsExtract.X + x] &= r2 < Size2;
                            }
                        }
                    }

                    for (int i = 0; i < Mask.Length; i++)
                        if (!Mask[i])
                            LocalVol[i] = Math.Min(LocalVol[i], Threshold - 1e-5f);

                    //IOHelper.WriteMapFloat("d_extract.mrc", HeaderMRC.ReadFromFile("test_extract.mrc"), LocalVol);
                    //IOHelper.WriteMapFloat("d_original.mrc", HeaderMRC.ReadFromFile("Tomo1L1_bin4.mrc"), MainWindow.Options.Membrane.TomogramTexture.OriginalData);

                    point.DepictionMesh = Mesh.FromVolume(LocalVol,
                                                          DimsExtract,
                                                          MainWindow.Options.PixelScale.X,
                                                          (float)DepictionLocalSurfaceLevel);
                    point.DepictionMesh.UsedComponents = MeshVertexComponents.Position | MeshVertexComponents.Normal;
                });

                foreach (var point in Points)
                {
                    if (point.DepictionMesh == null)
                        continue;
                    point.DepictionMesh.GLContext = MainWindow.Options.Viewport.GetControl();
                    point.DepictionMesh.UpdateBuffers();
                }
            }

            MainWindow.Options.Viewport.Redraw();
        }
    }

    public enum PointDepiction
    {
        Box = 1,
        Mesh = 2,
        LocalSurface = 3
    }
}
