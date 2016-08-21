using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.XPath;
using Membranogram.Controls;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Warp.Tools;
using Membranogram.Helpers;

namespace Membranogram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public static Options Options;
        public static Viewport Viewport;

        private SurfacePoint DraggingPoint = null;

        public MainWindow()
        {
            Options = new Options();
            Options.Viewport = new Viewport(this);
            DataContext = Options;
            Options.PropertyChanged += Options_PropertyChanged;

            InitializeComponent();

            Options.Membrane = new Membrane();

            Options.Membrane.PropertyChanged += Membrane_PropertyChanged;
            Options.Membrane.MouseMove += Membrane_MouseMove;
            Options.Membrane.MouseLeave += Membrane_MouseLeave;
            Options.Membrane.MouseEnter += Membrane_MouseEnter;
            Options.Membrane.MouseClick += Membrane_MouseClick;
            Options.Membrane.MouseWheel += Membrane_MouseWheel;
            Options.Membrane.MouseDown += Membrane_MouseDown;
            Options.Membrane.MouseUp += Membrane_MouseUp;
            Options.Membrane.TriangleSelectionChanged += Membrane_TriangleSelectionChanged;

            Options.Membrane.AttachToViewport(Options.Viewport);

            Options.Membrane.PreviewGroup.PointCloud.GLContext = Options.Viewport.GetControl();

            /*Options.Membrane.ActiveGroup.Depiction = PointDepiction.Mesh;
            Options.Membrane.ActiveGroup.DepictionMeshPath = "D:\\Dev\\membranorama\\emd_6617.obj";
            Options.Membrane.ActiveGroup.DepictionMesh = Mesh.FromOBJ(Options.Membrane.ActiveGroup.DepictionMeshPath, true);
            Options.Membrane.ActiveGroup.DepictionMesh.UsedComponents = MeshVertexComponents.Position | MeshVertexComponents.Normal;
            Options.Membrane.ActiveGroup.DepictionMesh.GLContext = Options.Viewport.GetControl();
            Options.Membrane.ActiveGroup.DepictionMesh.UpdateBuffers();*/
        }

        void Membrane_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Membrane Sender = (Membrane)sender;
            ButtonMeshPathText.Text = Sender.PathModel.Length > 0 ? Sender.PathModel : "Load Surface...";
            ButtonTomogramPathText.Text = Sender.PathTomogram.Length > 0 ? Sender.PathTomogram : "Load Tomogram...";

            if (e.PropertyName == "ActiveGroup")
                UpdatePointContextMenu();

            if (Options.Viewport != null)
                Options.Viewport.Redraw();
        }

        void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Options.Viewport != null)
                Options.Viewport.Redraw();
        }

        private void ViewportHost_Initialized(object sender, EventArgs e)
        {
            ViewportHost.Child = Options.Viewport.GetControl();
        }

        private void ButtonScreenshot_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog Dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "PNG|*.png|MRC|*.mrc"
            };
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();
            if (Result == System.Windows.Forms.DialogResult.OK)
            {
                Options.Viewport.GrabScreenshot(Dialog.FileName);
            }
        }

        #region Membrane mouse event handling

        void Membrane_MouseEnter(Membrane sender, List<Intersection> arg1, System.Windows.Forms.MouseEventArgs arg2)
        {
            
        }

        void Membrane_MouseLeave(Membrane sender, System.Windows.Forms.MouseEventArgs arg2)
        {
            TextCursorPosition.Text = "";
            DraggingPoint = null;

            if (Options.Membrane.PreviewGroup.Points.Count == 1)
            {
                Options.Membrane.PreviewGroup.Points.RemoveAt(0);
                Options.Viewport.Redraw();
            }
            else if (Options.Membrane.PreviewGroup.Points.Count > 1)
                throw new Exception("Preview group has more than 1 point, but it really should not.");
        }

        void Membrane_MouseMove(Membrane sender, List<Intersection> intersections, System.Windows.Forms.MouseEventArgs e)
        {
            bool RedrawNeeded = false;
            Options.Viewport.AreUpdatesDisabled = true;

            IEnumerable<Intersection> TriangleIntersections = intersections.Where(i => i.Target.GetType() == typeof (Triangle));
            IEnumerable<Intersection> PointIntersections = intersections.Where(i => i.Target.GetType() == typeof(SurfacePoint));

            bool IsPointForemost = PointIntersections.Count() > 0 ? PointIntersections.First() == intersections.First() : false;

            if (TriangleIntersections.Count() > 0)
            {
                Triangle PickedTriangle = (Triangle)TriangleIntersections.First().Target;
                Vector3 Position = TriangleIntersections.First().Position;
                TextCursorPosition.Text = $"{Position.X:0.00}, {Position.Y:0.00}, {Position.Z:0.00} ‎Å";

                if (PickedTriangle != null && e.Button == System.Windows.Forms.MouseButtons.Left)   // Extent triangle selection
                {
                    if (KeyboardHelper.ShiftDown() && !KeyboardHelper.AltDown())
                    {
                        sender.SelectTriangles(new[] { PickedTriangle });
                        RedrawNeeded = true;
                    }
                    else if (KeyboardHelper.AltDown() && !KeyboardHelper.ShiftDown())
                    {
                        sender.DeselectTriangles(new[] { PickedTriangle });
                        RedrawNeeded = true;
                    }
                }
                else if (KeyboardHelper.CtrlDown() && !IsPointForemost && !KeyboardHelper.ShiftDown() && !KeyboardHelper.AltDown())     // Show surface point preview
                {
                    sender.PreviewGroup.CopyPropertiesFrom(sender.ActiveGroup);

                    Vector3 PointPosition = Position;
                    if (sender.PreviewGroup.Depiction == PointDepiction.LocalSurface && sender.TomogramTexture != null) // Discretize position in case of local isosurface
                    {
                        Vector3 TomoOffset = sender.TomogramTexture.Offset;
                        float Scale = Options.PixelScale.X;
                        PointPosition.X = (float)Math.Round((Position.X - TomoOffset.X) / Scale) * Scale + TomoOffset.X;
                        PointPosition.Y = (float)Math.Round((Position.Y - TomoOffset.Y) / Scale) * Scale + TomoOffset.Y;
                        PointPosition.Z = (float)Math.Round((Position.Z - TomoOffset.Z) / Scale) * Scale + TomoOffset.Z;
                    }

                    if (sender.PreviewGroup.Points.Count == 0 ||
                        (sender.PreviewGroup.Points[0].Position - PointPosition).Length > 0.1f)
                    {
                        float PsiDiff = 0;
                        if (sender.PreviewGroup.Points.Count == 1)
                        {
                            OpenTK.Matrix3 OldFrame = sender.PreviewGroup.Points[0].TransformedMatrix;
                            OpenTK.Matrix3 NewFrame = PickedTriangle.GetPlaneMatrix3();

                            OpenTK.Matrix3 DiffFrame = OpenTK.Matrix3.Transpose(OldFrame) * NewFrame;
                            PsiDiff = (float)Math.Atan2(DiffFrame.Column0.Y, DiffFrame.Column0.X);
                        }

                        if (sender.PreviewGroup.Points.Count == 1)
                            sender.PreviewGroup.Points.RemoveAt(0);
                        else if (sender.PreviewGroup.Points.Count > 1)
                            throw new Exception("Preview group has more than 1 point, but it really should not.");

                        float Offset = (float)sender.SurfaceOffset * Options.PixelScale.X;
                        Vector3 TriangleGlobal = Position - PickedTriangle.Normal * Offset;
                        Vector3 TriangleLocal = PickedTriangle.ToBarycentric(TriangleGlobal);

                        sender.PreviewGroup.Points.Add(new SurfacePoint(PointPosition, PickedTriangle, TriangleLocal, Offset, PsiDiff));

                        RedrawNeeded = true;
                    }
                }
                else if (!KeyboardHelper.CtrlDown() || IsPointForemost)     // Hide surface point preview
                {
                    if (Options.Membrane.PreviewGroup.Points.Count == 1)
                    {
                        Options.Membrane.PreviewGroup.Points.RemoveAt(0);
                        RedrawNeeded = true;
                    }
                    else if (Options.Membrane.PreviewGroup.Points.Count > 1)
                        throw new Exception("Preview group has more than 1 point, but it really should not.");
                }

                if (KeyboardHelper.CtrlDown() && DraggingPoint != null && TriangleIntersections.Any())
                {
                    Vector3 PointPosition = Position;
                    if (DraggingPoint.Group.Depiction == PointDepiction.LocalSurface && sender.TomogramTexture != null)
                    {
                        Vector3 TomoOffset = sender.TomogramTexture.Offset;
                        float Scale = Options.PixelScale.X;
                        PointPosition.X = (float)Math.Round((Position.X - TomoOffset.X) / Scale) * Scale + TomoOffset.X;
                        PointPosition.Y = (float)Math.Round((Position.Y - TomoOffset.Y) / Scale) * Scale + TomoOffset.Y;
                        PointPosition.Z = (float)Math.Round((Position.Z - TomoOffset.Z) / Scale) * Scale + TomoOffset.Z;
                    }

                    float PsiDiff = 0;
                    OpenTK.Matrix3 OldFrame = DraggingPoint.TransformedMatrix;
                    OpenTK.Matrix3 NewFrame = PickedTriangle.GetPlaneMatrix3();

                    OpenTK.Matrix3 DiffFrame = OpenTK.Matrix3.Transpose(OldFrame) * NewFrame;
                    PsiDiff = (float)Math.Atan2(DiffFrame.Column0.Y, DiffFrame.Column0.X);

                    float Offset = (float)sender.SurfaceOffset * Options.PixelScale.X;
                    Vector3 TriangleGlobal = Position - PickedTriangle.Normal * Offset;
                    Vector3 TriangleLocal = PickedTriangle.ToBarycentric(TriangleGlobal);

                    DraggingPoint.OriginalMatrix = NewFrame;
                    DraggingPoint.Position = PointPosition;
                    DraggingPoint.SurfaceOffset = Offset;
                    DraggingPoint.Psi = PsiDiff;
                    DraggingPoint.Face = PickedTriangle;
                    DraggingPoint.BarycentricCoords = TriangleLocal;

                    DraggingPoint.Group.PointCloud.UpdateBuffers();

                    if (DraggingPoint.Group.Depiction == PointDepiction.LocalSurface)   // Only local surface has to be updated when position changes
                        DraggingPoint.Group.UpdateDepiction();
                    RedrawNeeded = true;
                }
            }

            Options.Viewport.AreUpdatesDisabled = false;
            if (RedrawNeeded)
                Options.Viewport.Redraw();
        }

        void Membrane_MouseClick(Membrane sender, List<Intersection> intersections, System.Windows.Forms.MouseEventArgs e)
        {
            IEnumerable<Intersection> TriangleIntersections = intersections.Where(i => i.Target.GetType() == typeof(Triangle));
            IEnumerable<Intersection> PointIntersections = intersections.Where(i => i.Target.GetType() == typeof(SurfacePoint));

            bool IsPointForemost = PointIntersections.Count() > 0 ? PointIntersections.First() == intersections.First() : false;

            if (KeyboardHelper.ShiftDown() || KeyboardHelper.AltDown() || KeyboardHelper.CtrlDown())
            {
                if (TriangleIntersections.Count() > 0 && !IsPointForemost)
                {
                    Triangle ClickedTriangle = (Triangle)TriangleIntersections.First().Target;

                    if (KeyboardHelper.ShiftDown() && !KeyboardHelper.AltDown() && !KeyboardHelper.CtrlDown()) // Select triangles
                    {
                        sender.SelectTriangles(new[] { ClickedTriangle });
                    }
                    else if (KeyboardHelper.AltDown() && !KeyboardHelper.ShiftDown() && !KeyboardHelper.CtrlDown()) // Deselect triangles
                    {
                        sender.DeselectTriangles(new[] { ClickedTriangle });
                    }
                    else if (KeyboardHelper.CtrlDown() && !KeyboardHelper.ShiftDown() && !KeyboardHelper.AltDown()) // Create a surface point
                    {
                        if (sender.PreviewGroup.Points.Count == 1)  // If there is already a preview point, keep it for its possibly non-default Psi
                        {
                            SurfacePoint PreviewPoint = sender.PreviewGroup.Points[0];
                            sender.PreviewGroup.Points.RemoveAt(0);

                            sender.ActiveGroup.Points.Add(PreviewPoint);
                        }
                        else    // If no preview points, create one from scratch with Psi = 0
                        {
                            Vector3 Position = TriangleIntersections.First().Position;

                            float Offset = (float)sender.SurfaceOffset * Options.PixelScale.X;
                            Vector3 TriangleGlobal = Position - ClickedTriangle.Normal * Offset;
                            Vector3 TriangleLocal = ClickedTriangle.ToBarycentric(TriangleGlobal);

                            sender.ActiveGroup.Points.Add(new SurfacePoint(Position, ClickedTriangle, TriangleLocal, Offset, 0));
                        }

                        Options.Viewport.Redraw();
                    }
                }
            }
        }

        void Membrane_MouseDown(Membrane sender, List<Intersection> intersections, System.Windows.Forms.MouseEventArgs e)
        {
            IEnumerable<Intersection> TriangleIntersections = intersections.Where(i => i.Target.GetType() == typeof(Triangle));
            IEnumerable<Intersection> PointIntersections = intersections.Where(i => i.Target.GetType() == typeof(SurfacePoint));

            bool IsPointForemost = PointIntersections.Count() > 0 ? PointIntersections.First() == intersections.First() : false;

            if (IsPointForemost && KeyboardHelper.CtrlDown())
                DraggingPoint = (SurfacePoint)PointIntersections.First().Target;
        }

        void Membrane_MouseUp(Membrane sender, List<Intersection> intersections, System.Windows.Forms.MouseEventArgs e)
        {
            IEnumerable<Intersection> TriangleIntersections = intersections.Where(i => i.Target.GetType() == typeof(Triangle));
            IEnumerable<Intersection> PointIntersections = intersections.Where(i => i.Target.GetType() == typeof(SurfacePoint));

            bool IsPointForemost = PointIntersections.Count() > 0 ? PointIntersections.First() == intersections.First() : false;

            DraggingPoint = null;
        }

        private void Membrane_MouseWheel(Membrane membrane, List<Intersection> intersections, System.Windows.Forms.MouseEventArgs e)
        {
            IEnumerable<Intersection> TriangleIntersections = intersections.Where(i => i.Target.GetType() == typeof(Triangle));
            IEnumerable<Intersection> PointIntersections = intersections.Where(i => i.Target.GetType() == typeof(SurfacePoint));

            bool IsPointForemost = PointIntersections.Count() > 0 ? PointIntersections.First() == intersections.First() : false;

            bool RedrawNeeded = false;

            if (KeyboardHelper.CtrlDown() && IsPointForemost)   // Rotate closest point overlapping with cursor
            {
                float Mult = KeyboardHelper.ShiftDown() ? 30f : 3f;
                ((SurfacePoint)PointIntersections.First().Target).Psi -= e.Delta / 120f * Mult / 180f * (float)Math.PI;
                ((SurfacePoint)PointIntersections.First().Target).Group.PointCloud.UpdateBuffers();

                RedrawNeeded = true;
            }

            if (KeyboardHelper.CtrlDown() && membrane.PreviewGroup.Points.Count == 1)   // Rotate preview point
            {
                float Mult = KeyboardHelper.ShiftDown() ? 30f : 3f;
                membrane.PreviewGroup.Points[0].Psi -= e.Delta / 120f * Mult / 180f * (float)Math.PI;
                membrane.PreviewGroup.Points[0].Group.PointCloud.UpdateBuffers();
                Options.Viewport.Redraw();

                RedrawNeeded = true;
            }

            if (RedrawNeeded)
                Options.Viewport.Redraw();
        }

        #endregion

        #region File loading

        private void ButtonMeshPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Wavefront OBJ|*.obj",
                Multiselect = false
            };
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                Options.Membrane.LoadModel(Dialog.FileName);
            }
        }

        private void ButtonTomogramPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "MRC|*.mrc;*.rec",
                Multiselect = false
            };
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                Options.Membrane.LoadTomogram(Dialog.FileName);
            }
        }

        #endregion

        #region Triangle selection

        private void ButtonSelectionFill_Click(object sender, RoutedEventArgs e)
        {
            Options.Membrane.FillTriangleSelection();
        }

        private void ButtonSelectionGrow_Click(object sender, RoutedEventArgs e)
        {
            Options.Membrane.GrowTriangleSelection();
        }

        private void ButtonSelectionShrink_Click(object sender, RoutedEventArgs e)
        {
            Options.Membrane.ShrinkTriangleSelection();
        }

        private void ButtonSelectionInvert_Click(object sender, RoutedEventArgs e)
        {
            Options.Membrane.InvertTriangleSelection();
        }

        private void ButtonSelectionClear_Click(object sender, RoutedEventArgs e)
        {
            Options.Membrane.DeselectAllTriangles();
        }

        void Membrane_TriangleSelectionChanged(Membrane sender, List<Triangle> selection)
        {
            float Area = selection.Sum(t => t.GetVolumeArea());

            TextSelectionStats.Text = String.Format("{0} faces, {1:0.0} Å².", selection.Count, Area);
        }

        #endregion

        #region Surface point groups

        private void ButtonPointGroupsAdd_OnClick(object sender, RoutedEventArgs e)
        {
            PointGroup NewGroup = new PointGroup
            {
                Name = "Group " + (Options.Membrane.PointGroups.Count + 1), 
                Size = 10,
                Color = ColorHelper.SpectrumColor(Options.Membrane.PointGroups.Count, 0.3f)
            };
            NewGroup.PointCloud.GLContext = Options.Viewport.GetControl();
            Options.Membrane.PointGroups.Add(NewGroup);

            UpdatePointContextMenu();   // There is a new group points can be moved into
        }

        private void ButtonPointGroupsRemove_OnClick(object sender, RoutedEventArgs e)
        {
            if (ListViewPointGroups.SelectedItem != null && Options.Membrane.PointGroups.Count > 1)
            {
                PointGroup ToRemove = (PointGroup)ListViewPointGroups.SelectedItem;
                Options.Membrane.PointGroups.Remove(ToRemove);

                // Make sure ActiveGroup isn't null.
                if (Options.Membrane.ActiveGroup == ToRemove)
                    Options.Membrane.ActiveGroup = Options.Membrane.PointGroups[0];

                Options.Viewport.Redraw();
            }
        }

        private void ButtonPointGroupsImport_OnClick(object sender, RoutedEventArgs e)
        {
            if (Options.Membrane.SurfaceMesh == null || Options.Membrane.SurfaceMesh.Triangles.Count == 0)
            {
                MessageBox.Show("This will not work without a mesh.");
                return;
            }

            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "Text File|*.txt|Session File|*.xml";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                FileInfo Info = new FileInfo(Dialog.FileName);
                if (Info.Extension.ToLower().Replace(".", "") == "txt")
                {
                    PointGroup NewGroup = new PointGroup
                    {
                        Name = Dialog.SafeFileName.Substring(0, Dialog.SafeFileName.LastIndexOf(".txt")),
                        Size = 10,
                        Color = ColorHelper.SpectrumColor(Options.Membrane.PointGroups.Count, 0.3f)
                    };
                    NewGroup.PointCloud.GLContext = Options.Viewport.GetControl();

                    CultureInfo IC = CultureInfo.InvariantCulture;

                    using (TextReader Reader = new StreamReader(File.OpenRead(Dialog.FileName)))
                    {
                        string Line;

                        while ((Line = Reader.ReadLine()) != null)
                        {
                            if (Line[0] == '#')
                                continue;

                            string[] Parts = Line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (Parts.Length < 3)
                                continue;

                            try
                            {
                                Vector3 Position = new Vector3(float.Parse(Parts[0], IC), float.Parse(Parts[1], IC), float.Parse(Parts[2], IC));

                                Triangle ClosestTri = Options.Membrane.SurfaceMesh.Triangles[0];
                                float ClosestDist = float.MaxValue;
                                foreach (var tri in Options.Membrane.SurfaceMesh.Triangles)
                                {
                                    float Dist = (tri.V0.Position - Position).Length;
                                    if (Dist < ClosestDist)
                                    {
                                        ClosestDist = Dist;
                                        ClosestTri = tri;
                                    }
                                }

                                OpenTK.Matrix3 Orientation = ClosestTri.GetPlaneMatrix3();
                                if (Parts.Length >= 3 + 9)
                                {
                                    int Offset = Parts.Length - 9;
                                    Vector3 C1 = new Vector3(float.Parse(Parts[Offset + 0], IC), float.Parse(Parts[Offset + 1], IC), float.Parse(Parts[Offset + 2], IC));
                                    Vector3 C2 = new Vector3(float.Parse(Parts[Offset + 3], IC), float.Parse(Parts[Offset + 4], IC), float.Parse(Parts[Offset + 5], IC));
                                    Vector3 C3 = new Vector3(float.Parse(Parts[Offset + 6], IC), float.Parse(Parts[Offset + 7], IC), float.Parse(Parts[Offset + 8], IC));

                                    Orientation = new OpenTK.Matrix3(C1.X, C2.X, C3.X, C1.Y, C2.Y, C3.Y, C1.Z, C2.Z, C3.Z);
                                }

                                SurfacePoint NewPoint = new SurfacePoint(Position, ClosestTri, Vector3.Zero, 0, Orientation);
                                NewGroup.Points.Add(NewPoint);
                            }
                            catch
                            {
                                MessageBox.Show("Could not import:\n" + Line);
                            }
                        }
                    }

                    if (NewGroup.Points.Count > 0)
                    {
                        Options.Membrane.PointGroups.Add(NewGroup);
                        Options.Viewport.Redraw();
                    }
                }
                else if (Info.Extension.ToLower().Replace(".", "") == "xml")
                {
                    List<string> GroupNames = new List<string>();

                    using (Stream SessionStream = File.OpenRead(Dialog.FileName))
                    {
                        XPathDocument Doc = new XPathDocument(SessionStream);
                        XPathNavigator Reader = Doc.CreateNavigator();
                        Reader.MoveToRoot();


                        foreach (XPathNavigator groupNav in Reader.Select("//PointGroups/Group"))
                            GroupNames.Add(XMLHelper.LoadAttribute(groupNav, "Name", "Group " + (GroupNames.Count + 1)));
                    }

                    if (GroupNames.Count == 0)
                        return;

                    PointGroupImportDialog ImportDialog = new PointGroupImportDialog();
                    foreach (var groupName in GroupNames)
                        ImportDialog.AvailableGroups.Add(groupName);
                    ImportDialog.SessionPath = Dialog.FileName;

                    ImportDialog.ShowDialog();
                }
            }
        }

        private void ListViewPointGroups_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView Sender = (ListView)sender;
            if (Sender.SelectedItem != null)
            {
                Options.Membrane.ActiveGroup = (PointGroup)Sender.SelectedItem;
                foreach (var point in Options.Membrane.ActiveGroup.Points.Where(p => p.IsSelected))
                    ListViewPoints.SelectedItems.Add(point);
            }
        }

        private void UpdatePointContextMenu()
        {
            ContextMenuSurfacePoint.Items.Clear();
            if (Options.Membrane.PointGroups.Count <= 1)
                ContextMenuSurfacePoint.IsEnabled = false;
            else
                ContextMenuSurfacePoint.IsEnabled = true;

            foreach (var pointGroup in Options.Membrane.PointGroups)
            {
                if (pointGroup == Options.Membrane.ActiveGroup)
                    continue;

                MenuItem GroupItem = new MenuItem() { Header = pointGroup.Name };
                GroupItem.Click += (gSender, gE) =>
                {
                    List<SurfacePoint> Selected = ListViewPoints.SelectedItems.Cast<SurfacePoint>().ToList();

                    // First, remove points from their current group
                    foreach (var group in Selected.GroupBy(p => p.Group))
                        foreach (var point in group)
                            group.Key.Points.Remove(point);

                    foreach (var point in Selected)
                    {
                        point.IsSelected = false;
                        pointGroup.Points.Add(point);
                    }

                    Options.Viewport.Redraw();
                };

                ContextMenuSurfacePoint.Items.Add(GroupItem);
            }
        }

        private void ColorPickerPointGroup_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            Options.Viewport.Redraw();
        }

        private void ListViewPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                foreach (var group in ListViewPoints.SelectedItems.Cast<SurfacePoint>().GroupBy(p => p.Group))
                    group.Key.PointCloud.SelectPoints(group);
            else if (e.RemovedItems.Count > 0)
                foreach (var group in e.RemovedItems.Cast<SurfacePoint>().GroupBy(p => p.Group))
                    if (group.Key == Options.Membrane.ActiveGroup)  // When changing active group, previous selection will also be under RemovedItems, so ignore it.
                        group.Key.PointCloud.SelectPoints(null);

            Options.Viewport.Redraw();
        }

        private void ButtonPointsExport_Click(object sender, RoutedEventArgs e)
        {
            if (Options.Membrane.ActiveGroup.Points.Count == 0)
                return;

            System.Windows.Forms.SaveFileDialog Dialog = new System.Windows.Forms.SaveFileDialog();
            Dialog.FileName += Options.Membrane.ActiveGroup.Name + ".txt";
            Dialog.Filter = "Text File|*.txt";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                using(TextWriter Writer = File.CreateText(Dialog.FileName))
                {
                    Writer.WriteLine("#PositionX\tPositionY\tPositionZ\tVolumeX\tVolumeY\tVolumeZ\tOffsetFromFace\tm11\tm21\tm31\tm12\tm22\tm32\tm13\tm23\tm33");

                    foreach (SurfacePoint p in Options.Membrane.ActiveGroup.Points)
                        if (ListViewPoints.SelectedItems.Count == 0 || p.IsSelected) // If nothing is selected, export all; otherwise only selected.
                        {
                            string Line = "";
                            Line += p.Position.X.ToString(CultureInfo.InvariantCulture) + "\t";
                            Line += p.Position.Y.ToString(CultureInfo.InvariantCulture) + "\t";
                            Line += p.Position.Z.ToString(CultureInfo.InvariantCulture) + "\t";

                            if (Options.Membrane.TomogramTexture != null)
                            {
                                Vector3 VolumePos = p.Position - Options.Membrane.TomogramTexture.Offset;
                                VolumePos.X /= Options.Membrane.TomogramTexture.Scale.X;
                                VolumePos.Y /= Options.Membrane.TomogramTexture.Scale.Y;
                                VolumePos.Z /= Options.Membrane.TomogramTexture.Scale.Z;
                                Line += VolumePos.X.ToString(CultureInfo.InvariantCulture) + "\t";
                                Line += VolumePos.Y.ToString(CultureInfo.InvariantCulture) + "\t";
                                Line += VolumePos.Z.ToString(CultureInfo.InvariantCulture) + "\t";
                            }
                            else
                            {
                                Line += p.Position.X.ToString(CultureInfo.InvariantCulture) + "\t";
                                Line += p.Position.Y.ToString(CultureInfo.InvariantCulture) + "\t";
                                Line += p.Position.Z.ToString(CultureInfo.InvariantCulture) + "\t";
                            }

                            Line += p.SurfaceOffset + "\t";

                            Line += p.TransformedMatrix.Column0.X + "\t";
                            Line += p.TransformedMatrix.Column0.Y + "\t";
                            Line += p.TransformedMatrix.Column0.Z + "\t";

                            Line += p.TransformedMatrix.Column1.X + "\t";
                            Line += p.TransformedMatrix.Column1.Y + "\t";
                            Line += p.TransformedMatrix.Column1.Z + "\t";

                            Line += p.TransformedMatrix.Column2.X + "\t";
                            Line += p.TransformedMatrix.Column2.Y + "\t";
                            Line += p.TransformedMatrix.Column2.Z;

                            Writer.WriteLine(Line);
                        }
                }
            }
        }

        private void ButtonPointsRemove_Click(object sender, RoutedEventArgs e)
        {
            List<SurfacePoint> Selected = ListViewPoints.SelectedItems.Cast<SurfacePoint>().ToList();

            foreach (var group in Selected.GroupBy(p => p.Group))
                foreach (var point in group)
                    group.Key.Points.Remove(point);

            Options.Viewport.Redraw();
        }

        private void ButtonPointGroupDepiction_OnClick(object sender, RoutedEventArgs e)
        {
            PointGroupDepictionDialog DepictionOptions = new PointGroupDepictionDialog();
            PointGroup Sender = (PointGroup)((Button)sender).DataContext;
            DepictionOptions.DataContext = Sender;
            DepictionOptions.Topmost = true;
            DepictionOptions.Show();
        }

        #endregion

        #region Patches

        private void ListViewPatches_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ColorPickerPatch_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

        }

        private void CheckPatchColored_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonPatchAdd_OnClick(object sender, RoutedEventArgs e)
        {
            List<Triangle> PatchTriangles = Options.Membrane.GetSelectedTriangles();
            if (PatchTriangles.Count == 0)
                return;

            Options.Membrane.DeselectAllTriangles();

            Color PatchColor = ColorHelper.SpectrumColor(Options.Membrane.Patches.Count, 0.2f);
            SurfacePatch NewPatch = new SurfacePatch(Options.Membrane, "Patch " + (Options.Membrane.Patches.Count + 1), PatchColor, PatchTriangles);
            Options.Membrane.Patches.Add(NewPatch);

            Options.Membrane.SetTriangleColor(PatchTriangles, ColorHelper.ColorToVector(PatchColor));
            Options.Membrane.SetTrianglePatch(PatchTriangles, NewPatch);

            Options.Viewport.GetControl().MakeCurrent();
            Options.Membrane.SurfaceMesh.UpdateBuffers();
            Options.Viewport.Redraw();
        }

        private void ButtonPatchRemove_OnClick(object sender, RoutedEventArgs e)
        {
            List<SurfacePatch> Selected = ListViewPatches.SelectedItems.Cast<SurfacePatch>().ToList();
            foreach (var item in Selected)
            {
                Options.Membrane.SetTriangleColor(item.OriginalToTransformed.Keys, new Vector4(0));
                Options.Membrane.SetTrianglePatch(item.OriginalToTransformed.Keys, null);
                Options.Membrane.Patches.Remove(item);
            }

            Options.Viewport.Redraw();
        }

        private void ButtonPatchShow_OnClick(object sender, RoutedEventArgs e)
        {
            SurfacePatch Patch = (SurfacePatch)((FrameworkElement)sender).DataContext;
            Patch.CreateWindow();
            Patch.ParentWindow.Owner = this;
            Patch.ParentWindow.Closing += ParentWindow_Closing;

            Options.Membrane.DisplayedPatches.Add(Patch);
        }

        private void ParentWindow_Closing(object sender, CancelEventArgs e)
        {
            SurfacePatch Patch = (SurfacePatch)((PatchWindow)sender).DataContext;
            Options.Membrane.DisplayedPatches.Remove(Patch);
        }

        #endregion

        #region Session load/save

        private void ButtonLoadSession_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "XML File|*.xml";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                Options.Viewport.AreUpdatesDisabled = true;

                using (Stream SessionStream = File.OpenRead(Dialog.FileName))
                {
                    XPathDocument Doc = new XPathDocument(SessionStream);
                    XPathNavigator Reader = Doc.CreateNavigator();
                    Reader.MoveToRoot();

                    string ModelPath = XMLHelper.LoadParamNode(Reader, "ModelPath", "");
                    Options.Membrane.LoadModel(ModelPath);

                    string TomogramPath = XMLHelper.LoadParamNode(Reader, "TomogramPath", "");
                    Options.Membrane.LoadTomogram(TomogramPath);

                    // Point groups
                    if (Reader.Select("//PointGroups/Group").Count > 0)
                    {
                        Options.Membrane.ActiveGroup = null;
                        Options.Membrane.PointGroups.Clear();
                    }

                    foreach (XPathNavigator groupNav in Reader.Select("//PointGroups/Group"))
                    {
                        PointGroup NewGroup = new PointGroup
                        {
                            Name = XMLHelper.LoadAttribute(groupNav, "Name", "Group " + (Options.Membrane.PointGroups.Count + 1)),
                            Size = XMLHelper.LoadAttribute(groupNav, "Size", 10),
                            Color = ColorHelper.LoadAttribute(groupNav, "Color", ColorHelper.SpectrumColor(Options.Membrane.PointGroups.Count, 0.3f))
                        };
                        NewGroup.PointCloud.GLContext = Options.Viewport.GetControl();

                        foreach (XPathNavigator pointNav in groupNav.SelectChildren("Point", ""))
                        {
                            int TriangleID = XMLHelper.LoadAttribute(pointNav, "ID", 0);
                            SurfacePoint NewPoint = new SurfacePoint(OpenGLHelper.LoadAttribute(pointNav, "Position", new Vector3(0)),
                                                                     Options.Membrane.SurfaceMesh.Triangles[TriangleID < Options.Membrane.SurfaceMesh.Triangles.Count ? TriangleID : 0],
                                                                     OpenGLHelper.LoadAttribute(pointNav, "Barycentric", new Vector3(0)),
                                                                     XMLHelper.LoadAttribute(pointNav, "Offset", 0f),
                                                                     OpenGLHelper.LoadAttribute(pointNav, "Orientation", new Vector3(0)).X);
                            NewGroup.Points.Add(NewPoint);
                        }

                        Options.Membrane.PointGroups.Add(NewGroup);
                        NewGroup.IsVisible = XMLHelper.LoadAttribute(groupNav, "IsVisible", true);

                        NewGroup.DepictionMeshPath = XMLHelper.LoadAttribute(groupNav, "DepictionMeshPath", "");
                        NewGroup.DepictionMeshLevel = XMLHelper.LoadAttribute(groupNav, "DepictionMeshLevel", 0.02M);
                        NewGroup.DepictionMeshOffset = XMLHelper.LoadAttribute(groupNav, "DepictionMeshOffset", 0M);
                        NewGroup.DepictionLocalSurfaceLevel = XMLHelper.LoadAttribute(groupNav, "DepictionLocalSurfaceLevel", 0.02M);
                        NewGroup.DepictionLocalSurfaceInvert = XMLHelper.LoadAttribute(groupNav, "DepictionLocalSurfaceInvert", true);
                        NewGroup.DepictionLocalSurfaceOnlyCenter = XMLHelper.LoadAttribute(groupNav, "DepictionLocalSurfaceOnlyCenter", true);
                        NewGroup.Depiction = (PointDepiction)XMLHelper.LoadAttribute(groupNav, "Depiction", 1);
                    }

                    if (Reader.Select("//PointGroups/Group").Count > 0)
                    {
                        Options.Membrane.ActiveGroup = Options.Membrane.PointGroups[0];
                    }

                    // Surface patches
                    Options.Membrane.DeselectAllTriangles();
                    foreach (XPathNavigator patchNav in Reader.Select("//SurfacePatches/Patch"))
                    {
                        List<Triangle> Triangles = XMLHelper.LoadAttribute(patchNav.SelectSingleNode("Faces"), "ID", "").
                            Split(',').
                            Select(v => Options.Membrane.SurfaceMesh.Triangles[int.Parse(v) < Options.Membrane.SurfaceMesh.Triangles.Count ? int.Parse(v) : 0]).ToList();

                        if (Triangles.Count == 0)
                            continue;

                        Color PatchColor = ColorHelper.LoadAttribute(patchNav, "Color", ColorHelper.SpectrumColor(Options.Membrane.Patches.Count, 0.2f));
                        SurfacePatch NewPatch = new SurfacePatch(Options.Membrane,
                                                                 XMLHelper.LoadAttribute(patchNav, "Name", "Patch " + (Options.Membrane.Patches.Count + 1)),
                                                                 PatchColor,
                                                                 Triangles);
                        Options.Membrane.Patches.Add(NewPatch);

                        Options.Membrane.SetTriangleColor(Triangles, ColorHelper.ColorToVector(PatchColor));
                        Options.Membrane.SetTrianglePatch(Triangles, NewPatch);

                        NewPatch.IsVisible = XMLHelper.LoadAttribute(patchNav, "IsVisible", true);
                        NewPatch.IsColored = XMLHelper.LoadAttribute(patchNav, "IsColored", true);
                    }

                    Options.Viewport.GetControl().MakeCurrent();
                    Options.Membrane.SurfaceMesh.UpdateBuffers();

                    // Options
                    Options.Membrane.SurfaceOffset = XMLHelper.LoadParamNode(Reader, "SurfaceOffset", 0M);

                    Options.Membrane.TraceDepth = XMLHelper.LoadParamNode(Reader, "TraceDepth", 0);
                    Options.Membrane.TraceDepthOffset = XMLHelper.LoadParamNode(Reader, "TraceDepthOffset", 0);
                    Options.Membrane.TraceSharpening = XMLHelper.LoadParamNode(Reader, "TraceSharpening", 0M);
                    
                    Options.Membrane.OutputRangeMin = XMLHelper.LoadParamNode(Reader, "OutputRangeMin", 0M);
                    Options.Membrane.OutputRangeMax = XMLHelper.LoadParamNode(Reader, "OutputRangeMax", 1M);
                    Options.Membrane.OutputLight = XMLHelper.LoadParamNode(Reader, "OutputLight", 30);

                    Options.Membrane.SelectionAngle = XMLHelper.LoadParamNode(Reader, "SelectionAngle", 90);

                    Options.Viewport.Camera.Target = OpenGLHelper.LoadParamNode(Reader, "CameraTarget", new Vector3(0));
                    Options.Viewport.Camera.Rotation = OpenGLHelper.LoadParamNode(Reader, "CameraRotation", new Quaternion());
                    Options.Viewport.Camera.Distance = XMLHelper.LoadParamNode(Reader, "CameraDistance", 1f);
                    Options.Viewport.Camera.FOV = XMLHelper.LoadParamNode(Reader, "CameraFOV", (decimal)(45f / 180f * (float)Math.PI));
                }

                Options.Viewport.AreUpdatesDisabled = false;
                Options.Viewport.Redraw();
            }
        }

        private void ButtonSaveSession_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog Dialog = new System.Windows.Forms.SaveFileDialog();
            Dialog.Filter = "XML File|*.xml";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                XmlTextWriter Writer = new XmlTextWriter(Dialog.FileName, Encoding.Unicode);
                Writer.Formatting = Formatting.Indented;
                Writer.IndentChar = '\t';
                Writer.Indentation = 1;
                Writer.WriteStartDocument();

                Writer.WriteStartElement("Session");

                // Paths
                XMLHelper.WriteParamNode(Writer, "ModelPath", Options.Membrane.PathModel);
                XMLHelper.WriteParamNode(Writer, "TomogramPath", Options.Membrane.PathTomogram);
                
                // Surface modifications
                XMLHelper.WriteParamNode(Writer, "SurfaceOffset", Options.Membrane.SurfaceOffset);

                // Tracing
                XMLHelper.WriteParamNode(Writer, "TraceDepth", Options.Membrane.TraceDepth);
                XMLHelper.WriteParamNode(Writer, "TraceDepthOffset", Options.Membrane.TraceDepthOffset);
                XMLHelper.WriteParamNode(Writer, "TraceSharpening", Options.Membrane.TraceSharpening);

                // Rendering
                XMLHelper.WriteParamNode(Writer, "OutputRangeMin", Options.Membrane.OutputRangeMin);
                XMLHelper.WriteParamNode(Writer, "OutputRangeMax", Options.Membrane.OutputRangeMax);
                XMLHelper.WriteParamNode(Writer, "OutputLight", Options.Membrane.OutputLight);

                // Selection
                XMLHelper.WriteParamNode(Writer, "SelectionAngle", Options.Membrane.SelectionAngle);

                // Camera
                OpenGLHelper.WriteParamNode(Writer, "CameraTarget", Options.Viewport.Camera.Target);
                OpenGLHelper.WriteParamNode(Writer, "CameraRotation", Options.Viewport.Camera.Rotation);
                XMLHelper.WriteParamNode(Writer, "CameraDistance", Options.Viewport.Camera.Distance);
                XMLHelper.WriteParamNode(Writer, "CameraFOV", Options.Viewport.Camera.FOV);

                {
                    Writer.WriteStartElement("PointGroups");
                    {
                        foreach (var group in Options.Membrane.PointGroups)
                        {
                            Writer.WriteStartElement("Group");
                            Writer.WriteAttributeString("Name", group.Name);
                            Writer.WriteAttributeString("Color", ColorHelper.ColorToString(group.Color));
                            Writer.WriteAttributeString("IsVisible", group.IsVisible.ToString(CultureInfo.InvariantCulture));
                            Writer.WriteAttributeString("Size", group.Size.ToString(CultureInfo.InvariantCulture));

                            Writer.WriteAttributeString("Depiction", ((int)group.Depiction).ToString(CultureInfo.InvariantCulture));

                            Writer.WriteAttributeString("DepictionMeshPath", group.DepictionMeshPath);
                            Writer.WriteAttributeString("DepictionMeshLevel", group.DepictionMeshLevel.ToString(CultureInfo.InvariantCulture));
                            Writer.WriteAttributeString("DepictionMeshOffset", group.DepictionMeshOffset.ToString(CultureInfo.InvariantCulture));

                            Writer.WriteAttributeString("DepictionLocalSurfaceLevel", group.DepictionLocalSurfaceLevel.ToString(CultureInfo.InvariantCulture));
                            Writer.WriteAttributeString("DepictionLocalSurfaceInvert", group.DepictionLocalSurfaceInvert.ToString(CultureInfo.InvariantCulture));
                            Writer.WriteAttributeString("DepictionLocalSurfaceOnlyCenter", group.DepictionLocalSurfaceOnlyCenter.ToString(CultureInfo.InvariantCulture));

                            foreach (var point in group.Points)
                            {
                                Writer.WriteStartElement("Point");
                                Writer.WriteAttributeString("ID", point.Face.ID.ToString(CultureInfo.InvariantCulture));
                                Writer.WriteAttributeString("Position", OpenGLHelper.Vector3ToString(point.Position));
                                Writer.WriteAttributeString("Offset", point.SurfaceOffset.ToString(CultureInfo.InvariantCulture));
                                Writer.WriteAttributeString("Barycentric", OpenGLHelper.Vector3ToString(point.BarycentricCoords));
                                Writer.WriteAttributeString("Orientation", OpenGLHelper.Vector3ToString(new Vector3(point.Psi, 0, 0)));

                                Writer.WriteEndElement();
                            }

                            Writer.WriteEndElement();
                        }
                    }
                    Writer.WriteEndElement();   // PointGroups

                    Writer.WriteStartElement("SurfacePatches");
                    {
                        foreach (var patch in Options.Membrane.Patches)
                        {
                            Writer.WriteStartElement("Patch");
                            Writer.WriteAttributeString("Name", patch.Name);
                            Writer.WriteAttributeString("Color", ColorHelper.ColorToString(patch.Color));
                            Writer.WriteAttributeString("IsColored", patch.IsColored.ToString(CultureInfo.InvariantCulture));
                            Writer.WriteAttributeString("IsVisible", patch.IsVisible.ToString(CultureInfo.InvariantCulture));

                            Writer.WriteStartElement("Faces");
                            Writer.WriteAttributeString("ID", string.Join(",", patch.SurfaceMesh.Triangles.Select(t => t.ID.ToString(CultureInfo.InvariantCulture))));
                            Writer.WriteEndElement();

                            Writer.WriteEndElement();
                        }
                    }
                    Writer.WriteEndElement();   // SurfacePatches
                }
                Writer.WriteEndElement();   // Session

                Writer.WriteEndDocument();
                Writer.Flush();
                Writer.Close();
            }
        }

        #endregion
    }
}
