using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;

namespace WPF_프로젝트;

public record SortOption(string Display, string Property);
public record DueFilter(string Display, int? WithinDays);

public class MainViewModel : ViewModelBase
{
    // ======================= 컬렉션 =======================

    public ObservableCollection<Equipment> Equipments { get; }
    public ObservableCollection<Part> Parts { get; }
    public ObservableCollection<ReplacementRecord> Records { get; }

    /// <summary>검색/필터/정렬이 적용된 장비 목록 (DataGrid 바인딩 대상)</summary>
    public ICollectionView EquipmentView { get; }
    public ICollectionView PartView { get; }
    public ICollectionView RecordView { get; }

    /// <summary>선택 장비에 쓸 수 있는 부품 (같은 분류 + 공통)</summary>
    public ObservableCollection<Part> CompatibleParts { get; } = new();

    /// <summary>선택 장비의 정비/교체 이력</summary>
    public ObservableCollection<ReplacementRecord> SelectedHistory { get; } = new();

    public DateTime Today => DateTime.Today;

    // ======================= 검색 / 필터 / 정렬 =======================

    /// <summary>"전체 체계" + 데이터에 있는 체계 이름들</summary>
    public IReadOnlyList<string> SystemFilters { get; }

    public IReadOnlyList<string> StatusFilters { get; } = new[] { "전체", "정상", "정비 임박", "정비 초과" };

    public IReadOnlyList<DueFilter> DueFilters { get; } = new[]
    {
        new DueFilter("예정일 전체", null),
        new DueFilter("7일 이내", 7),
        new DueFilter("30일 이내", 30),
        new DueFilter("90일 이내", 90),
    };

