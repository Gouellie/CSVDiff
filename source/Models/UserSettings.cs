
namespace CSVDiff.Models
{
    public class UserSettings
    {
        public string OptionalJoinFileFullName { get; set; } = string.Empty;
        public List<string> JoinOnColumnList { get; set; } = [];
        public List<string> DiffOnColumnList { get; set; } = [];
        public List<ViewModel.MergeableColumnViewModel> MergeableColumnList { get; set; } = [];
    }
}
