using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.XPath;
using OpenTK;
using Warp.Tools;
using Membranogram.Helpers;

namespace Membranogram.Controls
{
    /// <summary>
    /// Interaction logic for PointGroupImportDialog.xaml
    /// </summary>
    public partial class PointGroupImportDialog : Window
    {
        public ObservableCollection<string> AvailableGroups = new ObservableCollection<string>();
        public string SessionPath = "";

        public PointGroupImportDialog()
        {
            InitializeComponent();

            AvailableGroups.CollectionChanged += AvailableGroups_CollectionChanged;
        }

        private void AvailableGroups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var group in e.NewItems)
            {
                ListGroups.Items.Add(group);
            }
        }

        private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
        {
            List<int> SelectedGroups = new List<int>();
            for (int i = 0; i < AvailableGroups.Count; i++)
            {
                foreach (var selectedItem in ListGroups.SelectedItems)
                    if ((string)selectedItem == AvailableGroups[i])
                        SelectedGroups.Add(i);
            }

            using (Stream SessionStream = File.OpenRead(SessionPath))
            {
                XPathDocument Doc = new XPathDocument(SessionStream);
                XPathNavigator Reader = Doc.CreateNavigator();
                Reader.MoveToRoot();

                int iGroup = 0;
                foreach (XPathNavigator groupNav in Reader.Select("//PointGroups/Group"))
                {
                    if (SelectedGroups.Contains(iGroup))
                    {
                        PointGroup NewGroup = new PointGroup
                        {
                            Name = XMLHelper.LoadAttribute(groupNav, "Name", "Group " + (MainWindow.Options.Membrane.PointGroups.Count + 1)),
                            Size = XMLHelper.LoadAttribute(groupNav, "Size", 10),
                            Color = ColorHelper.LoadAttribute(groupNav, "Color", ColorHelper.SpectrumColor(MainWindow.Options.Membrane.PointGroups.Count, 0.3f))
                        };
                        NewGroup.PointCloud.GLContext = MainWindow.Options.Viewport.GetControl();

                        foreach (XPathNavigator pointNav in groupNav.SelectChildren("Point", ""))
                        {
                            int TriangleID = XMLHelper.LoadAttribute(pointNav, "ID", 0);
                            SurfacePoint NewPoint = new SurfacePoint(OpenGLHelper.LoadAttribute(pointNav, "Position", new Vector3(0)),
                                                                     MainWindow.Options.Membrane.SurfaceMesh.Triangles[TriangleID < MainWindow.Options.Membrane.SurfaceMesh.Triangles.Count ? TriangleID : 0],
                                                                     OpenGLHelper.LoadAttribute(pointNav, "Barycentric", new Vector3(0)),
                                                                     XMLHelper.LoadAttribute(pointNav, "Offset", 0f),
                                                                     OpenGLHelper.LoadAttribute(pointNav, "Orientation", new Vector3(0)).X);
                            NewGroup.Points.Add(NewPoint);
                        }

                        MainWindow.Options.Membrane.PointGroups.Add(NewGroup);
                        NewGroup.IsVisible = XMLHelper.LoadAttribute(groupNav, "IsVisible", true);
                    }

                    iGroup++;
                }
            }

            if (SelectedGroups.Count > 0)
                MainWindow.Options.Viewport.Redraw();

            Close();
        }
    }
}
