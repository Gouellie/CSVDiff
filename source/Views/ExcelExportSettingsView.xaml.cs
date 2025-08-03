using System;
using System.Collections.Generic;
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

namespace CSVDiff.Views
{
    /// <summary>
    /// Interaction logic for ExcelExportSettingsView.xaml
    /// </summary>
    public partial class ExcelExportSettingsView : Window
    {
        public ExcelExportSettingsView(Models.ExcelExportSettings excelExportSettings)
        {
            InitializeComponent();
            Owner = MainWindow.Instance;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DataContext = excelExportSettings;
        }
    }
}
