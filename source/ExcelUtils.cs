using CSVDiff.Models;
using CSVDiff.ViewModel;
using OfficeOpenXml;
using OfficeOpenXml.Export.ToDataTable;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Data;
using System.IO;

namespace CSVDiff
{
    internal class ExcelUtils
    {
        public static bool ReadExcel(FileInfo streamFile, out FileViewModel? fileViewModel)
        {
            ExcelPackage.License.SetNonCommercialPersonal("CSVDiff");
            using var package = new ExcelPackage(streamFile.FullName);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                fileViewModel = default;
                return false;
            }

            int colStart = worksheet.Dimension.Start.Column;
            int rowStart = worksheet.Dimension.Start.Row;

            int colEnd = worksheet.Dimension.End.Column;
            int rowEnd = worksheet.Dimension.End.Row;

            var fields = new List<string>();
            for (int col = colStart; col <= colEnd; col++)
            {
                var value = worksheet.Cells[rowStart, col].Value?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    worksheet.Cells[rowStart, col].Value = value = $"Column - {col}";
                }
                fields.Add(value);
            }

            var options = ToDataTableOptions.Create(o => { o.AlwaysAllowNull = true; o.AllowDuplicateColumnNames = true; });
            var dataTable = worksheet.Cells[rowStart, colStart, rowEnd, colEnd].ToDataTable(options);

            fileViewModel = new FileViewModel(streamFile, fields, dataTable);
            return true;
        }

        public static void ExportFile(string filePath, DataTable dataTable, ExcelExportSettings exportSettings)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                if (fileInfo.IsFileLocked())
                    return;

                fileInfo.Delete();
            }

            ExcelPackage.License.SetNonCommercialPersonal("CSVDiff");
            using var package = new ExcelPackage(filePath);
            var worksheet = package.Workbook.Worksheets.Add("Default");
            worksheet.Cells["A1"].LoadFromDataTable(dataTable, true, TableStyles.None);

            if (exportSettings != null)
            {
                if (exportSettings.ColumnSettings.Count == worksheet.Dimension.Columns)
                {
                    for (int col = 0; col < exportSettings.ColumnSettings.Count; col++)
                    {
                        var colSetting = exportSettings.ColumnSettings[col];

                        var range = worksheet.Cells[1, col + 1, worksheet.Dimension.Rows, col + 1];
                        range.Style.WrapText = colSetting.WrapText;
                        range.Style.Font.Bold = colSetting.Bold;
                        range.Style.Font.Size = colSetting.FontSize;

                        range.Style.HorizontalAlignment = GetHorizontalAlignment(colSetting.HorizontalAlignment);
                        range.Style.VerticalAlignment = GetVerticalAlignment(colSetting.VerticalAlignment);

                        if (colSetting.ColWidth > 0)
                        {
                            worksheet.Columns[col + 1].Width = colSetting.ColWidth;
                        }
                        else
                        {
                            range.AutoFitColumns();
                        }
                    }
                }

                if (exportSettings.RowHeight > 0)
                {
                    foreach (var row in worksheet.Rows)
                    {
                        row.Height = exportSettings.RowHeight;
                    }
                }
            }

            var fullRange = worksheet.Dimension;

            // Assign borders
            fullRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            package.Save();
        }

        private static OfficeOpenXml.Style.ExcelHorizontalAlignment GetHorizontalAlignment(Models.ExcelHorizontalAlignment excelHorizontalAlignment)
        {
            return excelHorizontalAlignment switch
            {
                Models.ExcelHorizontalAlignment.General => OfficeOpenXml.Style.ExcelHorizontalAlignment.General,
                Models.ExcelHorizontalAlignment.Left => OfficeOpenXml.Style.ExcelHorizontalAlignment.Left,
                Models.ExcelHorizontalAlignment.Center => OfficeOpenXml.Style.ExcelHorizontalAlignment.Center,
                Models.ExcelHorizontalAlignment.Right => OfficeOpenXml.Style.ExcelHorizontalAlignment.Right,
                Models.ExcelHorizontalAlignment.Fill => OfficeOpenXml.Style.ExcelHorizontalAlignment.Fill,
                Models.ExcelHorizontalAlignment.Distributed => OfficeOpenXml.Style.ExcelHorizontalAlignment.Distributed,
                Models.ExcelHorizontalAlignment.Justify => OfficeOpenXml.Style.ExcelHorizontalAlignment.Justify,
                _ => OfficeOpenXml.Style.ExcelHorizontalAlignment.General,
            };
        }

        private static OfficeOpenXml.Style.ExcelVerticalAlignment GetVerticalAlignment(Models.ExcelVerticalAlignment excelVerticalAlignment)
        {
            return excelVerticalAlignment switch
            {
                Models.ExcelVerticalAlignment.Top => OfficeOpenXml.Style.ExcelVerticalAlignment.Top,
                Models.ExcelVerticalAlignment.Center => OfficeOpenXml.Style.ExcelVerticalAlignment.Center,
                Models.ExcelVerticalAlignment.Bottom => OfficeOpenXml.Style.ExcelVerticalAlignment.Bottom,
                Models.ExcelVerticalAlignment.Distributed => OfficeOpenXml.Style.ExcelVerticalAlignment.Distributed,
                Models.ExcelVerticalAlignment.Justify => OfficeOpenXml.Style.ExcelVerticalAlignment.Justify,
                _ => OfficeOpenXml.Style.ExcelVerticalAlignment.Bottom,
            };
        }
    }
}
