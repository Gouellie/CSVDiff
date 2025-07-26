using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVDiff.ViewModel
{
    public class ColumnViewModel(string name) : ObservableObject
    {
        public string Name { get; } = name;

        private bool _selected;
        public bool Selected { get => _selected; set => SetProperty(ref _selected, value); }

        public override string ToString()
        {
            return Name;
        }
    }
}
