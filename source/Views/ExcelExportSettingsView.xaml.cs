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
        public Models.ExcelExportSettings UpdatedSettings { get; }
        public bool Save { get; private set; }

        public ExcelExportSettingsView(Models.ExcelExportSettings excelExportSettings)
        {
            InitializeComponent();
            Owner = MainWindow.Instance;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            UpdatedSettings = new Models.ExcelExportSettings(excelExportSettings);
            DataContext = UpdatedSettings;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Accept_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void AcceptAndSave_Button_Click(object sender, RoutedEventArgs e)
        {
            Save = true;
            DialogResult = true;
        }
    }
}
