using CSVDiff.ViewModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSVDiff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnMergeSelection_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            if (MergeableColumnListView.SelectedItems.Count == 0)
                return;

            var selection = MergeableColumnListView.SelectedItems.OfType<MergeableColumnViewModel>().ToArray();
            ViewModel.MergeColumns(selection);
        }

        private void OnUnmergeSelection_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            if (MergeableColumnListView.SelectedItems.Count == 0)
                return;

            var selection = MergeableColumnListView.SelectedItems.OfType<MergeableColumnViewModel>().ToArray();
            ViewModel.UnmergeColumns(selection);
        }
    }
}