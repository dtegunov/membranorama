using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Membranogram.Controls
{
    /// <summary>
    /// Interaction logic for PointGroupDepictionDialog.xaml
    /// </summary>
    public partial class PointGroupDepictionDialog : MahApps.Metro.Controls.MetroWindow
    {
        public PointGroupDepictionDialog()
        {
            InitializeComponent();

            DataContextChanged += PointGroupDepictionDialog_DataContextChanged;
            Closing += PointGroupDepictionDialog_Closing;
        }

        private void PointGroupDepictionDialog_Closing(object sender, CancelEventArgs e)
        {
            DataContext = null;
        }

        private void PointGroupDepictionDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
                ((PointGroup)e.NewValue).PropertyChanged += PointGroup_PropertyChanged;
            if (e.OldValue != null)
                ((PointGroup)e.OldValue).PropertyChanged -= PointGroup_PropertyChanged;
        }

        private void PointGroup_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void ButtonModelPath_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog Dialog = new System.Windows.Forms.OpenFileDialog();
            Dialog.Filter = "MRC Map|*.mrc|OBJ Mesh|*.obj";
            System.Windows.Forms.DialogResult Result = Dialog.ShowDialog();

            if (Result.ToString() == "OK")
            {
                if (DataContext != null)
                    ((PointGroup)DataContext).DepictionMeshPath = Dialog.FileName;
            }
        }
    }
}
