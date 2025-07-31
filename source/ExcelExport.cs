using OfficeOpenXml;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace CSVDiff
{
    internal class ExcelExport
    {
        public static void ExportFile(string filePath, DataTable dataTable)
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
            sheet.Cells["A1"].LoadFromDataTable(dataTable, true, TableStyles.Dark11);
            package.Save();
        }
    }
}
