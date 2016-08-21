using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Warp;
using Membranogram.Helpers;

namespace Membranogram
{
    public class Membrane : DataBase
    {
        public GLSLProgram MeshProgram, PointProgram, PointGizmoProgram, PointModelProgram;
        public ImageTexture SelectionTexture;
        private Viewport Viewport;

        public VolumeTexture TomogramTexture;
        public Mesh SurfaceMesh;

        #region File paths

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

        #endregion

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

                    if (SurfaceMesh != null)
                        SurfaceMesh.UpdateProcessedGeometry((float)SurfaceOffset * MainWindow.Options.PixelScale.X);
                }
            }
        }

        #endregion

        #region Volume tracing options

        private int _TraceDepthOffset = 0;
        public int TraceDepthOffset
        {
            get { return _TraceDepthOffset; }
            set { if (value != _TraceDepthOffset) { _TraceDepthOffset = value; OnPropertyChanged(); } }
        }

        private int _TraceDepth = 0;
        public int TraceDepth
        {
            get { return _TraceDepth; }
            set { if (value != _TraceDepth) { _TraceDepth = value; OnPropertyChanged(); } }
        }

        private decimal _TraceSharpening = 0M;
        public decimal TraceSharpening
        {
            get { return _TraceSharpening; }
            set { if (value != _TraceSharpening) { _TraceSharpening = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Rendering output options

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

        #endregion

        #region Point groups

        private ObservableCollection<PointGroup> _PointGroups = new ObservableCollection<PointGroup>();
        public ObservableCollection<PointGroup> PointGroups
        {
            get { return _PointGroups; }
            set { if (value != _PointGroups) { _PointGroups = value; OnPropertyChanged(); } }
        }

        private PointGroup _ActiveGroup = null;
        public PointGroup ActiveGroup
        {
            get { return _ActiveGroup; }
            set { if (value != _ActiveGroup) { _ActiveGroup = value; OnPropertyChanged(); } }
        }

        public PointGroup PreviewGroup { get; } = new PointGroup();

        #endregion

        #region Selection properties

        private int _SelectionAngle = 90;
        public int SelectionAngle
        {
            get { return _SelectionAngle; }
            set { if (value != _SelectionAngle) { _SelectionAngle = value; OnPropertyChanged(); } }
        }

        private bool _IsMouseOver = false;
        public bool IsMouseOver
        {
            get { return _IsMouseOver; }
            set { if (value != _IsMouseOver) { _IsMouseOver = value; OnPropertyChanged(); } }
        }

        private string _TriangleSelectionStats = "";
        public string TriangleSelectionStats
        {
            get { return _TriangleSelectionStats; }
            set { if (value != _TriangleSelectionStats) { _TriangleSelectionStats = value; OnPropertyChanged(); } }
        }

        HashSet<Triangle> CurrentTriangleSelection = new HashSet<Triangle>();

        #endregion

        #region Patches

        private ObservableCollection<SurfacePatch> _Patches = new ObservableCollection<SurfacePatch>();
        public ObservableCollection<SurfacePatch> Patches
        {
            get { return _Patches; }
            set { if (value != _Patches) { _Patches = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<SurfacePatch> _DisplayedPatches = new ObservableCollection<SurfacePatch>();
        public ObservableCollection<SurfacePatch> DisplayedPatches
        {
            get { return _DisplayedPatches; }
            set { if (value != _DisplayedPatches) { _DisplayedPatches = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Events

        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseEnter;
        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseMove;
        public event Action<Membrane, MouseEventArgs> MouseLeave;
        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseClick;
        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseWheel;
        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseDown;
        public event Action<Membrane, List<Intersection>, MouseEventArgs> MouseUp;

        public event Action<Membrane, List<Triangle>> TriangleSelectionChanged; 

        #endregion

        public Membrane()
        {
            MeshProgram = new GLSLProgram("Shaders/Membrane.vert", null, null, null, "Shaders/Membrane.frag");
            PointProgram = new GLSLProgram("Shaders/Point.vert", null, null, "Shaders/Point.geom", "Shaders/Point.frag");
            PointGizmoProgram = new GLSLProgram("Shaders/Point.vert", null, null, "Shaders/PointGizmo.geom", "Shaders/Point.frag");
            PointModelProgram = new GLSLProgram("Shaders/PointModel.vert", null, null, null, "Shaders/PointModel.frag");
            SelectionTexture = ImageTexture.FromMRC("Shaders/unicorn.mrc");

            PointGroups.CollectionChanged += PointGroups_CollectionChanged;

            ActiveGroup = new PointGroup { Color = ColorHelper.SpectrumColor(0, 0.3f), Name = "Default Group", Size = 10 };
            PointGroups.Add(ActiveGroup);

            Patches.CollectionChanged += Patches_CollectionChanged;
        }

        public void AttachToViewport(Viewport viewport)
        {
            if (Viewport != null)
            {
                Viewport.MouseClick -= Viewport_MouseClick;
                Viewport.MouseMove -= Viewport_MouseMove;
                Viewport.MouseWheel -= Viewport_MouseWheel;
                Viewport.MouseDown -= Viewport_MouseDown;
                Viewport.MouseUp -= Viewport_MouseUp;
                Viewport.Paint -= Viewport_Paint;
            }

            Viewport = viewport;
            Viewport.MouseClick += Viewport_MouseClick;
            Viewport.MouseMove += Viewport_MouseMove;
            Viewport.MouseWheel += Viewport_MouseWheel;
            Viewport.MouseDown += Viewport_MouseDown;
            Viewport.MouseUp += Viewport_MouseUp;
            Viewport.Paint += Viewport_Paint;

            foreach (var item in PointGroups)
                item.PointCloud.GLContext = viewport.GetControl();

            SelectionTexture.UpdateBuffers();
        }

        #region Event handling

        void Viewport_MouseMove(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));
            foreach (var group in PointGroups)
                if (group.IsVisible)
                    Intersections.AddRange(group.Intersect(ray));

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

        void Viewport_MouseClick(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));
            foreach (var group in PointGroups)
                if (group.IsVisible)
                    Intersections.AddRange(group.Intersect(ray));

            if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                MouseClick?.Invoke(this, Intersections, e);
            }
        }

        private void Viewport_MouseWheel(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));
            foreach (var group in PointGroups)
                if (group.IsVisible)
                    Intersections.AddRange(group.Intersect(ray));

            if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                MouseWheel?.Invoke(this, Intersections, e);
            }
        }

        private void Viewport_MouseDown(Ray3 ray, MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));
            foreach (var group in PointGroups)
                if (group.IsVisible)
                    Intersections.AddRange(group.Intersect(ray));

            if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                MouseDown?.Invoke(this, Intersections, e);
            }
        }

        private void Viewport_MouseUp(Ray3 ray, MouseEventArgs e)
        {
            if (SurfaceMesh == null)
                return;

            List<Intersection> Intersections = new List<Intersection>();
            Intersections.AddRange(SurfaceMesh.Intersect(ray));
            foreach (var group in PointGroups)
                if (group.IsVisible)
                    Intersections.AddRange(group.Intersect(ray));

            if (Intersections.Count > 0)
            {
                Intersections.Sort((i1, i2) => i1.Distance.CompareTo(i2.Distance));

                MouseUp?.Invoke(this, Intersections, e);
            }
        }

        void PointGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.Cast<PointGroup>())
                    item.PropertyChanged -= PointGroup_PropertyChanged;

            if (e.NewItems != null)
                foreach (var item in e.NewItems.Cast<PointGroup>())
                    item.PropertyChanged += PointGroup_PropertyChanged;
        }

        void PointGroup_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsVisible")
                Viewport.Redraw();
        }

        #endregion

        #region Rendering
        
        public void Viewport_Paint()
        {
            if (SurfaceMesh != null)
            {
                Vector3[] Bounds = SurfaceMesh.BoundingBoxCorners;
                Matrix4 ViewMatrix = Viewport.Camera.GetView();

                float MinDist = float.MaxValue;
                float MaxDist = -float.MaxValue;

                foreach (var corner in Bounds)
                {
                    float Dist = -Vector3.Transform(corner, ViewMatrix).Z;
                    MinDist = Math.Min(MinDist, Dist);
                    MaxDist = Math.Max(MaxDist, Dist);
                }

                MainWindow.Options.Viewport.Camera.ClipNear = Math.Max(1f, MinDist / 2);
                MainWindow.Options.Viewport.Camera.ClipFar = Math.Max(2f, MaxDist * 2);
            }

            MeshProgram.Use();
            {
                MeshProgram.SetUniform("worldViewProjMatrix", MainWindow.Options.Viewport.Camera.GetViewProj());
                MeshProgram.SetUniform("surfaceOffset", (float)SurfaceOffset * MainWindow.Options.PixelScale.X);

                if (TomogramTexture != null)
                {
                    MeshProgram.SetUniform("useVolume", 1f);
                    MeshProgram.SetUniform("volScale", OpenGLHelper.Reciprocal(TomogramTexture.Scale));
                    MeshProgram.SetUniform("volOffset", TomogramTexture.Offset);
                    MeshProgram.SetUniform("texSize", OpenGLHelper.Reciprocal(TomogramTexture.Size));

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture3D, TomogramTexture.Handle);
                }
                else
                {
                    MeshProgram.SetUniform("useVolume", 0f);
                }

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, SelectionTexture.Handle);

                float TraceStart = TraceDepthOffset;
                float TraceLength = TraceDepth;
                MeshProgram.SetUniform("traceParams", new Vector2(TraceStart, TraceLength));
                MeshProgram.SetUniform("traceSharpening", (float)TraceSharpening / 100f);

                float RangeMin = (float)Math.Min(OutputRangeMin, OutputRangeMax), RangeMax = (float)Math.Max(OutputRangeMin, OutputRangeMax);
                MeshProgram.SetUniform("normalizeParams", new Vector2(RangeMin, RangeMax - RangeMin));

                Vector3 LightDirection = MainWindow.Options.Viewport.Camera.GetDirection();
                MeshProgram.SetUniform("lightDirection", LightDirection);
                MeshProgram.SetUniform("lightIntensity", OutputLight / 100f);

                if (SurfaceMesh != null)
                    SurfaceMesh.Draw();
            }

            // Draw point groups. There are 3 options for depiction:
            // - Boxes with custom size
            // - Same mesh for every point in a group, e. g. protein map from EMDB
            // - Individual mesh for every point in a group, e. g. isosurface within its enclosed volume
            {
                List<PointGroup> AllGroups = new List<PointGroup>(PointGroups);
                AllGroups.Add(PreviewGroup);
                foreach (PointGroup group in AllGroups.Where(g => g.IsVisible && g.Points.Count > 0))
                {
                    if (group.Depiction == PointDepiction.Box)
                    {
                        // Draw orientation gizmos
                        PointGizmoProgram.Use();
                        PointGizmoProgram.SetUniform("worldViewProjMatrix", MainWindow.Options.Viewport.Camera.GetViewProj());
                        GL.LineWidth(10f);
                        PointGizmoProgram.SetUniform("cubeSize", (float)group.Size * MainWindow.Options.PixelScale.X);
                        group.PointCloud.Draw();
                        
                        // Draw boxes
                        PointProgram.Use();
                        PointProgram.SetUniform("worldViewProjMatrix", MainWindow.Options.Viewport.Camera.GetViewProj());

                        // Draw back faces first, then front, to ensure correct transparency (locally)
                        GL.Enable(EnableCap.CullFace);
                        GL.CullFace(CullFaceMode.Front);

                        PointProgram.SetUniform("cubeSize", (float)group.Size * MainWindow.Options.PixelScale.X);
                        group.PointCloud.Draw();

                        GL.CullFace(CullFaceMode.Back);
                        PointProgram.SetUniform("cubeSize", (float)group.Size * MainWindow.Options.PixelScale.X);
                        group.PointCloud.Draw();
                        GL.Disable(EnableCap.CullFace);
                    }
                    else if (group.Depiction == PointDepiction.Mesh)
                    {
                        PointModelProgram.Use();
                        PointModelProgram.SetUniform("modelColor", ColorHelper.ColorToVector(group.Color, true));
                        PointModelProgram.SetUniform("cameraDirection", Viewport.Camera.GetDirection());

                        foreach (var point in group.Points)
                        {
                            PointModelProgram.SetUniform("isSelected", point.IsSelected ? 1f : 0f);
                            Vector3 Offset = point.TransformedMatrix.Column2 * (float)group.DepictionMeshOffset;
                            PointModelProgram.SetUniform("worldViewProjMatrix", Matrix4.CreateTranslation(point.Position + Offset) * MainWindow.Options.Viewport.Camera.GetViewProj());
                            PointModelProgram.SetUniform("rotationMatrix", Matrix3.Transpose(point.TransformedMatrix));

                            group.DepictionMesh?.Draw();
                        }
                    }
                    else if (group.Depiction == PointDepiction.LocalSurface)
                    {
                        PointModelProgram.Use();
                        PointModelProgram.SetUniform("modelColor", ColorHelper.ColorToVector(group.Color, true));
                        PointModelProgram.SetUniform("cameraDirection", Viewport.Camera.GetDirection());

                        foreach (var point in group.Points)
                        {
                            PointModelProgram.SetUniform("isSelected", point.IsSelected ? 1f : 0f);
                            PointModelProgram.SetUniform("worldViewProjMatrix", Matrix4.CreateTranslation(point.Position) * MainWindow.Options.Viewport.Camera.GetViewProj());
                            PointModelProgram.SetUniform("rotationMatrix", Matrix3.Identity);

                            point.DepictionMesh?.Draw();
                        }

                        // Also draw the orientation stick
                        if (KeyboardHelper.CtrlDown())
                        {
                            PointGizmoProgram.Use();
                            PointGizmoProgram.SetUniform("worldViewProjMatrix", MainWindow.Options.Viewport.Camera.GetViewProj());
                            GL.LineWidth(10f);
                            PointGizmoProgram.SetUniform("cubeSize", (float)group.Size * MainWindow.Options.PixelScale.X);
                            group.PointCloud.Draw();
                        }
                    }
                }
            }
        }

        #endregion

        #region Triangle selection operations
        
        public void SelectTriangles(IEnumerable<Triangle> triangles)
        {
            if (SurfaceMesh == null)
                return;

            foreach (Triangle t in triangles.Where(t => t.IsVisible && t.Patch == null))
            {
                t.IsSelected = true;
                CurrentTriangleSelection.Add(t);
            }

            SurfaceMesh.UpdateSelection();
            Viewport.Redraw();

            if (TriangleSelectionChanged != null)
                TriangleSelectionChanged(this, CurrentTriangleSelection.ToList());
        }

        public void DeselectTriangles(IEnumerable<Triangle> triangles)
        {
            if (SurfaceMesh == null)
                return;

            foreach (Triangle t in triangles)
            {
                t.IsSelected = false;
                CurrentTriangleSelection.Remove(t);
            }

            SurfaceMesh.UpdateSelection();
            Viewport.Redraw();

            if (TriangleSelectionChanged != null)
                TriangleSelectionChanged(this, CurrentTriangleSelection.ToList());
        }

        public void DeselectAllTriangles()
        {
            if (SurfaceMesh == null || CurrentTriangleSelection.Count == 0)
                return;

            Triangle[] All = new Triangle[SurfaceMesh.Triangles.Count];
            for (int i = 0; i < SurfaceMesh.Triangles.Count; i++)
                All[i] = SurfaceMesh.Triangles[i];
            DeselectTriangles(All);
        }

        public void InvertTriangleSelection()
        {
            if (SurfaceMesh == null)
                return;

            List<Triangle> Select = new List<Triangle>();
            List<Triangle> Deselect = new List<Triangle>();

            foreach (Triangle t in SurfaceMesh.Triangles)
                if (CurrentTriangleSelection.Contains(t))
                    Deselect.Add(t);
                else
                    Select.Add(t);

            DeselectTriangles(Deselect);
            SelectTriangles(Select);
        }

        public void FillTriangleSelection()
        {
            if (CurrentTriangleSelection.Count == 0)
                return;

            HashSet<Triangle> FillSelect = new HashSet<Triangle>(CurrentTriangleSelection);
            HashSet<Triangle> Frontier = new HashSet<Triangle>(CurrentTriangleSelection);

            while (true)
            {
                HashSet<Triangle> Select = new HashSet<Triangle>();
                float AngleLimit = (float) Math.Cos(SelectionAngle / 180f * Math.PI);
                Vector3 SelectionNormal = new Vector3(0);
                SelectionNormal = FillSelect.Aggregate(SelectionNormal, (current, t) => current + t.VolumeNormal);
                SelectionNormal.Normalize();

                foreach (Triangle t in Frontier)
                    foreach (Triangle n in t.Neighbors)
                        if (n.IsVisible && n.Patch == null && !FillSelect.Contains(n) && Vector3.Dot(SelectionNormal, n.VolumeNormal) >= AngleLimit)
                            Select.Add(n);

                if (Select.Count == 0)
                    break;

                Frontier = Select;
                foreach (var t in Select)
                    FillSelect.Add(t);
            }

            SelectTriangles(FillSelect);
        }

        public void GrowTriangleSelection()
        {
            if (CurrentTriangleSelection.Count == 0)
                return;

            HashSet<Triangle> Select = new HashSet<Triangle>();
            float AngleLimit = (float)Math.Cos(SelectionAngle / 180f * Math.PI);
            Vector3 SelectionNormal = new Vector3(0);
            SelectionNormal = CurrentTriangleSelection.Aggregate(SelectionNormal, (current, t) => current + t.VolumeNormal);
            SelectionNormal.Normalize();

            foreach (Triangle t in CurrentTriangleSelection)
                foreach (Triangle n in t.Neighbors)
                    if (n.IsVisible && n.Patch == null && !CurrentTriangleSelection.Contains(n) && Vector3.Dot(SelectionNormal, n.VolumeNormal) >= AngleLimit)
                        Select.Add(n);

            SelectTriangles(Select);
        }

        public void ShrinkTriangleSelection()
        {
            HashSet<Triangle> Deselect = new HashSet<Triangle>();

            foreach (Triangle t in CurrentTriangleSelection)
                foreach (Triangle n in t.Neighbors)
                    if (!CurrentTriangleSelection.Contains(n))
                    {
                        Deselect.Add(t);
                        break;
                    }

            DeselectTriangles(Deselect);
        }

        public List<Triangle> GetSelectedTriangles()
        {
            return CurrentTriangleSelection.ToList();
        }

        #endregion

        #region Patches

        public void SetTriangleColor(IEnumerable<Triangle> triangles, Vector4 color)
        {
            foreach (var t in triangles)
                t.Color = color;
            SurfaceMesh.UpdateColors();
        }

        public void SetTriangleVisible(IEnumerable<Triangle> triangles, bool isVisible)
        {
            foreach (var t in triangles)
                t.IsVisible = isVisible;

            Viewport.GetControl().MakeCurrent();
            SurfaceMesh.UpdateBuffers();
            SurfaceMesh.UpdateProcessedGeometry((float)SurfaceOffset);
        }

        public void SetTrianglePatch(IEnumerable<Triangle> triangles, SurfacePatch patch)
        {
            foreach (var t in triangles)
                t.Patch = patch;

            SurfaceMesh.UpdateProcessedGeometry((float)SurfaceOffset);
        }

        void Patches_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        #endregion

        #region File loading

        public void LoadModel(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (SurfaceMesh != null)
                SurfaceMesh.FreeBuffers();

            SurfaceMesh = Mesh.FromOBJ(path);
            SurfaceMesh.GLContext = Viewport.GetControl();
            SurfaceMesh.UpdateProcessedGeometry(0f);

            Viewport.GetControl().MakeCurrent();
            SurfaceMesh.UpdateBuffers();

            SurfaceOffset = 0;
            Viewport.Camera.CenterOn(SurfaceMesh);

            PathModel = path;
        }

        public void LoadTomogram(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (TomogramTexture != null)
                TomogramTexture.FreeBuffers();

            // Make null and go through temp so subsequent parameter updates don't cause a viewport redraw.
            TomogramTexture = null;
            VolumeTexture Temp = VolumeTexture.FromMRC(path);

            PathTomogram = path;
            MainWindow.Options.PixelScale = Temp.Scale;

            TomogramTexture = Temp;
            TomogramTexture.UpdateBuffers();
            Viewport.Redraw();
        }

        #endregion
    }
}