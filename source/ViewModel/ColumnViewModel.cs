﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

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

    public class MergeableColumnViewModel(string name, int mergeGroup, Brush groupColor) : ColumnViewModel(name)
    {
        private int _mergeGroup = mergeGroup;
        public int MergeGroup { get => _mergeGroup; set => SetProperty(ref _mergeGroup, value); }

        private Brush _mergeGroupColor = groupColor;
        [Newtonsoft.Json.JsonIgnore]
        public Brush MergeGroupColor { get => _mergeGroupColor; set => SetProperty(ref _mergeGroupColor, value); }
    }

    public class GhostColumn() : MergeableColumnViewModel("N/A", 0, Brushes.AliceBlue)
    {
    }
}
