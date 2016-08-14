using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Accord.Math.Optimization;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class SurfacePatch : DataBase
    {
        private static object RenderSync = new object();

        private Membrane Membrane;

        public Mesh SurfaceMesh;
        public Wireframe SurfaceWireframe;
        private List<PointGroup> PointGroups = new List<PointGroup>(); 

        public Dictionary<Triangle, Triangle> OriginalToTransformed = new Dictionary<Triangle, Triangle>();
        public Dictionary<Triangle, Triangle> TransformedToOriginal = new Dictionary<Triangle, Triangle>();

        private Viewport _Viewport;
        public Viewport Viewport
        {
            get { return _Viewport; }
            set { if (value != _Viewport) { _Viewport = value; OnPropertyChanged(); } }
        }
        public PatchWindow ParentWindow;

        private string _Name = "";
        public string Name
        {
            get { return _Name; }
            set { if (value != _Name) { _Name = value; OnPropertyChanged(); } }
        }

        private Color _Color;
        public Color Color
        {
            get { return _Color; }
            set
            {
                if (value != _Color)
                {
                    _Color = value;
                    OnPropertyChanged();
                    UpdateMembraneTriangles();
                    OpaqueColor = Color.FromArgb(128, value.R, value.G, value.B);
                }
            }
        }

        private Color _OpaqueColor;
        public Color OpaqueColor
        {
            get { return _OpaqueColor; }
            set { if (value != _OpaqueColor) { _OpaqueColor = value; OnPropertyChanged(); } }
        }

        private bool _IsColored = true;
        public bool IsColored
        {
            get { return _IsColored; }
            set { if (value != _IsColored) { _IsColored = value; OnPropertyChanged(); UpdateMembraneTriangles(); } }
        }

        private string _SurfaceArea = "";
        public string SurfaceArea
        {
            get { return _SurfaceArea; }
            set { if (value != _SurfaceArea) { _SurfaceArea = value; OnPropertyChanged(); } }
        }

        private bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set { if (value != _IsVisible) { _IsVisible = value; OnPropertyChanged(); UpdateMembraneTriangles(); } }
        }

        private bool _IsMouseOver = false;
        public bool IsMouseOver
        {
            get { return _IsMouseOver; }
            set { if (value != _IsMouseOver) { _IsMouseOver = value; OnPropertyChanged(); } }
        }

        private SurfacePatch _LockTarget = null;
        public SurfacePatch LockTarget
        {
            get { return _LockTarget; }
            set
            {
                if (value != _LockTarget)
                {
                    UnlockFrom(_LockTarget);
                    _LockTarget = value;
                    LockTo(_LockTarget);
                    OnPropertyChanged();
                    UpdateRMSD();
                }
            }
        }

        private bool _IsLocked = false;
        public bool IsLocked
        {
            get { return _IsLocked; }
            set
            {
                if (value != _IsLocked)
                {
                    _IsLocked = value;
                    if (_IsLocked)
                        LockTo(_LockTarget);
                    else
                        UnlockFrom(_LockTarget);
                    OnPropertyChanged();
                    UpdateRMSD();
                }
            }
        }

        private bool _IsLockedCamera = true;
        public bool IsLockedCamera
        {
            get { return _IsLockedCamera; }
            set { if (value != _IsLockedCamera) { _IsLockedCamera = value; OnPropertyChanged(); MatchCamera(); } }
        }

        private bool _IsLockedPosition = false;
        public bool IsLockedPosition
        {
            get { return _IsLockedPosition; }
            set { if (value != _IsLockedPosition) { _IsLockedPosition = value; OnPropertyChanged(); MatchPosition(); UpdateRMSD(); } }
        }

        private Dictionary<Vertex, Triangle[]> VertexLocks = new Dictionary<Vertex, Triangle[]>(); 

        private ObservableCollection<SurfacePatch> _DisplayedPatches = new ObservableCollection<SurfacePatch>();
        public ObservableCollection<SurfacePatch> DisplayedPatches
        {
            get { return _DisplayedPatches; }
            set { if (value != _DisplayedPatches) { _DisplayedPatches = value; OnPropertyChanged(); } }
        }

        #region Surface modification options

        private decimal _SurfaceOffset = 0M;
        public decimal SurfaceOffset
        {
            get { return _SurfaceOffset; }
            set
            {
                if (value != _SurfaceOffset)
                {
                    _SurfaceOffset = value;
                    OnPropertyChanged();
                    
                    SurfaceMesh?.UpdateProcessedGeometry((float)SurfaceOffset * MainWindow.Options.PixelScale.X);
                    Viewport?.Redraw();
                }
            }
        }

        #endregion

        #region Volume tracing options

        private int _TraceDepthOffset = 0;
        public int TraceDepthOffset
        {
            get { return _TraceDepthOffset; }
            set { if (value != _TraceDepthOffset) { _TraceDepthOffset = value; OnPropertyChanged(); Viewport?.Redraw(); } }
        }

        private int _TraceDepth = 0;
        public int TraceDepth
        {
            get { return _TraceDepth; }
            set { if (value != _TraceDepth) { _TraceDepth = value; OnPropertyChanged(); Viewport?.Redraw(); } }
        }

        private decimal _TraceSharpening = 0M;
        public decimal TraceSharpening
        {
            get { return _TraceSharpening; }
            set { if (value != _TraceSharpening) { _TraceSharpening = value; OnPropertyChanged(); Viewport?.Redraw(); } }
        }

        #endregion

        #region Rendering output options

        private decimal _OutputRangeMin = 0M;
        public decimal OutputRangeMin
        {
            get { return _OutputRangeMin; }
            set { if (value != _OutputRangeMin) { _OutputRangeMin = value; OnPropertyChanged(); Viewport?.Redraw(); } }
        }

        private decimal _OutputRangeMax = 1M;
        public decimal OutputRangeMax
        {
            get { return _OutputRangeMax; }
            set { if (value != _OutputRangeMax) { _OutputRangeMax = value; OnPropertyChanged(); Viewport?.Redraw(); } }
        }

        #endregion

        #region Planarization

        private Thread PlanarizationThread;
        private bool PlanarizationRunning = false;
        private List<Tuple<Vertex, float>[]> PlanarizationAccel; 

        private decimal _ShapePreservation = 1M;
        public decimal ShapePreservation
        {
            get { return _ShapePreservation; }
            set { if (value != _ShapePreservation) { _ShapePreservation = value; OnPropertyChanged(); } }
        }

        private decimal _MeanFaceAngle = 0;
        public decimal MeanFaceAngle
        {
            get { return _MeanFaceAngle; }
            set { if (value != _MeanFaceAngle) { _MeanFaceAngle = value; OnPropertyChanged(); } }
        }

        private decimal _MeanEdgeError = 0;
        public decimal MeanEdgeError
        {
            get { return _MeanEdgeError; }
            set { if (value != _MeanEdgeError) { _MeanEdgeError = value; OnPropertyChanged(); } }
        }

        #endregion

        public event Action<SurfacePatch, List<Intersection>, System.Windows.Forms.MouseEventArgs> MouseEnter;
        public event Action<SurfacePatch, List<Intersection>, System.Windows.Forms.MouseEventArgs> MouseMove;
        public event Action<SurfacePatch, System.Windows.Forms.MouseEventArgs> MouseLeave;
        public event Action<SurfacePatch, List<Intersection>, System.Windows.Forms.MouseEventArgs> MouseClick;

        public event Action<Membrane, List<Triangle>> TriangleSelectionChanged; 

        public SurfacePatch(Membrane membrane, string name, Color color, IEnumerable<Triangle> triangles)
        {
            Membrane = membrane;
            Name = name;
            Color = color;

            Dictionary<Vertex, Vertex> VertexToTransformed = new Dictionary<Vertex, Vertex>();
            List<Triangle> NewTriangles = new List<Triangle>(triangles.Count());

            foreach (var t in triangles)
            {
                foreach (var v in t.Vertices)
                    if (!VertexToTransformed.ContainsKey(v))
                        VertexToTransformed.Add(v, new Vertex(v.VolumePosition, v.VolumeNormal));

                Triangle NewTriangle = new Triangle(t.ID, VertexToTransformed[t.V0], VertexToTransformed[t.V1], VertexToTransformed[t.V2]);
                NewTriangles.Add(NewTriangle);
                OriginalToTransformed.Add(t, NewTriangle);
                TransformedToOriginal.Add(NewTriangle, t);
            }

            SurfaceMesh = new Mesh();
            SurfaceMesh.Vertices.AddRange(VertexToTransformed.Values);
            SurfaceMesh.Triangles.AddRange(OriginalToTransformed.Values);

            SurfaceMesh.UpdateGraph();
            SurfaceMesh.UpdateVertexIDs();

            TurnUpsideUp();
            // Don't update buffers because there is no OpenGL context yet.

            UpdateStats();
            UpdatePlanarizationStats();

            Membrane.DisplayedPatches.CollectionChanged += MembraneDisplayedPatches_CollectionChanged;
            Membrane.PointGroups.CollectionChanged += MembranePointGroups_CollectionChanged;
            MembranePointGroups_CollectionChanged(null, null);
        }

        private void TurnUpsideUp()
        {
            Vector3 Centroid = SurfaceMesh.GetCentroid();
            foreach (Vertex v in SurfaceMesh.Vertices)
                v.Position -= Centroid;

            #region Fit plane to vertices by calculating SVD:

            double[,] ForSVD = new double[3, SurfaceMesh.Vertices.Count];
            for (int i = 0; i < SurfaceMesh.Vertices.Count; i++)
            {
                ForSVD[0, i] = SurfaceMesh.Vertices[i].Position.X;
                ForSVD[1, i] = SurfaceMesh.Vertices[i].Position.Y;
                ForSVD[2, i] = SurfaceMesh.Vertices[i].Position.Z;
            }

            double[] W;
            double[,] U, V;

            alglib.rmatrixsvd(ForSVD, 3, SurfaceMesh.Vertices.Count, 2, 0, 2, out W, out U, out V);

            Vector3 FitNormal = new Vector3((float)U[0, 2], (float)U[1, 2], (float)U[2, 2]);

            // See if SVD normal is pointing in the opposite direction
            Vector3 MeanNormal = new Vector3();
            foreach (var t in SurfaceMesh.Triangles)
                MeanNormal += t.VolumeNormal;
            MeanNormal.Normalize();
            if (Vector3.Dot(MeanNormal, FitNormal) < 0f)
                FitNormal *= -1f;

            float Phi = (float)Math.Atan2(FitNormal.Y, FitNormal.X) + (float)Math.PI * 1.5f;
            float Theta = (float)Math.Acos(FitNormal.Z);
            if (Math.Min(Math.Abs(Theta), Math.Abs(Theta - (float)Math.PI)) < Helper.ToRad(0.5f))
                return;

            Matrix4 BackRotate = Matrix4.CreateFromAxisAngle(new Vector3((float)Math.Cos(Phi), (float)Math.Sin(Phi), 0), Theta);
            foreach (Vertex v in SurfaceMesh.Vertices)
                v.Position = Vector3.Transform(v.Position, BackRotate);

            foreach (Triangle t in SurfaceMesh.Triangles)
            {
                t.UpdateNormal();
                t.UpdateVolumeNormal();
            }

            #endregion

            SurfaceMesh.UpdateProcessedGeometry(0f);
        }

        private void MembraneDisplayedPatches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var newItem in e.NewItems.Cast<SurfacePatch>().Where(item => item != this && !DisplayedPatches.Contains(item)))
                    DisplayedPatches.Add(newItem);

            if (e.OldItems != null)
                foreach (var oldItem in e.OldItems.Cast<SurfacePatch>().Where(item => DisplayedPatches.Contains(item)))
                    DisplayedPatches.Remove(oldItem);
        }

        private void MembranePointGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PointGroups.Clear();
            PointGroups.AddRange(Membrane.PointGroups.Where(group => group.Points.Any(point => OriginalToTransformed.Keys.Contains(point.Face))));
            UpdatePointGroups();
        }

        public void AttachToViewport(Viewport viewport)
        {
            DetachFromViewport();

            Viewport = viewport;
            Viewport.MouseClick += Viewport_MouseClick;
            Viewport.MouseMove += Viewport_MouseMove;
            Viewport.Paint += Viewport_Paint;

            SurfaceMesh.GLContext = viewport.GetControl();
            SurfaceMesh.UpdateBuffers();
            foreach (var group in PointGroups)
                group.PointCloud.UpdateBuffers();
        }

        public void DetachFromViewport()
        {
            if (Viewport == null)
                return;

            Viewport.MouseClick -= Viewport_MouseClick;
            Viewport.MouseMove -= Viewport_MouseMove;
            Viewport.Paint -= Viewport_Paint;

            Viewport.GetControl().MakeCurrent();
            SurfaceMesh.FreeBuffers();
            foreach (var group in PointGroups)
                group.PointCloud.FreeBuffers();

            Viewport = null;
            LockTarget = null;
        }

        private void Viewport_MouseMove(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));

            if (Intersections.Count == 0 && IsMouseOver)
            {
                IsMouseOver = false;
                MouseLeave?.Invoke(this, e);
            }
            else if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                if (!IsMouseOver)
                {
                    IsMouseOver = true;
                    MouseEnter?.Invoke(this, Intersections, e);
                }

                MouseMove?.Invoke(this, Intersections, e);
            }
        }

        private void Viewport_MouseClick(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));

            if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                MouseClick?.Invoke(this, Intersections, e);
            }
        }

        private void Viewport_Paint()
        {
            lock (RenderSync)
            {
                Viewport.MakeCurrent();

                Membrane.MeshProgram.Use();
                {
                    Membrane.MeshProgram.SetUniform("worldViewProjMatrix", Viewport.Camera.GetViewProj());
                    Membrane.MeshProgram.SetUniform("surfaceOffset",
                        (float) SurfaceOffset * MainWindow.Options.PixelScale.X);

                    if (Membrane.TomogramTexture != null)
                    {
                        Membrane.MeshProgram.SetUniform("useVolume", 1f);
                        Membrane.MeshProgram.SetUniform("volScale", Helper.Reciprocal(Membrane.TomogramTexture.Scale));
                        Membrane.MeshProgram.SetUniform("volOffset", Membrane.TomogramTexture.Offset);
                        Membrane.MeshProgram.SetUniform("texSize", Helper.Reciprocal(Membrane.TomogramTexture.Size));

                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture3D, Membrane.TomogramTexture.Handle);
                    }
                    else
                    {
                        Membrane.MeshProgram.SetUniform("useVolume", 0f);
                    }

                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, Membrane.SelectionTexture.Handle);

                    float TraceStart = TraceDepthOffset;
                    float TraceLength = TraceDepth;
                    Membrane.MeshProgram.SetUniform("traceParams", new Vector2(TraceStart, TraceLength));
                    Membrane.MeshProgram.SetUniform("traceSharpening", (float) TraceSharpening / 100f);

                    float RangeMin = (float) Math.Min(OutputRangeMin, OutputRangeMax),
                        RangeMax = (float) Math.Max(OutputRangeMin, OutputRangeMax);
                    Membrane.MeshProgram.SetUniform("normalizeParams", new Vector2(RangeMin, RangeMax - RangeMin));

                    Vector3 LightDirection = Viewport.Camera.GetDirection();
                    Membrane.MeshProgram.SetUniform("lightDirection", LightDirection);
                    Membrane.MeshProgram.SetUniform("lightIntensity", 0.0f);

                    SurfaceMesh?.Draw();
                }

                Membrane.PointProgram.Use();
                {
                    Membrane.PointProgram.SetUniform("worldViewProjMatrix",
                        MainWindow.Options.Viewport.Camera.GetViewProj());

                    // Draw back faces first, then front, to ensure correct transparency (locally)
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Front);
                    foreach (PointGroup group in PointGroups.Where(g => g.IsVisible && g.Points.Count > 0))
                    {
                        Membrane.PointProgram.SetUniform("cubeSize",
                            (float) group.Size * MainWindow.Options.PixelScale.X);
                        group.PointCloud.Draw();
                    }
                    GL.CullFace(CullFaceMode.Back);
                    foreach (PointGroup group in PointGroups.Where(g => g.IsVisible && g.Points.Count > 0))
                    {
                        Membrane.PointProgram.SetUniform("cubeSize",
                            (float) group.Size * MainWindow.Options.PixelScale.X);
                        group.PointCloud.Draw();
                    }
                    GL.Disable(EnableCap.CullFace);
                }
            }
        }

        public void UpdateStats()
        {
            float Area = SurfaceMesh.Triangles.Sum(t => t.GetVolumeArea());
            SurfaceArea = $"{Area:0}";
        }

        public void UpdateMembraneTriangles()
        {
            if (Membrane == null)
                return;

            Membrane.SetTriangleColor(OriginalToTransformed.Keys, IsColored ? Helper.ColorToVector(Color) : new Vector4(0));
            Membrane.SetTriangleVisible(OriginalToTransformed.Keys, IsVisible);
            MainWindow.Options.Viewport.Redraw();
        }

        public void UpdatePointGroups()
        {
            
        }

        public void CreateWindow()
        {
            if (ParentWindow != null)
                return;

            ParentWindow = new PatchWindow();
            ParentWindow.DataContext = this;
            ParentWindow.Closing += (sender, args) => { StopPlanarization(); ParentWindow = null; DetachFromViewport(); };
            ParentWindow.Show();

            AttachToViewport(ParentWindow.Viewport);
            Viewport.IsRollOnly = true;

            Viewport.Camera.IsOrthogonal = true;
            Viewport.Camera.CenterOn(SurfaceMesh);
        }

        private void LockTo(SurfacePatch target)
        {
            if (target == null || target.ParentWindow == null || target.Viewport == null)
                return;

            target.ParentWindow.Closing += TargetWindow_Closing;
            target.ParentWindow.SizeChanged += TargetWindow_SizeChanged;
            target.Viewport.Camera.PropertyChanged += TargetCamera_PropertyChanged;

            Random Rand = new Random(123);
            List<Vertex> ForRandom = new List<Vertex>(SurfaceMesh.Vertices);
            List<Vertex> RandomSelection = new List<Vertex>();
            for (int i = 0; i < Math.Min(50, SurfaceMesh.Vertices.Count); i++)
            {
                int Index = Rand.Next(ForRandom.Count - 1);
                RandomSelection.Add(ForRandom[Index]);
                ForRandom.RemoveAt(Index);
            }

            VertexLocks.Clear();

            foreach (var v in RandomSelection)
            {
                float ClosestDistance = float.MaxValue;
                List<Triangle> Targets = target.OriginalToTransformed.Values.ToList();
                Triangle ClosestTriangle = Targets[0];
                foreach (var t in Targets)
                {
                    float Distance = (v.VolumePosition - t.GetVolumeCenter()).Length;
                    if (Distance < ClosestDistance)
                    {
                        ClosestDistance = Distance;
                        ClosestTriangle = t;
                    }
                }

                HashSet<Triangle> Neighborhood = Triangle.Grow(new HashSet<Triangle> { ClosestTriangle });
                VertexLocks.Add(v, Neighborhood.ToArray());
            }

            MatchCamera();
            MatchPosition();
        }

        private void TargetWindow_Closing(object sender, CancelEventArgs e)
        {
            LockTarget = null;
        }

        private void TargetCamera_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MatchCamera();
        }

        private void TargetWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MatchCamera();
        }

        private void MatchCamera()
        {
            if (LockTarget == null || !IsLocked || !IsLockedCamera)
                return;
            
            ParentWindow.Width = LockTarget.ParentWindow.Width;
            ParentWindow.Height = LockTarget.ParentWindow.Height;
            Viewport.Camera.OrthogonalSize = LockTarget.Viewport.Camera.OrthogonalSize;
            Viewport.Camera.Target = LockTarget.Viewport.Camera.Target;
            Viewport.Camera.Rotation = LockTarget.Viewport.Camera.Rotation;
        }

        private void MatchPosition()
        {
            if (LockTarget == null || !IsLocked || !IsLockedPosition)
            {
                TurnUpsideUp();
                SurfaceMesh.UpdateBuffers();
                Viewport.Redraw();
                return;
            }

            Vector3[] IdealPositions = new Vector3[VertexLocks.Count];
            Vector4[] StartPositions = new Vector4[VertexLocks.Count];
            {
                int i = 0;
                foreach (var pair in VertexLocks)
                {
                    Vector3 OwnPosition = pair.Key.VolumePosition;
                    Vector3 Mean = new Vector3(0);
                    foreach (var target in pair.Value)
                    {
                        Vector3 Relative = OwnPosition - target.GetVolumeCenter();
                        Matrix4 Volume2Transformed = target.GetPlaneMatrix() * Matrix4.Transpose(target.GetVolumePlaneMatrix());
                        Relative = Vector3.Transform(Relative, Matrix4.Transpose(Volume2Transformed));
                        Relative += target.GetCenter();
                        Mean += Relative;
                    }

                    Mean *= 1f / pair.Value.Length;
                    IdealPositions[i] = Mean;
                    StartPositions[i] = new Vector4(pair.Key.Position, 1);
                    i++;
                }
            }

            Vector3 BestRotation = new Vector3(), BestTranslation = new Vector3();
            float BestScore = float.MaxValue;

            Vector3 Extent = SurfaceMesh.GetMax() - SurfaceMesh.GetMin();
            float MaxExtent = Math.Max(Math.Max(Extent.X, Extent.Y), Extent.Z) / 2f;
            float AngleConditioning = (float) Math.Asin(Math.Min(1f / MaxExtent, Math.Sin(Helper.ToRad(30))));
            AngleConditioning = Math.Max(1, Helper.ToDeg(AngleConditioning));

            List<Vector3> AnglesToTry = new List<Vector3> { new Vector3(5, 3, -4), new Vector3(5, 3, 184), new Vector3(185, 3, -4), new Vector3(5, 182, -4) };
            Random Rand = new Random(123);
            for (int i = 0; i < 5; i++)
                AnglesToTry.Add(new Vector3(Rand.Next(360), Rand.Next(360), Rand.Next(360)));
            for (int i = 0; i < AnglesToTry.Count; i++)
                AnglesToTry[i] /= AngleConditioning;

            Func<double[], double> F = vars =>
            {
                Vector3 Angles = new Vector3(Helper.ToRad((float)vars[0]), Helper.ToRad((float)vars[1]), Helper.ToRad((float)vars[2])) * AngleConditioning;
                Vector3 Shifts = new Vector3((float)vars[3], (float)vars[4], (float)vars[5]);

                Matrix4 Transform = Matrix4.CreateTranslation(Shifts) *
                                    Matrix4.CreateRotationX(Angles.X) *
                                    Matrix4.CreateRotationY(Angles.Y) *
                                    Matrix4.CreateRotationZ(Angles.Z);

                float MeanDistance = 0;
                for (int i = 0; i < StartPositions.Length; i++)
                    MeanDistance += (Vector4.Transform(StartPositions[i], Transform).Xyz - IdealPositions[i]).LengthSquared;
                MeanDistance /= StartPositions.Length;

                return Math.Sqrt(MeanDistance);
            };

            Func<double[], double[]> G = vars =>
            {
                double[] Gradients = new double[vars.Length];

                for (int i = 0; i < vars.Length; i++)
                {
                    double[] Plus = new double[vars.Length];
                    vars.CopyTo(Plus, 0);
                    Plus[i] += 0.1;

                    double[] Minus = new double[vars.Length];
                    vars.CopyTo(Minus, 0);
                    Minus[i] -= 0.1;

                    Gradients[i] = (F(Plus) - F(Minus)) / 0.2;
                }

                return Gradients;
            };

            foreach (var startAngle in AnglesToTry)
            {
                double[] Start = { startAngle.X, startAngle.Y, startAngle.Z, 0, 0, 0 };

                Cobyla Optimizer = new Cobyla(6, F);
                Optimizer.Minimize(Start);

                if (Optimizer.Value < BestScore)
                {
                    BestScore = (float)Optimizer.Value;
                    BestRotation = new Vector3((float)Optimizer.Solution[0], (float)Optimizer.Solution[1], (float)Optimizer.Solution[2]) * AngleConditioning;
                    BestTranslation = new Vector3((float)Optimizer.Solution[3], (float)Optimizer.Solution[4], (float)Optimizer.Solution[5]);
                }
            }

            Matrix4 ToTarget = Matrix4.CreateTranslation(BestTranslation) *
                                    Matrix4.CreateRotationX(Helper.ToRad(BestRotation.X)) *
                                    Matrix4.CreateRotationY(Helper.ToRad(BestRotation.Y)) *
                                    Matrix4.CreateRotationZ(Helper.ToRad(BestRotation.Z));

            foreach (var v in SurfaceMesh.Vertices)
                v.Position = Vector4.Transform(new Vector4(v.Position, 1), ToTarget).Xyz;
            foreach (var t in SurfaceMesh.Triangles)
                t.UpdateNormal();

            SurfaceMesh.UpdateBuffers();
            Viewport.Redraw();
        }

        private void UpdateRMSD()
        {
            if (LockTarget == null)
            {
                if (ParentWindow != null)
                    ParentWindow.TextRMSD.Text = "";
                return;
            }

            Vector3[] IdealPositions = new Vector3[VertexLocks.Count];
            Vector3[] StartPositions = new Vector3[VertexLocks.Count];
            {
                int i = 0;
                foreach (var pair in VertexLocks)
                {
                    Vector3 OwnPosition = pair.Key.VolumePosition;
                    Vector3 Mean = new Vector3(0);
                    foreach (var target in pair.Value)
                    {
                        Vector3 Relative = OwnPosition - target.GetVolumeCenter();
                        Matrix4 Volume2Transformed = target.GetPlaneMatrix() * Matrix4.Transpose(target.GetVolumePlaneMatrix());
                        Relative = Vector3.Transform(Relative, Matrix4.Transpose(Volume2Transformed));
                        Relative += target.GetCenter();
                        Mean += Relative;
                    }

                    Mean *= 1f / pair.Value.Length;
                    IdealPositions[i] = Mean;
                    StartPositions[i] = pair.Key.Position;
                    i++;
                }
            }

            float MeanDistance = 0;
            for (int i = 0; i < StartPositions.Length; i++)
                MeanDistance += (StartPositions[i] - IdealPositions[i]).LengthSquared;
            MeanDistance /= StartPositions.Length;

            float RMSD =  (float)Math.Sqrt(MeanDistance);
            ParentWindow.TextRMSD.Text = $"(RMSD ca. {RMSD:0.0} Å)";
        }

        private void UnlockFrom(SurfacePatch target)
        {
            if (target == null || target.ParentWindow == null || target.Viewport == null)
                return;

            target.ParentWindow.SizeChanged -= TargetWindow_SizeChanged;
            target.Viewport.Camera.PropertyChanged -= TargetCamera_PropertyChanged;
        }

        private void UpdatePlanarizationStats()
        {
            float MeanZ = SurfaceMesh.Triangles.Sum(x => Math.Abs(x.Normal.Z));
            MeanZ /= SurfaceMesh.Triangles.Count;
            MeanFaceAngle = (decimal)Helper.ToDeg((float)Math.Acos(MeanZ));

            float Offset = (float) SurfaceOffset * MainWindow.Options.PixelScale.X;
            float MeanError = SurfaceMesh.Edges.Sum(e => Math.Abs(e.GetOffsetLength(Offset) - e.GetVolumeOffsetLength(Offset)) / Math.Max(1e-4f, e.GetVolumeLength()));
            MeanError /= SurfaceMesh.Edges.Count;
            MeanEdgeError = (decimal)MeanError * 100M;
        }

        public void StartPlanarization()
        {
            if (PlanarizationThread != null)
                StopPlanarization();

            PlanarizationAccel = new List<Tuple<Vertex, float>[]>();
            float Offset = (float)SurfaceOffset * MainWindow.Options.PixelScale.X;
            for (int i = 0; i < SurfaceMesh.Vertices.Count; i++)
            {
                List<Tuple<Vertex, float>> Neighbors = new List<Tuple<Vertex, float>>();
                foreach (var n in SurfaceMesh.Vertices[i].Neighbors)
                    Neighbors.Add(new Tuple<Vertex, float>(n, (n.GetVolumeOffset(Offset).Position - SurfaceMesh.Vertices[i].GetVolumeOffset(Offset).Position).Length));
                PlanarizationAccel.Add(Neighbors.ToArray());
            }

            PlanarizationThread = new Thread(() =>
            {
                Vector3 Center = SurfaceMesh.GetCentroid();

                while (PlanarizationRunning)
                {
                    lock (ParentWindow)
                    {
                        PerformPlanarizationStep(Center);
                    }
                }
            });
            PlanarizationRunning = true;
            PlanarizationThread.Start();
        }

        public void StopPlanarization()
        {
            PlanarizationRunning = false;
            PlanarizationThread = null;
        }

        private void PerformPlanarizationStep(Vector3 center)
        {
            float EdgeFactor = (float) ShapePreservation;
            Vector3[] Forces = new Vector3[SurfaceMesh.Vertices.Count];

            for (int r = 0; r < 10; r++)
            {
                float MeanZ = SurfaceMesh.GetCentroid().Z;

                Parallel.For(0, SurfaceMesh.Vertices.Count,
                    i => SurfaceMesh.Vertices[i].Position.Z += (MeanZ - SurfaceMesh.Vertices[i].Position.Z) / EdgeFactor * 0.4f);

                for (int s = 0; s < 30; s++)
                {
                    Parallel.For(0, Forces.Length, i =>
                    {
                        Vector3 Force = new Vector3(0);
                        Vector3 OwnPosition = SurfaceMesh.Vertices[i].Position;
                        foreach (var t in PlanarizationAccel[i])
                        {
                            Vector3 Direction = t.Item1.Position - OwnPosition;
                            float Distance = Direction.Length;
                            Direction /= Math.Max(1e-4f, Distance);
                            float Difference = Distance - t.Item2;
                            Force += Direction * Difference;
                        }

                        Forces[i] = Force * 0.5f;
                    });

                    Parallel.For(0, Forces.Length, i => SurfaceMesh.Vertices[i].Position += Forces[i] * 0.4f);
                }
            }

            Vector3 NewCenter = SurfaceMesh.GetCentroid();
            Vector3 CenterAdjustment = center - NewCenter;
            Parallel.ForEach(SurfaceMesh.Vertices, v => v.Position += CenterAdjustment);

            Parallel.ForEach(SurfaceMesh.Triangles, face => face.UpdateNormal());

            ParentWindow?.Dispatcher.Invoke(() =>
            {
                SurfaceMesh.UpdateBuffers();
                Viewport?.Redraw();
            });

            UpdatePlanarizationStats();
        }
    }
}