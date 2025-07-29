using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSVDiff.Managers;
using CSVDiff.Models;
using CsvHelper;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CSVDiff.ViewModel
{
#pragma warning disable CS8618
    internal class MainWindowViewModel : ObservableObject
    {
        private const string INVALID = "<multi-values>";
        public static string VersionString => Version.VersionString;
        public RelayCommand<string> LoadFileCommand { get; }
        public RelayCommand<string> ClearFileCommand { get; }
        public ICommand SwapFilesCommand { get; }
        public ICommand CompareCommand { get; }
        public ICommand SaveUserSettingsCommand { get; }
        public ICommand ExportDiffCommand { get; }

        private FileViewModel? _previousFile;
        public FileViewModel? PreviousFile 
        { 
            get => _previousFile;
            private set 
            { 
                if (SetProperty(ref _previousFile, value))
                {
                    RefreshMatchList();
                } 
            } 
        }

        private FileViewModel? _latestFile;
        public FileViewModel? LatestFile 
        { 
            get => _latestFile;
            private set
            {
                if (SetProperty(ref _latestFile, value))
                {
                    RefreshMatchList();
                }
            }
        }

        private FileViewModel? _optionalJoinFile;
        public FileViewModel? OptionalJoinFile{ get => _optionalJoinFile; set => SetProperty(ref _optionalJoinFile, value); }

        private bool _isValidColumnMatchFound;
        public bool IsValidColumnMatchFound { get => _isValidColumnMatchFound; set => SetProperty(ref _isValidColumnMatchFound, value); }

        public ObservableCollection<ColumnViewModel> JoinOnColumnList { get; } = [new GhostColumn()];
        public ObservableCollection<ColumnViewModel> DiffOnColumnList { get; } = [new GhostColumn()];
        public ObservableCollection<MergeableColumnViewModel> MergeableColumnList { get; } = [];

        private bool SuspendRefreshMatchList;

        private DataTable? _diffResult;
        public DataTable? DiffResult 
        {
            get => _diffResult;
            private set 
            {
                if (SetProperty(ref _diffResult, value))
                {
                    RefreshMergeColumnList();
                }
            } 
        }

        private SettingsManager SettingsManager { get; }

        public MainWindowViewModel()
        {
            CompareCommand = new RelayCommand(Compare);

            ClearFileCommand = new RelayCommand<string>(ClearFile);
            LoadFileCommand = new RelayCommand<string>(LoadFile);

            SwapFilesCommand = new RelayCommand(SwapFiles);
            ExportDiffCommand = new RelayCommand(ExportDiff);
            SaveUserSettingsCommand = new RelayCommand(SaveUserSettings);

            SettingsManager = new SettingsManager();

            LoadOptionalFileIfFound();
        }

        private bool UpdateSettings()
        {
            if (MessageBox.Show(MainWindow.Instance, "Do you wish to override the Settings on disk?", "Update Settings", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return false;

            if (SettingsManager.UserSettings is UserSettings settings)
            {
                settings.OptionalJoinFileFullName = OptionalJoinFile?.FileInfo.FullName ?? string.Empty;
                settings.JoinOnColumnList = [.. JoinOnColumnList.Where(c => c.Selected).Select(l => l.Name)];
                settings.DiffOnColumnList = [.. DiffOnColumnList.Where(c => c.Selected).Select(l => l.Name)];
                settings.MergeableColumnList = [.. MergeableColumnList];
                return true;
            }

            return false;
        }

        private void LoadOptionalFileIfFound()
        {
            if (SettingsManager.UserSettings is not UserSettings settings)
                return;

            if (string.IsNullOrWhiteSpace(settings.OptionalJoinFileFullName))
                return;

            if (System.IO.File.Exists(settings.OptionalJoinFileFullName))
            {
                if (TryPeekAtCVS(settings.OptionalJoinFileFullName, out var optionalJoinFile))
                {
                    OptionalJoinFile = optionalJoinFile;
                }
            }
        }

        private void SaveUserSettings()
        {
            if (UpdateSettings())
            {
                SettingsManager?.SaveUserSettings();
            }
        }

        private void LoadFile(string? file)
        {
            switch (file)
            {
                case "previous":
                    PreviousFile = LoadFile();
                    break;
                case "latest":
                    LatestFile = LoadFile();
                    break;
                case "optional":
                    OptionalJoinFile = LoadFile();
                    break;
            }
        }

        private void ClearFile(string? file)
        {
            switch (file)
            {
                case "previous":
                    PreviousFile = null;
                    break;
                case "latest":
                    LatestFile = null;
                    break;
                case "optional":
                    OptionalJoinFile = null;
                    break;
            }
        }

        private void SwapFiles()
        {
            SuspendRefreshMatchList = true;
            (LatestFile, PreviousFile) = (PreviousFile, LatestFile);
            SuspendRefreshMatchList = false;
        }

        private void RefreshMatchList()
        {
            if (SuspendRefreshMatchList)
                return;

            IsValidColumnMatchFound = false;

            if (PreviousFile == null || LatestFile == null)
            {
                JoinOnColumnList.Clear();
                DiffOnColumnList.Clear();
                JoinOnColumnList.Add(new GhostColumn());
                DiffOnColumnList.Add(new GhostColumn());
                return;
            }

            List<string> matches = [.. LatestFile.Headers.Where(PreviousFile.Headers.Contains)];
            if (JoinOnColumnList.Count == matches.Count && matches.All(m => JoinOnColumnList.Select(c => c.Name).Contains(m)))
                return;

            JoinOnColumnList.Clear();
            DiffOnColumnList.Clear();

            foreach (var match in matches)
            {
                JoinOnColumnList.Add(new ColumnViewModel(match));
                DiffOnColumnList.Add(new ColumnViewModel(match));
            }
            
            if (SettingsManager.UserSettings is UserSettings settings)
            {
                foreach (var col in JoinOnColumnList)
                {
                    col.Selected = settings.JoinOnColumnList.Contains(col.Name);
                }
                foreach (var col in DiffOnColumnList)
                {
                    col.Selected = settings.DiffOnColumnList.Contains(col.Name);
                }
            }

            IsValidColumnMatchFound = JoinOnColumnList.Count > 0;

            if (!IsValidColumnMatchFound)
            {
                JoinOnColumnList.Add(new GhostColumn());
                DiffOnColumnList.Add(new GhostColumn());
            }

            DiffResult = null;
        }

        private void RefreshMergeColumnList()
        {
            MergeableColumnList.Clear();
            if (DiffResult == null)
                return;

            for (int col = 0;  col < DiffResult.Columns.Count; col++)
            {
                SolidColorBrush brush = new(Helpers.GetNewRandomColor(col));
                MergeableColumnList.Add(new MergeableColumnViewModel(DiffResult.Columns[col].ColumnName, col, brush) { Selected = true });
            }
        }

        private static bool TryBrowseForFile(out string selectedFilepath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilepath = openFileDialog.FileName;
                return true;
            }

            selectedFilepath = string.Empty;
            return false;
        }

        private static FileViewModel? LoadFile()
        {
            try
            {
                if (TryBrowseForFile(out string selectedFilepath))
                {
                    if (TryPeekAtCVS(selectedFilepath, out var newFileViewModel))
                    {
                        return newFileViewModel;
                    }
                }
            }
            catch
            {
                // TODO Add logs
            }
            return null;
        }

        private static bool TryPeekAtCVS(string filepath, out FileViewModel? fileViewModel)
        {
            fileViewModel = default;
            if (!System.IO.File.Exists(filepath))
                return false;

            var fileInfo = new FileInfo(filepath);
            FileInfo streamFile = fileInfo;
            bool isUsingCopy = false;

            try
            {
                if (fileInfo.IsFileLocked())
                {
                    streamFile = fileInfo.CopyFile();
                    isUsingCopy = true;
                }

                using var reader = new StreamReader(streamFile.FullName);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                if (csv.Read() && csv.ReadHeader())
                {
                    var fields = GetFields(csv).Where(f => string.IsNullOrWhiteSpace(f) == false).ToList();
                    using var dr = new CsvDataReader(csv);
                    var dataTable = new DataTable();
                    dataTable.Load(dr);

                    for (int col = dataTable.Columns.Count - 1; col >= 0; col--)
                    {
                        if (fields.Contains(dataTable.Columns[col].ColumnName) == false)
                        {
                            dataTable.Columns.RemoveAt(col);
                        }
                    }

                    fileViewModel = new FileViewModel(streamFile, fields, dataTable);
                    return true;
                }
            }
            catch
            {
                // TODO Add logs
            }
            finally
            {
                if (isUsingCopy && System.IO.File.Exists(streamFile.FullName))
                {
                    System.IO.File.Delete(streamFile.FullName);
                }
            }

            return false;

            static IEnumerable<string> GetFields(CsvReader csv)
            {
                for (int i = 0; i < csv.ColumnCount; i++)
                {
                    if (csv.GetField(i) is string field)
                    {
                        yield return field;
                    }
                }
            }
        }

        private void Compare()
        {
            if (JoinOnColumnList.Any(c => c.Selected) == false)
                return;

            if (DiffOnColumnList.Any(c => c.Selected) == false)
                return;

            if (LatestFile == null)
                return;

            if (PreviousFile == null)
                return;

            var joinOnList = JoinOnColumnList.Where(c => c.Selected).Select(c => c.Name).ToArray();
            var diffOnList = DiffOnColumnList.Where(c => c.Selected).Select(c => c.Name).ToArray();

            var reducedLatest   = ReduceTable(LatestFile.Data, joinOnList, diffOnList);
            var reducedPrevious = ReduceTable(PreviousFile.Data, joinOnList, diffOnList);

            var diffResult = CompareTables(reducedLatest, reducedPrevious, joinOnList, diffOnList);

            if (OptionalJoinFile != null)
            {
                diffResult = JoinTable(diffResult, OptionalJoinFile.Data, joinOnList);
            }

            DiffResult = diffResult;
        }

        public static DataTable ReduceTable(DataTable table, IEnumerable<string> joinOn, IEnumerable<string> aggregateOn) 
        {
            var joinOnIndexes = GetIndexOfColumns(table, joinOn).ToArray();
            if (joinOnIndexes.Any(i => i < 0))
                return table;

            var aggregateIndexes = GetIndexOfColumns(table, aggregateOn).ToArray();
            if (aggregateIndexes.Any(i => i < 0))
                return table;

            var allRows = new List<DataRow>();
            foreach (DataRow row in table.Rows)
            {
                allRows.Add(row);
            }

            var reducedTable = new DataTable();

            for (int i = 0; i < table.Columns.Count; i++)
            {
                reducedTable.Columns.Add(table.Columns[i].ColumnName);
            }

            foreach (var joinGroup in allRows.GroupBy(r => GetJoinOnValue(r, joinOnIndexes)))
            {
                if (string.IsNullOrWhiteSpace(joinGroup.Key?.ToString())) 
                    continue;

                string joinGroupValue = $"{joinGroup.Key}";

                // reducing to a single row
                var newRow = reducedTable.NewRow();

                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (joinOnIndexes.Contains(col))
                    {
                        newRow[col] = joinGroup.First()[col];
                        continue;
                    }
                    else if (aggregateIndexes.Contains(col))
                    {
                        double aggregatedValue = 0;
                        foreach (var row in joinGroup)
                        {
                            if (double.TryParse(row[col]?.ToString(), out double valueParsed))
                            {
                                aggregatedValue += valueParsed;
                            }
                        }
                        if (double.IsInteger(aggregatedValue))
                        {
                            newRow[col] = (int)aggregatedValue;
                        }
                        else
                        {
                            newRow[col] = aggregatedValue;
                        }
                    }
                    else
                    {
                        var values = joinGroup.Select(r => r[col]).Distinct().ToArray();
                        if (values.Length  > 1)
                        {
                            newRow[col] = INVALID;
                        }
                        else
                        {
                            newRow[col] = joinGroup.First()[col];
                        }
                    }
                }

                reducedTable.Rows.Add(newRow);
            }

            return reducedTable;
        }

        public static DataTable CompareTables(DataTable latestTable, DataTable previousTable, IEnumerable<string> joinOn, IEnumerable<string> diffOn)
        {
            var joinOnDiffTableColumnIndex = GetIndexOfColumns(latestTable, joinOn).ToArray();
            if (joinOnDiffTableColumnIndex.Any(i => i < 0))
                return latestTable;

            var joinOnPreviousColumnIndex = GetIndexOfColumns(previousTable, joinOn).ToArray();
            if (joinOnPreviousColumnIndex.Any(i => i < 0))
                return latestTable;

            var diffTable = latestTable.Copy();

            List<int> removeRows = [];
            for (int row = 0; row < diffTable.Rows.Count; row++)
            {
                var currentRow = diffTable.Rows[row];
                var joinOnValue = GetJoinOnValue(currentRow, joinOnDiffTableColumnIndex);

                DataRow? previousRow = GetMatchingRow(previousTable, joinOnValue, joinOnPreviousColumnIndex);

                bool zeroedRow = true;

                foreach (var diffOnValue in diffOn)
                {
                    var diffOnLatestIndex = diffTable.Columns.IndexOf(diffOnValue);
                    if (diffOnLatestIndex < 0)
                        continue;

                    if (previousRow == null)
                    {
                        if (GetNumericCellValue(currentRow[diffOnLatestIndex]) > 0)
                        {
                            zeroedRow = false;
                        }
                        continue;
                    }

                    var diffOnPreviousIndex = previousTable.Columns.IndexOf(diffOnValue);
                    if (diffOnPreviousIndex < 0)
                        continue;

                    double diffedValue = DiffOnValues(currentRow[diffOnLatestIndex], previousRow[diffOnPreviousIndex]);
                    currentRow[diffOnLatestIndex] = double.IsInteger(diffedValue) ? (int)diffedValue : diffedValue;
                    zeroedRow &= diffedValue <= 0;
                }
                if (zeroedRow)
                {
                    removeRows.Add(row);
                }
            }

            var diffOnDiffTableColumnIndex = GetIndexOfColumns(latestTable, diffOn).ToHashSet();

            for (int col = diffTable.Columns.Count - 1; col >= 0; col--)
            {
                if (joinOnDiffTableColumnIndex.Contains(col) || diffOnDiffTableColumnIndex.Contains(col))
                    continue;

                diffTable.Columns.RemoveAt(col);
            }

            foreach (var row in removeRows.OrderByDescending(x => x))
            {
                diffTable.Rows.RemoveAt(row);
            }

            return diffTable;
        }

        private static DataTable JoinTable(DataTable diffResult, DataTable joinTable, string[] joinOnList)
        {
            // Partial match is allowed for optional join
            var joinOnTableColumnIndex = GetIndexOfColumns(joinTable, joinOnList).Where(i => i >= 0).ToList();
            if (joinOnTableColumnIndex.Count == 0)
                return diffResult;

            // Retrieving the official join list based on the columns found on the join table
            var partialMatchJoinList = joinOnTableColumnIndex.Select(i => joinTable.Columns[i].ColumnName).ToArray();

            var joinOnDiffTableColumnIndex = GetIndexOfColumns(diffResult, partialMatchJoinList).ToArray();
            if (joinOnDiffTableColumnIndex.Any(i => i < 0))
                return diffResult;

            var diffResultWithJoin = diffResult.Copy();

            for (int col = 0; col < joinTable.Columns.Count; col++)
            {
                if (joinOnTableColumnIndex.Contains(col))
                    continue;

                var indexOfNewCol = diffResultWithJoin.Columns.Count;
                diffResultWithJoin.Columns.Add(joinTable.Columns[col].ColumnName);

                for (int row = 0; row < diffResultWithJoin.Rows.Count; row++)
                {
                    var currentRow = diffResultWithJoin.Rows[row];
                    var joinOnValue = GetJoinOnValue(currentRow, joinOnDiffTableColumnIndex);

                    DataRow? joinRow = GetMatchingRow(joinTable, joinOnValue, joinOnTableColumnIndex);
                    if (joinRow == null)
                        continue;

                    currentRow[indexOfNewCol] = joinRow[col];
                }
            }

            return diffResultWithJoin;
        }

        private static IEnumerable<int> GetIndexOfColumns(DataTable table, IEnumerable<string> columnNames)
        {
            foreach (var columnName in columnNames)
            {
                yield return table.Columns.IndexOf(columnName);
            }
        }

        private static string GetJoinOnValue(DataRow row, IEnumerable<int> joinOnIndexes, string separator = "")
        {
            return string.Join(separator, joinOnIndexes.Select(i => row[i].ToString()));
        }

        private static DataRow? GetMatchingRow(DataTable table, string joinValue, IEnumerable<int> joinOnIndexes)
        {
            for (int row = 0; row < table.Rows.Count; row++)
            {
                if (GetJoinOnValue(table.Rows[row], joinOnIndexes) == joinValue)
                {
                    return table.Rows[row];
                }
            }
            return null;
        }

        private static double GetNumericCellValue(object cell)
        {
            if (!double.TryParse(cell.ToString(), out double cellValue))
            {
                return 0.0;
            }
            return cellValue;
        }

        private static double DiffOnValues(object current, object latest)
        {
            return GetNumericCellValue(current) - GetNumericCellValue(latest);
        }

        public static void OpenWithDefaultProgram(string path)
        {
            using var process = new Process();

            process.StartInfo.FileName = "explorer";
            process.StartInfo.Arguments = "\"" + path + "\"";
            process.Start();
        }

        internal void MergeColumns(IList<MergeableColumnViewModel> selection)
        {
            if (MergeableColumnList.Count == 0) 
                return;
            var usedGroups = MergeableColumnList.Where(c => selection.Contains(c) == false).Select(c => c.MergeGroup).ToHashSet();
            int mergeGroup = 0;
            while (usedGroups.Contains(mergeGroup))
            {
                mergeGroup++;
            }
            SolidColorBrush brush = new(Helpers.GetNewRandomColor(mergeGroup));
            foreach (var item in selection)
            {
                item.MergeGroup = mergeGroup;
                item.MergeGroupColor = brush;
            }
        }

        internal void UnmergeColumns(IList<MergeableColumnViewModel> selection)
        {
            if (MergeableColumnList.Count == 0)
                return;
            var usedGroups = MergeableColumnList.Where(c => selection.Contains(c) == false).Select(c => c.MergeGroup).ToHashSet();
            foreach (var item in selection)
            {
                int mergeGroup = 0;
                while (usedGroups.Contains(mergeGroup))
                {
                    mergeGroup++;
                }

                usedGroups.Add(mergeGroup);
                SolidColorBrush brush = new(Helpers.GetNewRandomColor(mergeGroup));
                item.MergeGroup = mergeGroup;
                item.MergeGroupColor = brush;
            }
        }

        private void ExportDiff()
        {
            if (DiffResult == null || MergeableColumnList.Count == 0)
                return;

            if (MergeableColumnList.All(c => c.Selected == false))
                return;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "csv files (*.csv)|*.csv",
                Title = "Save Diff Result",
                FileName = $"CSVDiff_{DateTime.Now:yyyyddMM_hhmm}.csv",
            };

            if (saveFileDialog.ShowDialog() == false)
                return;

            using var writer = new StreamWriter(saveFileDialog.FileName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            foreach (var mergedGroup in MergeableColumnList.GroupBy(c => c.MergeGroup))
            {
                var mergedValue = string.Join(" ", mergedGroup.Where(c => c.Selected));
                if (!string.IsNullOrWhiteSpace(mergedValue))
                {
                    csv.WriteField(mergedValue);
                }
            }
            csv.NextRecord();

            // Write the rows
            foreach (DataRow row in DiffResult.Rows)
            {
                foreach (var mergedGroup in MergeableColumnList.GroupBy(c => c.MergeGroup))
                {
                    var indexOfColumns = GetIndexOfColumns(DiffResult, mergedGroup.Where(c => c.Selected).Select(c => c.Name)).ToArray();
                    if (indexOfColumns.Length == 0)
                        continue;

                    var mergeRowValue = GetJoinOnValue(row, indexOfColumns, " ");
                    csv.WriteField(mergeRowValue);
                }
                csv.NextRecord();
            }

            OpenWithDefaultProgram(saveFileDialog.FileName);
        }
    }
#pragma warning restore CS8618
}
