using CSVDiff.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Data;
using System.IO;

namespace CSVDiff
{
    internal class ExcelExport
    {
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
            var sheet = package.Workbook.Worksheets.Add("Default");
            sheet.Cells["A1"].LoadFromDataTable(dataTable, true, TableStyles.None);

            if (exportSettings != null)
            {
                if (exportSettings.ColumnSettings.Count == sheet.Dimension.Columns)
                {
                    for (int col = 0; col < exportSettings.ColumnSettings.Count; col++)
                    {
                        var colSetting = exportSettings.ColumnSettings[col];

                        var range = sheet.Cells[1, col + 1, sheet.Dimension.Rows, col + 1];
                        range.Style.WrapText = colSetting.WrapText;
                        range.Style.Font.Bold = colSetting.Bold;
                        range.Style.Font.Size = colSetting.FontSize;

                        range.Style.HorizontalAlignment = GetHorizontalAlignment(colSetting.HorizontalAlignment);
                        range.Style.VerticalAlignment = GetVerticalAlignment(colSetting.VerticalAlignment);

                        if (colSetting.ColWidth > 0)
                        {
                            sheet.Columns[col + 1].Width = colSetting.ColWidth;
                        }
                        else
                        {
                            range.AutoFitColumns();
                        }
                    }
                }

                if (exportSettings.RowHeight > 0)
                {
                    foreach (var row in sheet.Rows)
                    {
                        row.Height = exportSettings.RowHeight;
                    }
                }
            }

            var fullRange = sheet.Dimension;

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
