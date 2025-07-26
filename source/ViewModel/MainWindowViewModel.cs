using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;

namespace CSVDiff.ViewModel
{
#pragma warning disable CS8618
    internal class MainWindowViewModel : ObservableObject
    {
        private const string INVALID = "<multi-values>";
        public RelayCommand<string> LoadFileCommand { get; }
        public RelayCommand<string> ClearFileCommand { get; }
        public ICommand SwapFilesCommand { get; }
        public ICommand CompareCommand { get; }
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

        public ObservableCollection<ColumnViewModel> JoinOnColumnList { get; } = [];
        public ObservableCollection<ColumnViewModel> DiffOnColumnList { get; } = [];

        private bool SuspendRefreshMatchList;

        private DataTable? _diffResult;
        public DataTable? DiffResult { get => _diffResult; private set => SetProperty(ref _diffResult, value); }

        public MainWindowViewModel()
        {
            CompareCommand = new RelayCommand(Compare);

            ClearFileCommand = new RelayCommand<string>(ClearFile);
            LoadFileCommand = new RelayCommand<string>(LoadFile);

            SwapFilesCommand = new RelayCommand(SwapFiles);
            ExportDiffCommand = new RelayCommand(ExportDiff);
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

            if (PreviousFile == null || LatestFile == null)
            {
                JoinOnColumnList.Clear();
                DiffOnColumnList.Clear();
                return;
            }

            HashSet<string> matches = [];
            foreach (var match in LatestFile.Headers.Where(h => PreviousFile.Headers.Contains(h)))
            {
                if (JoinOnColumnList.Any(c => c.Name == match) == false)
                {
                    JoinOnColumnList.Add(new ColumnViewModel(match));
                }
                if (DiffOnColumnList.Any(c => c.Name == match) == false)
                {
                    DiffOnColumnList.Add(new ColumnViewModel(match));
                }
                matches.Add(match);
            }

            foreach (var colToClean in JoinOnColumnList.Where(c => matches.Contains(c.Name) == false).ToArray())
            {
                JoinOnColumnList.Remove(colToClean);
            }

            foreach (var colToClean in DiffOnColumnList.Where(c => matches.Contains(c.Name) == false).ToArray())
            {
                DiffOnColumnList.Remove(colToClean);
            }

            DiffResult = null;
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
            if (TryBrowseForFile(out string selectedFilepath))
            {
                if (TryPeekAtCVS(selectedFilepath, out var newFileViewModel))
                {
                    return newFileViewModel;
                }
            }

            return null;
        }

        private static bool TryPeekAtCVS(string filepath, out FileViewModel? fileViewModel)
        {
            fileViewModel = default;
            if (!System.IO.File.Exists(filepath))
                return false;

            using var reader = new StreamReader(filepath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            if (csv.Read() && csv.ReadHeader())
            {
                var fields = GetFields(csv).ToArray();
                using var dr = new CsvDataReader(csv);
                var dataTable = new DataTable();
                dataTable.Load(dr);
                var fileInfo = new FileInfo(filepath);
                fileViewModel = new FileViewModel(fileInfo, fields, dataTable);
                return true;
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
            var joinOnIndexes = GetIndexOfColumn(table, joinOn).ToArray();
            if (joinOnIndexes.Any(i => i < 0))
                return table;

            var aggregateIndexes = GetIndexOfColumn(table, aggregateOn).ToArray();
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
            var diffTable = latestTable.Copy();

            var joinOnDiffTableColumnIndex = GetIndexOfColumn(latestTable, joinOn).ToArray();
            if (joinOnDiffTableColumnIndex.Any(i => i < 0))
                return diffTable;

            var joinOnPreviousColumnIndex = GetIndexOfColumn(previousTable, joinOn).ToArray();
            if (joinOnPreviousColumnIndex.Any(i => i < 0))
                return diffTable;

            List<int> removeRows = [];
            for (int row = 0; row < diffTable.Rows.Count; row++)
            {
                var currentRow = diffTable.Rows[row];
                var joinOnValue = GetJoinOnValue(currentRow, joinOnDiffTableColumnIndex);

                DataRow? previousRow = getMatchingRow(joinOnValue);

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

            var diffOnDiffTableColumnIndex = GetIndexOfColumn(latestTable, diffOn).ToHashSet();

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

            DataRow? getMatchingRow(string joinValue)
            {
                for (int row = 0; row < previousTable.Rows.Count; row++)
                {
                    if (GetJoinOnValue(previousTable.Rows[row], joinOnPreviousColumnIndex) == joinValue)
                    {
                        return previousTable.Rows[row];
                    }
                }
                return null;
            }
        }

        private static DataTable JoinTable(DataTable diffResult, DataTable data, string[] joinOnList)
        {
            return diffResult;
        }

        private static IEnumerable<int> GetIndexOfColumn(DataTable table, IEnumerable<string> columnNames)
        {
            foreach (var columnName in columnNames)
            {
                yield return table.Columns.IndexOf(columnName);
            }
        }

        private static string GetJoinOnValue(DataRow row, IEnumerable<int> joinOnIndexes)
        {
            return string.Join("", joinOnIndexes.Select(i => row[i].ToString()));
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

        private void ExportDiff()
        {
            if (DiffResult == null)
                return;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "csv files (*.csv)|*.csv",
                Title = "Save Diff Result",
                FileName = $"Diff_{DateTime.Now:yyyyddMM_hhmm}.csv",
            };

            if (saveFileDialog.ShowDialog() == false)
                return;

            using var writer = new StreamWriter(saveFileDialog.FileName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Write the header
            foreach (DataColumn column in DiffResult.Columns)
            {
                csv.WriteField(column.ColumnName);
            }
            csv.NextRecord();

            // Write the rows
            foreach (DataRow row in DiffResult.Rows)
            {
                foreach (DataColumn column in DiffResult.Columns)
                {
                    csv.WriteField(row[column]);
                }
                csv.NextRecord();
            }

            OpenWithDefaultProgram(saveFileDialog.FileName);
        }

        public static void OpenWithDefaultProgram(string path)
        {
            using var process = new Process();

            process.StartInfo.FileName = "explorer";
            process.StartInfo.Arguments = "\"" + path + "\"";
            process.Start();
        }
    }
#pragma warning restore CS8618
}
