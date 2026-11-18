using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace WPF_프로젝트;

public enum MaintenanceStatus
{
    Normal = 0,
    Imminent = 1,
    Overdue = 2
}

public class Equipment : ViewModelBase
{
    /// <summary>사용률이 이 값 이상이면 '정비 임박'</summary>
    public const double ImminentRate = 0.9;

    /// <summary>정비 예정일까지 남은 일수가 이 값 이하이면 '정비 임박'</summary>
    public const int ImminentDays = 7;

    private string _id = "";
    private string _name = "";
    private string _category = "";
    private string _systemName = "";
    private double _runtimeHours;
    private double _thresholdHours;
    private DateTime _nextMaintenanceDate = DateTime.Today;
    private DateTime? _lastMaintenanceDate;
    private int _maintenanceIntervalDays = 90;
    private string? _imagePath;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }

    /// <summary>소속 무기체계 (예: 천궁-II, L-SAM). 한 체계는 발사대·레이더·교전통제소 등 여러 장비로 구성됩니다.</summary>
    public string SystemName { get => _systemName; set => SetProperty(ref _systemName, value); }

    public double RuntimeHours
    {
        get => _runtimeHours;
        set { if (SetProperty(ref _runtimeHours, value)) RaiseComputed(); }
    }

    public double ThresholdHours
    {
        get => _thresholdHours;
        set { if (SetProperty(ref _thresholdHours, value)) RaiseComputed(); }
    }

    public DateTime NextMaintenanceDate
    {
        get => _nextMaintenanceDate;
        set { if (SetProperty(ref _nextMaintenanceDate, value)) RaiseComputed(); }
    }

    public DateTime? LastMaintenanceDate
    {
        get => _lastMaintenanceDate;
        set => SetProperty(ref _lastMaintenanceDate, value);
    }

    public int MaintenanceIntervalDays
    {
        get => _maintenanceIntervalDays;
        set => SetProperty(ref _maintenanceIntervalDays, value);
    }

    /// <summary>장비 사진의 절대 경로 (없으면 null)</summary>
    public string? ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }

    // ---------- 계산 속성 (저장하지 않음) ----------

    [JsonIgnore]
    public double UsageRate => ThresholdHours <= 0 ? 0 : RuntimeHours / ThresholdHours;

    [JsonIgnore]
    public int DaysUntilMaintenance => (NextMaintenanceDate.Date - DateTime.Today).Days;

    [JsonIgnore]
    public string DDayText => DaysUntilMaintenance switch
    {
        < 0 => $"D+{-DaysUntilMaintenance}",
        0 => "D-Day",
        _ => $"D-{DaysUntilMaintenance}"
    };

    [JsonIgnore]
    public MaintenanceStatus Status
    {
        get
        {
            if (UsageRate >= 1.0 || DaysUntilMaintenance < 0) return MaintenanceStatus.Overdue;
            if (UsageRate >= ImminentRate || DaysUntilMaintenance <= ImminentDays) return MaintenanceStatus.Imminent;
            return MaintenanceStatus.Normal;
        }
    }

    [JsonIgnore]
    public string StatusText => Status switch
    {
        MaintenanceStatus.Overdue => "정비 초과",
        MaintenanceStatus.Imminent => "정비 임박",
        _ => "정상"
    };

    private void RaiseComputed()
    {
        OnPropertyChanged(nameof(UsageRate));
        OnPropertyChanged(nameof(DaysUntilMaintenance));
        OnPropertyChanged(nameof(DDayText));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusText));
    }
}
