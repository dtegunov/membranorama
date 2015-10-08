using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        Options Options;

        int MembraneProgramHandle = -1;
        int PointProgramHandle = -1;

        Mesh MembraneMesh;
        VolumeTexture TomogramTexture;

        private Thread IntersectionThread;

        HashSet<Triangle> CurrentSelection = new HashSet<Triangle>();

        public MainWindow()
        {
            Options = new Options();
            this.DataContext = Options;
            Options.PropertyChanged += Options_PropertyChanged;

            Options.Viewport.Paint += Viewport_Paint;
            Options.Viewport.MouseMove += Viewport_MouseMove;
            Options.Viewport.MouseClick += Viewport_MouseClick;

            Options.SurfacePoints.CollectionChanged += SurfacePoints_CollectionChanged;

            MembraneProgramHandle = GLSLProgram.CompileProgram("Shaders/Membrane.vert", null, null, null, "Shaders/Membrane.frag");
            PointProgramHandle = GLSLProgram.CompileProgram("Shaders/Point.vert", null, null, null, "Shaders/Point.frag");

            InitializeComponent();

            UpdateSelectionStats();
        }

        void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ButtonMeshPathText.Text = Options.PathModel.Length > 0 ? Options.PathModel : "Load Surface...";
            ButtonTomogramPathText.Text = Options.PathTomogram.Length > 0 ? Options.PathTomogram : "Load Tomogram...";

            if (e.PropertyName == "SurfaceOffset")
                MembraneMesh.UpdateIntersectionGeometry((float)Options.SurfaceOffset * Options.PixelScale.X);

            if (Options.Viewport != null)
            {
                Options.Viewport.Redraw();
            }
        }

        void SurfacePoints_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Options.Viewport.Redraw();
        }

        private void ViewportHost_Initialized(object sender, EventArgs e)
        {
            ViewportHost.Child = Options.Viewport.GetControl();
        }

        void Viewport_Paint()
        {
            Options.Viewport.MakeCurrent();

            GL.ClearColor(new OpenTK.Graphics.Color4(255, 255, 255, 255));
            GL.ClearDepth(1.0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(MembraneProgramHandle);
            {
                GL.ProgramUniformMatrix4(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "worldViewProjMatrix"), 1, false, Options.Viewport.Camera.GetViewProjAsArray());
                GL.ProgramUniformMatrix4(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "viewProjMatrix"), 1, false, Options.Viewport.Camera.GetViewProjAsArray());
                GL.ProgramUniform1(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "surfaceOffset"), (float)Options.SurfaceOffset * Options.PixelScale.X);

                if (TomogramTexture != null)
                {
                    GL.ProgramUniform1(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "useVolume"), 1f);
                    GL.ProgramUniform3(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "volScale"), 1f / TomogramTexture.Scale.X, 1f / TomogramTexture.Scale.Y, 1f / TomogramTexture.Scale.Z);
                    GL.ProgramUniform3(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "volOffset"), Options.VolumeOffset.X, Options.VolumeOffset.Y, Options.VolumeOffset.Z);
                    GL.ProgramUniform3(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "texSize"), 1f / (float)TomogramTexture.Size.X, 1f / (float)TomogramTexture.Size.Y, 1f / (float)TomogramTexture.Size.Z);

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture3D, TomogramTexture.Handle);
                }
                else
                {
                    GL.ProgramUniform1(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "useVolume"), 0f);
                }

                float TraceStart = (float)Options.TraceDepthOffset;
                float TraceLength = (float)Options.TraceDepth;
                GL.ProgramUniform2(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "traceParams"), TraceStart, TraceLength);

                float RangeMin = (float)Math.Min(Options.OutputRangeMin, Options.OutputRangeMax), RangeMax = (float)Math.Max(Options.OutputRangeMin, Options.OutputRangeMax);
                GL.ProgramUniform2(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "normalizeParams"), RangeMin, RangeMax - RangeMin);

                Vector3 LightDirection = Options.Viewport.Camera.GetDirection();
                GL.ProgramUniform3(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "lightDirection"), LightDirection.X, LightDirection.Y, LightDirection.Z);
                GL.ProgramUniform1(MembraneProgramHandle, GL.GetProgramResourceIndex(MembraneProgramHandle, ProgramInterface.Uniform, "lightIntensity"), (float)Options.OutputLight / 100f);

                if (MembraneMesh != null)
                    MembraneMesh.Draw();
            }

            GL.UseProgram(PointProgramHandle);
            if (Options.SurfacePoints.Count > 0)
            {
                GL.ProgramUniformMatrix4(PointProgramHandle, GL.GetProgramResourceIndex(PointProgramHandle, ProgramInterface.Uniform, "worldViewProjMatrix"), 1, false, Options.Viewport.Camera.GetViewProjAsArray());

                int PointsArray = GL.GenVertexArray();
                GL.BindVertexArray(PointsArray);
                {
                    int PointsBufferPosition = GL.GenBuffer(), PointsBufferColor = GL.GenBuffer();

                    GL.BindBuffer(BufferTarget.ArrayBuffer, PointsBufferPosition);
                    {
                        Vector3 InverseDirection = -Options.Viewport.Camera.GetDirection() * 1.0f;
                        Vector3[] Data = new Vector3[Options.SurfacePoints.Count];
                        for (int i = 0; i < Options.SurfacePoints.Count; i++)
                            Data[i] = Options.SurfacePoints[i].Position + InverseDirection;

                        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(Options.SurfacePoints.Count * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                        GL.EnableVertexAttribArray(0);
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, PointsBufferColor);
                    {
                        Vector3[] Data = new Vector3[Options.SurfacePoints.Count];
                        for (int i = 0; i < Options.SurfacePoints.Count; i++)
                            Data[i] = Options.SurfacePoints[i].IsSelected ? new Vector3(1f, 180f / 255f, 0f) : new Vector3(100f / 255f, 149f / 255f, 237f / 255f);

                        GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(Options.SurfacePoints.Count * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                        GL.EnableVertexAttribArray(1);
                    }
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                    GL.PointSize(14);
                    GL.DrawArrays(PrimitiveType.Points, 0, Options.SurfacePoints.Count);
                    GL.Flush();

                    GL.DeleteBuffer(PointsBufferPosition);
                    GL.DeleteBuffer(PointsBufferColor);
                }
                GL.BindVertexArray(0);
                GL.DeleteVertexArray(PointsArray);
            }

            GL.Finish();
            Options.Viewport.SwapBuffers();
        }

        void Viewport_MouseMove(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (IntersectionThread != null)
                IntersectionThread.Abort();
            IntersectionThread = new Thread(new ParameterizedThreadStart((inputRay) =>
            {
                try
                {
                    if (MembraneMesh == null)
                        return;

                    Ray3 Ray = (Ray3)inputRay;
                    List<Intersection> Intersections = new List<Intersection>();
                    Intersections.AddRange(MembraneMesh.Intersect(Ray));

                    if (Intersections.Count == 0)
                    {
                        Dispatcher.Invoke(() => { TextCursorPosition.Text = ""; });
                        return;
                    }
                    else
                    {
                        Intersections.Sort(new Comparison<Intersection>((i1, i2) =>
                        {
                            return i1.Distance.CompareTo(i2.Distance);
                        }));

                        Vector3 Position = Intersections[0].Position;
                        Dispatcher.Invoke(() =>
                        {
                            TextCursorPosition.Text = String.Format("{0:0.00}, {1:0.00}, {2:0.00} ‎Å", Position.X, Position.Y, Position.Z);
                        });
                    }
                }
                catch (Exception exc)
                {

                }
            }));
            IntersectionThread.Start(ray);

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (MembraneMesh == null)
                    return;

                List<Intersection> Intersections = new List<Intersection>();
                Intersections.AddRange(MembraneMesh.Intersect(ray));
                if (Intersections.Count == 0)
                    return;

                Intersections.Sort(new Comparison<Intersection>((i1, i2) => { return i1.Distance.CompareTo(i2.Distance); }));
                Triangle ClickedTriangle = Intersections[0].Triangle;

                if (Helper.ShiftDown() && !Helper.AltDown())
                    SelectTriangles(new Triangle[] { ClickedTriangle });
                else if (Helper.AltDown() && !Helper.ShiftDown())
                    DeselectTriangles(new Triangle[] { ClickedTriangle });
            }
        }

        void Viewport_MouseClick(Ray3 ray, System.Windows.Forms.MouseEventArgs e)
        {
            if (MembraneMesh == null)
                return;
            
            if (Helper.ShiftDown() || Helper.AltDown() || Helper.CtrlDown())
            {
                List<Intersection> Intersections = new List<Intersection>();
                Intersections.AddRange(MembraneMesh.Intersect(ray));
                if (Intersections.Count == 0)
                    return;

                Intersections.Sort(new Comparison<Intersection>((i1, i2) => { return i1.Distance.CompareTo(i2.Distance); }));
                Triangle ClickedTriangle = Intersections[0].Triangle;

                if (Helper.ShiftDown() && !Helper.AltDown() && !Helper.CtrlDown())
                    SelectTriangles(new Triangle[] { ClickedTriangle });
                else if (Helper.AltDown() && !Helper.ShiftDown() && !Helper.CtrlDown())
                    DeselectTriangles(new Triangle[] { ClickedTriangle });
                else if (Helper.CtrlDown() && !Helper.ShiftDown() && !Helper.AltDown())
                    Options.SurfacePoints.Add(new SurfacePoint(Intersections[0].Position, Intersections[0].Triangle));
            }
        }

        void SelectTriangles(IEnumerable<Triangle> triangles)
        {
            if (MembraneMesh == null)
                return;

            foreach (Triangle t in triangles)
                CurrentSelection.Add(t);
            UpdateSelectionStats();

            foreach (Triangle t in triangles)
                t.Color = new Vector4(1, 0, 0, 0.5f);
            MembraneMesh.UpdateColors();
            Options.Viewport.Redraw();
        }

        void DeselectTriangles(IEnumerable<Triangle> triangles)
        {
            if (MembraneMesh == null)
                return;

            foreach (Triangle t in triangles)
                CurrentSelection.Remove(t);
            UpdateSelectionStats();

            foreach (Triangle t in triangles)
                t.Color = new Vector4(0);
            MembraneMesh.UpdateColors();
            Options.Viewport.Redraw();
        }

        void DeselectAllTriangles()
        {
            if (MembraneMesh == null || CurrentSelection.Count == 0)
                return;

            Triangle[] All = new Triangle[MembraneMesh.Triangles.Count];
            for (int i = 0; i < MembraneMesh.Triangles.Count; i++)
                All[i] = MembraneMesh.Triangles[i];
            DeselectTriangles(All);
        }

        void InvertTriangleSelection()
        {
            if (MembraneMesh == null)
                return;

            List<Triangle> Select = new List<Triangle>();
            List<Triangle> Deselect = new List<Triangle>();

            foreach (Triangle t in MembraneMesh.Triangles)
                if (CurrentSelection.Contains(t))
                    Deselect.Add(t);
                else
                    Select.Add(t);

            DeselectTriangles(Deselect);
            SelectTriangles(Select);
        }

        void GrowTriangleSelection()
        {
            if (CurrentSelection.Count == 0)
                return;

            HashSet<Triangle> Select = new HashSet<Triangle>();
            float AngleLimit = (float)Math.Cos((float)Options.SelectionAngle / 180f * Math.PI);
            Vector3 SelectionNormal = new Vector3(0);
            foreach (Triangle t in CurrentSelection)
                SelectionNormal += t.Normal;
            SelectionNormal.Normalize();

            foreach (Triangle t in CurrentSelection)
                foreach (Triangle n in t.Neighbors)
                    if (!CurrentSelection.Contains(n) && Vector3.Dot(SelectionNormal, n.Normal) >= AngleLimit)
                        Select.Add(n);

            SelectTriangles(Select);
        }

        void ShrinkTriangleSelection()
        {
            HashSet<Triangle> Deselect = new HashSet<Triangle>();

            foreach (Triangle t in CurrentSelection)
                foreach (Triangle n in t.Neighbors)
                    if (!CurrentSelection.Contains(n))
                    {
                        Deselect.Add(t);
                        break;
                    }

            DeselectTriangles(Deselect);
        }

        void UpdateSelectionStats()
        {
            float Area = 0f;
            foreach (Triangle t in CurrentSelection)
                Area += t.GetArea();

            TextSelectionStats.Text = String.Format("{0} faces, {1:0.00} Å².", CurrentSelection.Count, Area);
        }

        void LoadModel(string path)
        {
            if (MembraneMesh != null)
            {
                MembraneMesh.FreeBuffers();
            }

            MembraneMesh = Mesh.FromOBJ(path);
            MembraneMesh.UpdateBuffers();
            MembraneMesh.UpdateIntersectionGeometry(0f);

            Options.SurfaceOffset = 0;
            Options.Viewport.Camera.CenterOn(MembraneMesh);

            Options.PathModel = path;
        }

        void LoadTomogram(string path)
        {
            if (TomogramTexture != null)
                TomogramTexture.FreeBuffers();

            // Make null and go through temp so subsequent parameter updates don't cause a viewport redraw.
            TomogramTexture = null;
            VolumeTexture Temp = VolumeTexture.FromMRC(path);

            Options.PathTomogram = path;
            Options.PixelScale = Temp.Scale;
            Options.VolumeOffset = Temp.Offset;

            TomogramTexture = Temp;
            TomogramTexture.UpdateBuffers();
        }

        private void ButtonMeshPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "Wavefront OBJ|*.obj";
            Dialog.Multiselect = false;
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                LoadModel(Dialog.FileName);
            }
        }

        private void ButtonTomogramPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "MRC|*.mrc;*.rec";
            Dialog.Multiselect = false;
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                LoadTomogram(Dialog.FileName);
            }
        }

        private void ButtonSelectionGrow_Click(object sender, RoutedEventArgs e)
        {
            GrowTriangleSelection();
        }

        private void ButtonSelectionShrink_Click(object sender, RoutedEventArgs e)
        {
            ShrinkTriangleSelection();
        }

        private void ButtonSelectionInvert_Click(object sender, RoutedEventArgs e)
        {
            InvertTriangleSelection();
        }

        private void ButtonSelectionClear_Click(object sender, RoutedEventArgs e)
        {
            DeselectAllTriangles();
        }

        private void ListBoxPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (SurfacePoint p in Options.SurfacePoints)
                p.IsSelected = false;

            foreach (SurfacePoint p in ListBoxPoints.SelectedItems)
                p.IsSelected = true;

            Options.Viewport.Redraw();
        }

        private void ButtonPointsExport_Click(object sender, RoutedEventArgs e)
        {
            if (Options.SurfacePoints.Count == 0)
                return;

            System.Windows.Forms.SaveFileDialog Dialog = new System.Windows.Forms.SaveFileDialog();
            Dialog.Filter = "Text File|*.txt";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                using(TextWriter Writer = File.CreateText(Dialog.FileName))
                {
                    Writer.WriteLine("PositionX\tPositionY\tPositionZ\tNormalX\tNormalY\tNormalZ");

                    foreach (SurfacePoint p in Options.SurfacePoints)
                        if (ListBoxPoints.SelectedItems.Count == 0 || p.IsSelected) // If nothing is selected, export all; otherwise only selected.
                            Writer.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", p.Position.X, p.Position.Y, p.Position.Z, p.Face.Normal.X, p.Face.Normal.Y, p.Face.Normal.Z));
                }
            }
        }

        private void ButtonPointsRemove_Click(object sender, RoutedEventArgs e)
        {
            List<SurfacePoint> Selected = new List<SurfacePoint>();
            foreach (SurfacePoint item in ListBoxPoints.SelectedItems)
                Selected.Add(item);

            foreach (SurfacePoint p in Selected)
                Options.SurfacePoints.Remove(p);
        }
    }
}
