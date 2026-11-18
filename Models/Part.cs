using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace WPF_프로젝트;

public class Part : ViewModelBase
{
    private string _partNo = "";
    private string _name = "";
    private string _category = "";
    private string _unit = "EA";
    private int _stock;
    private int _minStock;

    public string PartNo { get => _partNo; set => SetProperty(ref _partNo, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    /// <summary>적용 장비 분류 (화력/기동/감시/지원/통신/공통)</summary>
    public string Category { get => _category; set => SetProperty(ref _category, value); }

    public string Unit { get => _unit; set => SetProperty(ref _unit, value); }

    public int Stock
    {
        get => _stock;
        set
        {
            if (SetProperty(ref _stock, value))
            {
                OnPropertyChanged(nameof(IsLowStock));
                OnPropertyChanged(nameof(StockStatusText));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public int MinStock
    {
        get => _minStock;
        set
        {
            if (SetProperty(ref _minStock, value))
            {
                OnPropertyChanged(nameof(IsLowStock));
                OnPropertyChanged(nameof(StockStatusText));
            }
        }
    }

    [JsonIgnore] public bool IsLowStock => Stock <= MinStock;
    [JsonIgnore] public string StockStatusText => Stock == 0 ? "품절" : IsLowStock ? "부족" : "양호";
    [JsonIgnore] public string DisplayName => $"{PartNo}  {Name}  (재고 {Stock}{Unit})";
}