    public IReadOnlyList<SortOption> SortOptions { get; } = new[]
    {
        new SortOption("정비 예정일", nameof(Equipment.NextMaintenanceDate)),
        new SortOption("사용률",     nameof(Equipment.UsageRate)),
        new SortOption("가동 시간",  nameof(Equipment.RuntimeHours)),
        new SortOption("임계값",     nameof(Equipment.ThresholdHours)),
        new SortOption("상태",       nameof(Equipment.Status)),
        new SortOption("체계",       nameof(Equipment.SystemName)),
        new SortOption("장비명",     nameof(Equipment.Name)),
        new SortOption("분류",       nameof(Equipment.Category)),
        new SortOption("ID",         nameof(Equipment.Id)),
    };

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) EquipmentView.Refresh(); }
    }

    private string _selectedSystemFilter = "전체 체계";
    public string SelectedSystemFilter
    {
        get => _selectedSystemFilter;
        set { if (SetProperty(ref _selectedSystemFilter, value)) EquipmentView.Refresh(); }
    }

    private string _selectedStatusFilter = "전체";
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set { if (SetProperty(ref _selectedStatusFilter, value)) EquipmentView.Refresh(); }
    }

    private DueFilter _selectedDueFilter;
    public DueFilter SelectedDueFilter
    {
        get => _selectedDueFilter;
        set { if (SetProperty(ref _selectedDueFilter, value)) EquipmentView.Refresh(); }
    }

    private SortOption _selectedSortOption;
    public SortOption SelectedSortOption
    {
        get => _selectedSortOption;
        set { if (SetProperty(ref _selectedSortOption, value)) ApplySort(); }
    }

    private bool _sortDescending;
    public bool SortDescending
    {
        get => _sortDescending;
        set { if (SetProperty(ref _sortDescending, value)) ApplySort(); }
    }

    private string _partSearchText = "";
    public string PartSearchText
    {
        get => _partSearchText;
        set { if (SetProperty(ref _partSearchText, value)) PartView.Refresh(); }
    }

    private bool _showLowStockOnly;
    public bool ShowLowStockOnly
    {
        get => _showLowStockOnly;
        set { if (SetProperty(ref _showLowStockOnly, value)) PartView.Refresh(); }
    }

    private string _recordSearchText = "";
    public string RecordSearchText
    {
        get => _recordSearchText;
        set { if (SetProperty(ref _recordSearchText, value)) RecordView.Refresh(); }
    }

    // ======================= 선택 / 입력 =======================

    private Equipment? _selectedEquipment;
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (SetProperty(ref _selectedEquipment, value))
            {
                RefreshCompatibleParts();
                RefreshHistory();
            }
        }
    }

    private Part? _selectedPart;
    public Part? SelectedPart
    {
        get => _selectedPart;
        set { if (SetProperty(ref _selectedPart, value)) OnPropertyChanged(nameof(ReplaceWarning)); }
    }

    private int _replaceQuantity = 1;
    public int ReplaceQuantity
    {
        get => _replaceQuantity;
        set { if (SetProperty(ref _replaceQuantity, value)) OnPropertyChanged(nameof(ReplaceWarning)); }
    }

    private string _replaceNote = "";
    public string ReplaceNote { get => _replaceNote; set => SetProperty(ref _replaceNote, value); }

    private int _restockQuantity = 10;
    public int RestockQuantity { get => _restockQuantity; set => SetProperty(ref _restockQuantity, value); }

    public string? ReplaceWarning =>
        SelectedPart is { } p && ReplaceQuantity > p.Stock ? $"재고 부족 — 현재 {p.Stock}{p.Unit}" : null;

    // ======================= 요약 / 경고 =======================

    private int _overdueCount, _imminentCount, _lowStockCount;
    public int OverdueCount { get => _overdueCount; private set => SetProperty(ref _overdueCount, value); }
    public int ImminentCount { get => _imminentCount; private set => SetProperty(ref _imminentCount, value); }
    public int LowStockCount { get => _lowStockCount; private set => SetProperty(ref _lowStockCount, value); }

    public bool HasAlert => OverdueCount + ImminentCount + LowStockCount > 0;

    public string AlertText
    {
        get
        {
            if (!HasAlert) return "모든 장비 정상 · 부품 재고 양호";
            var parts = new List<string>();
            if (OverdueCount > 0) parts.Add($"정비 초과 {OverdueCount}대");
            if (ImminentCount > 0) parts.Add($"정비 임박 {ImminentCount}대");
            if (LowStockCount > 0) parts.Add($"재고 부족 부품 {LowStockCount}종");
            return string.Join("  /  ", parts);
        }
    }

    private string _statusMessage = "준비됨";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    // ======================= 커맨드 =======================

    public ICommand AddRuntimeCommand { get; }
    public ICommand CompleteMaintenanceCommand { get; }
    public ICommand ReplacePartCommand { get; }
    public ICommand RestockCommand { get; }
    public ICommand ChangePhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }
    public ICommand ClearSearchCommand { get; }

    // ======================= 생성자 =======================

    public MainViewModel()
    {
        var data = DataService.Load();
        Equipments = new ObservableCollection<Equipment>(data.Equipments);
        Parts = new ObservableCollection<Part>(data.Parts);
        Records = new ObservableCollection<ReplacementRecord>(data.Records);

        SystemFilters = new[] { "전체 체계" }
            .Concat(Equipments.Select(e => e.SystemName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n))
            .ToList();

        foreach (var e in Equipments) e.PropertyChanged += (_, _) => UpdateSummary();
        foreach (var p in Parts) p.PropertyChanged += OnPartChanged;

        // 장비 뷰: 필터 + 정렬 + 값이 바뀌면 자동 재정렬(Live Shaping)
        EquipmentView = CollectionViewSource.GetDefaultView(Equipments);
        EquipmentView.Filter = FilterEquipment;
        EnableLiveShaping(EquipmentView,
            nameof(Equipment.Id), nameof(Equipment.Name), nameof(Equipment.Category), nameof(Equipment.SystemName),
            nameof(Equipment.RuntimeHours), nameof(Equipment.ThresholdHours), nameof(Equipment.UsageRate),
            nameof(Equipment.NextMaintenanceDate), nameof(Equipment.DaysUntilMaintenance), nameof(Equipment.Status));

        PartView = CollectionViewSource.GetDefaultView(Parts);
        PartView.Filter = FilterPart;
        PartView.SortDescriptions.Add(new SortDescription(nameof(Part.PartNo), ListSortDirection.Ascending));
        EnableLiveShaping(PartView, nameof(Part.Stock), nameof(Part.IsLowStock));

        RecordView = CollectionViewSource.GetDefaultView(Records);
        RecordView.Filter = FilterRecord;
        RecordView.SortDescriptions.Add(new SortDescription(nameof(ReplacementRecord.Timestamp), ListSortDirection.Descending));

        AddRuntimeCommand = new RelayCommand(AddRuntime, _ => SelectedEquipment != null);
        CompleteMaintenanceCommand = new RelayCommand(CompleteMaintenance, () => SelectedEquipment != null);
        ReplacePartCommand = new RelayCommand(ReplacePart, CanReplacePart);
        RestockCommand = new RelayCommand(Restock, p => (p as Part ?? SelectedPart) != null && RestockQuantity > 0);
        ChangePhotoCommand = new RelayCommand(ChangePhoto, () => SelectedEquipment != null);
        RemovePhotoCommand = new RelayCommand(RemovePhoto, () => !string.IsNullOrEmpty(SelectedEquipment?.ImagePath));
        ClearSearchCommand = new RelayCommand(() => { SearchText = ""; SelectedSystemFilter = "전체 체계"; SelectedStatusFilter = "전체"; SelectedDueFilter = DueFilters[0]; });

        // 기본: 정비 예정일 오름차순 (급한 장비가 위로)
        _selectedDueFilter = DueFilters[0];
        _selectedSortOption = SortOptions[0];
        ApplySort();
        UpdateSummary();
        SelectedEquipment = EquipmentView.Cast<Equipment>().FirstOrDefault();
    }

    // ======================= 필터 / 정렬 구현 =======================

    private bool FilterEquipment(object obj)
    {
        if (obj is not Equipment e) return false;

        if (SelectedSystemFilter != "전체 체계" && e.SystemName != SelectedSystemFilter) return false;

        var statusOk = SelectedStatusFilter switch
        {
            "정상" => e.Status == MaintenanceStatus.Normal,
            "정비 임박" => e.Status == MaintenanceStatus.Imminent,
            "정비 초과" => e.Status == MaintenanceStatus.Overdue,
            _ => true
        };
        if (!statusOk) return false;

        // 정비 예정일 기준 필터 (이미 지난 장비는 항상 포함)
        if (SelectedDueFilter?.WithinDays is int days && e.DaysUntilMaintenance > days) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var k = SearchText.Trim();
        return Has(e.Id, k) || Has(e.Name, k) || Has(e.Category, k) || Has(e.SystemName, k) || Has(e.StatusText, k)
               || e.NextMaintenanceDate.ToString("yyyy-MM-dd").Contains(k);
    }

    private bool FilterPart(object obj)
    {
        if (obj is not Part p) return false;
        if (ShowLowStockOnly && !p.IsLowStock) return false;
        if (string.IsNullOrWhiteSpace(PartSearchText)) return true;
        var k = PartSearchText.Trim();
        return Has(p.PartNo, k) || Has(p.Name, k) || Has(p.Category, k);
    }

    private bool FilterRecord(object obj)
    {
        if (obj is not ReplacementRecord r) return false;
        if (string.IsNullOrWhiteSpace(RecordSearchText)) return true;
        var k = RecordSearchText.Trim();
        return Has(r.Kind, k) || Has(r.EquipmentId, k) || Has(r.EquipmentName, k)
               || Has(r.PartNo, k) || Has(r.PartName, k) || Has(r.Note, k)
               || r.Timestamp.ToString("yyyy-MM-dd").Contains(k);
    }

    private static bool Has(string? source, string keyword)
        => source?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private void ApplySort()
    {
        using (EquipmentView.DeferRefresh())
        {
            EquipmentView.SortDescriptions.Clear();
            var dir = SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            EquipmentView.SortDescriptions.Add(new SortDescription(SelectedSortOption.Property, dir));
            if (SelectedSortOption.Property != nameof(Equipment.Id))
                EquipmentView.SortDescriptions.Add(new SortDescription(nameof(Equipment.Id), ListSortDirection.Ascending));
        }
    }

    private static void EnableLiveShaping(ICollectionView view, params string[] properties)
    {
        if (view is not ICollectionViewLiveShaping live) return;
        if (live.CanChangeLiveSorting)
        {
            live.IsLiveSorting = true;
            foreach (var p in properties) live.LiveSortingProperties.Add(p);
        }
        if (live.CanChangeLiveFiltering)
        {
            live.IsLiveFiltering = true;
            foreach (var p in properties) live.LiveFilteringProperties.Add(p);
        }
    }

    // ======================= 커맨드 구현 =======================

    private void AddRuntime(object? parameter)
    {
        if (SelectedEquipment is not { } e) return;
        var hours = double.TryParse(parameter?.ToString(), out var h) ? h : 10;

        var before = e.Status;
        e.RuntimeHours += hours;
        StatusMessage = $"{e.Name}: 가동 +{hours:N0}h → {e.RuntimeHours:N0}h ({e.UsageRate:P0})";
        if (e.Status != before && e.Status != MaintenanceStatus.Normal)
            StatusMessage += $"  ⚠ {e.StatusText} 단계 진입";
        Save();
    }

    private void CompleteMaintenance()
    {
        if (SelectedEquipment is not { } e) return;

        AddRecord(new ReplacementRecord
        {
            Kind = RecordKinds.Maintenance,
            EquipmentId = e.Id,
            EquipmentName = e.Name,
            Note = $"가동 {e.RuntimeHours:N0}h 시점 정비 완료"
        });

        e.RuntimeHours = 0;
        e.LastMaintenanceDate = DateTime.Today;
        e.NextMaintenanceDate = DateTime.Today.AddDays(e.MaintenanceIntervalDays);

        StatusMessage = $"{e.Name}: 정비 완료 · 다음 정비 {e.NextMaintenanceDate:yyyy-MM-dd}";
        Save();
    }

    private bool CanReplacePart()
        => SelectedEquipment != null && SelectedPart != null
           && ReplaceQuantity > 0 && SelectedPart.Stock >= ReplaceQuantity;

    private void ReplacePart()
    {
        if (SelectedEquipment is not { } e || SelectedPart is not { } p || !CanReplacePart()) return;

        var qty = ReplaceQuantity;
        p.Stock -= qty;   // 재고 실시간 차감 → 모든 화면에 즉시 반영

        AddRecord(new ReplacementRecord
        {
            Kind = RecordKinds.PartReplace,
            EquipmentId = e.Id,
            EquipmentName = e.Name,
            PartNo = p.PartNo,
            PartName = p.Name,
            QuantityChange = -qty,
            Note = ReplaceNote.Trim()
        });

        StatusMessage = $"{e.Name}: {p.Name} {qty}{p.Unit} 교체 · 남은 재고 {p.Stock}{p.Unit}"
                        + (p.IsLowStock ? "  ⚠ 최소 재고 이하" : "");
        ReplaceNote = "";
        ReplaceQuantity = 1;
        Save();
    }

    private void Restock(object? parameter)
    {
        if ((parameter as Part ?? SelectedPart) is not { } p || RestockQuantity <= 0) return;

        p.Stock += RestockQuantity;
        AddRecord(new ReplacementRecord
        {
            Kind = RecordKinds.Restock,
            PartNo = p.PartNo,
            PartName = p.Name,
            QuantityChange = RestockQuantity,
            Note = "입고 처리"
        });

        StatusMessage = $"{p.Name} {RestockQuantity}{p.Unit} 입고 · 현재 재고 {p.Stock}{p.Unit}";
        Save();
    }

    private void ChangePhoto()
    {
        if (SelectedEquipment is not { } e) return;

        var dialog = new OpenFileDialog
        {
            Title = $"{e.Name} 사진 선택",
            Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif|모든 파일|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var old = e.ImagePath;
            e.ImagePath = DataService.ImportImage(dialog.FileName, e.Id);
            DataService.TryDeleteManagedImage(old);
            StatusMessage = $"{e.Name}: 사진 등록 완료";
            Save();
        }
        catch (Exception ex)
        {
            StatusMessage = $"사진 등록 실패: {ex.Message}";
        }
    }

    private void RemovePhoto()
    {
        if (SelectedEquipment is not { } e) return;
        var old = e.ImagePath;
        e.ImagePath = null;
        DataService.TryDeleteManagedImage(old);
        StatusMessage = $"{e.Name}: 사진 삭제";
        Save();
    }

    // ======================= 내부 도우미 =======================

    private void AddRecord(ReplacementRecord record)
    {
        Records.Add(record);
        if (record.EquipmentId == SelectedEquipment?.Id) RefreshHistory();
    }

    private void RefreshHistory()
    {
        SelectedHistory.Clear();
        if (SelectedEquipment is not { } e) return;
        foreach (var r in Records.Where(r => r.EquipmentId == e.Id).OrderByDescending(r => r.Timestamp))
            SelectedHistory.Add(r);
    }

    private void RefreshCompatibleParts()
    {
        CompatibleParts.Clear();
        if (SelectedEquipment is { } e)
        {
            foreach (var p in Parts.Where(p => p.Category == e.Category || p.Category == "공통")
                                   .OrderBy(p => p.Category == "공통").ThenBy(p => p.PartNo))
                CompatibleParts.Add(p);
        }
        SelectedPart = CompatibleParts.FirstOrDefault();
        ReplaceQuantity = 1;
    }

    private void OnPartChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateSummary();
        if (ReferenceEquals(sender, SelectedPart)) OnPropertyChanged(nameof(ReplaceWarning));
    }

    private void UpdateSummary()
    {
        OverdueCount = Equipments.Count(e => e.Status == MaintenanceStatus.Overdue);
        ImminentCount = Equipments.Count(e => e.Status == MaintenanceStatus.Imminent);
        LowStockCount = Parts.Count(p => p.IsLowStock);
        OnPropertyChanged(nameof(HasAlert));
        OnPropertyChanged(nameof(AlertText));
    }

    public void Save()
    {
        try
        {
            DataService.Save(new AppData
            {
                Equipments = Equipments.ToList(),
                Parts = Parts.ToList(),
                Records = Records.ToList()
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"저장 실패: {ex.Message}";
        }
    }
}
