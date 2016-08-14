using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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

namespace Membranogram
{
    /// <summary>
    /// Interaction logic for PatchWindow.xaml
    /// </summary>
    public partial class PatchWindow : MahApps.Metro.Controls.MetroWindow
    {
        public Viewport Viewport;
        GridLength ControlsHeight;
        bool IsPlanarizing = false;

        public PatchWindow()
        {
            Viewport = new Viewport(this);

            InitializeComponent();
        }

        private void ViewportHost_Initialized(object sender, EventArgs e)
        {
            ViewportHost.Child = Viewport.GetControl();
        }

        private void ButtonHideControls_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonHideControls.Visibility = Visibility.Collapsed;
            ButtonShowControls.Visibility = Visibility.Visible;
            ControlsHeight = RowControls.Height;
            RowControls.Height = new GridLength(0);
        }

        private void ButtonShowControls_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonHideControls.Visibility = Visibility.Visible;
            ButtonShowControls.Visibility = Visibility.Collapsed;
            RowControls.Height = ControlsHeight;
        }

        private void ButtonPlanarization_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsPlanarizing)
            {
                ((SurfacePatch) DataContext).StartPlanarization();
                ButtonPlanarization.Content = "STOP PLANARIZATION";
                IsPlanarizing = true;
            }
            else
            {
                ((SurfacePatch)DataContext).StopPlanarization();
                ButtonPlanarization.Content = "START PLANARIZATION";
                IsPlanarizing = false;
            }
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
                Viewport.GrabScreenshot(Dialog.FileName);
            }
        }
    }
}
