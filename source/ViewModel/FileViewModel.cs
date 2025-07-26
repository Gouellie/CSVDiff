using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CSVDiff.ViewModel
{
    public class FileViewModel(FileInfo fileInfo, IEnumerable<string> headers, DataTable data) : ObservableObject
    {
        public List<string> Headers { get; } = [.. headers];
        public FileInfo FileInfo { get; } = fileInfo;
        public DataTable Data { get; } = data;

        public override string ToString()
        {
            return FileInfo?.Name ?? "Invalid";
        }
    }
}
