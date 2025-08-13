
using System.Windows.Media;

namespace CSVDiff.Models
{
    public enum ExcelHorizontalAlignment { General, Left, Center, Right, Fill, Distributed, Justify }
    public enum ExcelVerticalAlignment { Top, Center, Bottom, Distributed, Justify }

    public class ColumnSettings
    {
        public int MergeGroup { get; set; }
        public double ColWidth { get; set; }
        public int FontSize { get; set; } = 16;
        public bool WrapText { get; set; }
        public bool Bold { get; set; }
        public ExcelHorizontalAlignment HorizontalAlignment { get; set; } = ExcelHorizontalAlignment.General;
        public ExcelVerticalAlignment VerticalAlignment { get; set; } = ExcelVerticalAlignment.Bottom;

        [Newtonsoft.Json.JsonIgnore]
        public Brush? MergeGroupColor => new SolidColorBrush(Helpers.GetNewRandomColor(MergeGroup));

        public ColumnSettings() { }
        public ColumnSettings(ColumnSettings columnSettings) 
        {
            MergeGroup = columnSettings.MergeGroup;
            ColWidth = columnSettings.ColWidth;
            FontSize = columnSettings.FontSize;
            WrapText = columnSettings.WrapText;
            Bold = columnSettings.Bold;
            HorizontalAlignment = columnSettings.HorizontalAlignment;
            VerticalAlignment = columnSettings.VerticalAlignment;
        }
    }

    public class ExcelExportSettings
    {
        public double RowHeight { get; set; }
        public List<ColumnSettings> ColumnSettings { get; set; } = [];
        public ExcelExportSettings() { }
        public ExcelExportSettings(ExcelExportSettings exportSettings)
        {
            RowHeight = exportSettings.RowHeight;
            ColumnSettings = [.. exportSettings.ColumnSettings.Select(c => new ColumnSettings(c))];
        }
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
