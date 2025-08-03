
using System.Windows.Media;

namespace CSVDiff.Models
{
    public class ColumnSettings
    {
        public int MergeGroup { get; set; }
        public double ColWidth { get; set; }
        public int FontSize { get; set; } = 16;
        public bool WrapText { get; set; }
        public bool Bold { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Brush? MergeGroupColor => new SolidColorBrush(Helpers.GetNewRandomColor(MergeGroup));
    }

    public class ExcelExportSettings
    {
        public double RowHeight { get; set; }
        public List<ColumnSettings> ColumnSettings { get; set; } = [];
    }

    public class UserSettings
    {
        public string OptionalJoinFileFullName { get; set; } = string.Empty;
        public List<string> JoinOnColumnList { get; set; } = [];
        public List<string> DiffOnColumnList { get; set; } = [];
        public List<ViewModel.MergeableColumnViewModel> MergeableColumnList { get; set; } = [];
        public ExcelExportSettings ExcelExportSettings { get; set; } = new();
    }
}
