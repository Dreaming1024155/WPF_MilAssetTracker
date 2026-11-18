using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WPF_프로젝트;

public class AppData
{
    public List<Equipment> Equipments { get; set; } = new();
    public List<Part> Parts { get; set; } = new();
    public List<ReplacementRecord> Records { get; set; } = new();
}

/// <summary>
/// 데이터(JSON)와 장비 사진 저장 담당.
/// 저장 위치: %LOCALAPPDATA%\MilAssetTracker\
/// </summary>
public static class DataService
{
    public static readonly string AppFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MilAssetTracker");

    public static string ImageFolder => Path.Combine(AppFolder, "Images");
    private static string DataFile => Path.Combine(AppFolder, "data_v4.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppData Load()
    {
        if (!File.Exists(DataFile)) return CreateSeedData();

        try
        {
            var json = File.ReadAllText(DataFile);
            return JsonSerializer.Deserialize<AppData>(json, Options) ?? CreateSeedData();
        }
        catch
        {
            // 파일이 깨졌으면 지우지 않고 백업해 두고 샘플 데이터로 시작
            var backup = Path.Combine(AppFolder, $"data.broken_{DateTime.Now:yyyyMMddHHmmss}.json");
            try { File.Move(DataFile, backup); } catch { /* 무시 */ }
            return CreateSeedData();
        }
    }

    public static void Save(AppData data)
    {
        Directory.CreateDirectory(AppFolder);
        var temp = DataFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, DataFile, overwrite: true);   // 저장 중 꺼져도 기존 파일이 깨지지 않도록
    }

    /// <summary>선택한 사진을 앱 폴더로 복사하고 복사본 경로를 돌려줍니다.</summary>
    public static string ImportImage(string sourcePath, string equipmentId)
    {
        Directory.CreateDirectory(ImageFolder);
        var dest = Path.Combine(ImageFolder,
            $"{equipmentId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourcePath).ToLowerInvariant()}");
        File.Copy(sourcePath, dest, overwrite: true);
        return dest;
    }

    /// <summary>앱이 관리하는 사진(Images 폴더 안)만 삭제합니다.</summary>
    public static void TryDeleteManagedImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var full = Path.GetFullPath(path);
            if (full.StartsWith(Path.GetFullPath(ImageFolder), StringComparison.OrdinalIgnoreCase) && File.Exists(full))
                File.Delete(full);
        }
        catch { /* 사용 중이면 다음에 정리 */ }
    }

    private static AppData CreateSeedData() => new()
    {
        Equipments =
        {
            // ※ LIG D&A 체계를 참고한 장비명입니다. 가동 시간·임계값·정비 주기·부품은 모두 임의의 예시 값입니다.
            new Equipment { Id = "EQ-001", Name = "천궁-II 발사대 1호기", SystemName = "천궁-II", Category = "유도무기", RuntimeHours = 320, ThresholdHours = 500, NextMaintenanceDate = new DateTime(2026, 12, 28), LastMaintenanceDate = new DateTime(2026, 9, 29), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-002", Name = "천궁-II 다기능레이더 1호기", SystemName = "천궁-II", Category = "감시정찰", RuntimeHours = 640, ThresholdHours = 1000, NextMaintenanceDate = new DateTime(2027, 1, 12), LastMaintenanceDate = new DateTime(2026, 7, 16), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-003", Name = "천궁-II 교전통제소 1호기", SystemName = "천궁-II", Category = "지휘통제", RuntimeHours = 410, ThresholdHours = 800, NextMaintenanceDate = new DateTime(2027, 1, 10), LastMaintenanceDate = new DateTime(2026, 10, 12), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-004", Name = "L-SAM 발사대 1호기", SystemName = "L-SAM", Category = "유도무기", RuntimeHours = 120, ThresholdHours = 500, NextMaintenanceDate = new DateTime(2027, 2, 25), LastMaintenanceDate = new DateTime(2026, 8, 29), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-005", Name = "L-SAM 다기능레이더 1호기", SystemName = "L-SAM", Category = "감시정찰", RuntimeHours = 475, ThresholdHours = 500, NextMaintenanceDate = new DateTime(2026, 12, 2), LastMaintenanceDate = new DateTime(2026, 6, 5), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-006", Name = "L-SAM 교전통제소 1호기", SystemName = "L-SAM", Category = "지휘통제", RuntimeHours = 300, ThresholdHours = 800, NextMaintenanceDate = new DateTime(2027, 1, 20), LastMaintenanceDate = new DateTime(2026, 10, 22), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-007", Name = "신궁 발사장치 3호기", SystemName = "신궁", Category = "유도무기", RuntimeHours = 285, ThresholdHours = 300, NextMaintenanceDate = new DateTime(2026, 11, 23), LastMaintenanceDate = new DateTime(2026, 8, 25), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-008", Name = "현궁 발사장치 2호기", SystemName = "현궁", Category = "유도무기", RuntimeHours = 410, ThresholdHours = 600, NextMaintenanceDate = new DateTime(2027, 1, 5), LastMaintenanceDate = new DateTime(2026, 10, 7), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-009", Name = "비궁 발사대 1호기", SystemName = "비궁", Category = "유도무기", RuntimeHours = 552, ThresholdHours = 600, NextMaintenanceDate = new DateTime(2026, 12, 10), LastMaintenanceDate = new DateTime(2026, 9, 11), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-010", Name = "천마 사격통제장비 2호기", SystemName = "천마", Category = "유도무기", RuntimeHours = 505, ThresholdHours = 500, NextMaintenanceDate = new DateTime(2026, 12, 5), LastMaintenanceDate = new DateTime(2026, 9, 6), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-011", Name = "해궁 수직발사체계 1호기", SystemName = "해궁", Category = "유도무기", RuntimeHours = 150, ThresholdHours = 400, NextMaintenanceDate = new DateTime(2027, 2, 20), LastMaintenanceDate = new DateTime(2026, 8, 24), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-012", Name = "해성 발사통제장비 3호기", SystemName = "해성", Category = "유도무기", RuntimeHours = 230, ThresholdHours = 400, NextMaintenanceDate = new DateTime(2027, 1, 25), LastMaintenanceDate = new DateTime(2026, 10, 27), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-013", Name = "천경-II 대포병탐지레이더 2호기", SystemName = "천경-II", Category = "감시정찰", RuntimeHours = 1040, ThresholdHours = 1000, NextMaintenanceDate = new DateTime(2026, 11, 16), LastMaintenanceDate = new DateTime(2026, 5, 20), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-014", Name = "국지방공레이더 5호기", SystemName = "국지방공레이더", Category = "감시정찰", RuntimeHours = 60, ThresholdHours = 200, NextMaintenanceDate = new DateTime(2027, 1, 17), LastMaintenanceDate = new DateTime(2026, 10, 19), MaintenanceIntervalDays = 90 },
            new Equipment { Id = "EQ-015", Name = "TMMR 무전기 11호기", SystemName = "TMMR", Category = "통신", RuntimeHours = 365, ThresholdHours = 400, NextMaintenanceDate = new DateTime(2026, 11, 30), LastMaintenanceDate = new DateTime(2026, 6, 3), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-016", Name = "TMMR 무전기 23호기", SystemName = "TMMR", Category = "통신", RuntimeHours = 200, ThresholdHours = 800, NextMaintenanceDate = new DateTime(2027, 3, 2), LastMaintenanceDate = new DateTime(2026, 9, 3), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-017", Name = "선체고정음탐기 1호기", SystemName = "수상함 소나", Category = "해양", RuntimeHours = 720, ThresholdHours = 1000, NextMaintenanceDate = new DateTime(2026, 11, 21), LastMaintenanceDate = new DateTime(2026, 5, 25), MaintenanceIntervalDays = 180 },
            new Equipment { Id = "EQ-018", Name = "전술 전자전장비 1호기", SystemName = "전자전장비", Category = "전자전", RuntimeHours = 90, ThresholdHours = 300, NextMaintenanceDate = new DateTime(2027, 1, 28), LastMaintenanceDate = new DateTime(2026, 10, 30), MaintenanceIntervalDays = 90 },
        },
        Parts =
        {
            new Part { PartNo = "PT-1001", Name = "발사관 커넥터", Category = "유도무기", Stock = 6, MinStock = 2 },
            new Part { PartNo = "PT-1002", Name = "열전지 배터리", Category = "유도무기", Stock = 3, MinStock = 4 },
            new Part { PartNo = "PT-1003", Name = "구동장치 유압유", Category = "유도무기", Stock = 12, MinStock = 5 },
            new Part { PartNo = "PT-2001", Name = "레이더 냉각팬", Category = "감시정찰", Stock = 2, MinStock = 2 },
            new Part { PartNo = "PT-2002", Name = "T/R 모듈", Category = "감시정찰", Stock = 8, MinStock = 3 },
            new Part { PartNo = "PT-2003", Name = "RF 커넥터", Category = "감시정찰", Stock = 15, MinStock = 5 },
            new Part { PartNo = "PT-3001", Name = "안테나 케이블", Category = "통신", Stock = 7, MinStock = 3 },
            new Part { PartNo = "PT-3002", Name = "무전기 배터리 팩", Category = "통신", Stock = 5, MinStock = 3 },
            new Part { PartNo = "PT-4001", Name = "수신기 필터 모듈", Category = "전자전", Stock = 4, MinStock = 2 },
            new Part { PartNo = "PT-5001", Name = "음향센서 케이블", Category = "해양", Stock = 3, MinStock = 2 },
            new Part { PartNo = "PT-6001", Name = "운용 콘솔 디스플레이", Category = "지휘통제", Stock = 4, MinStock = 2 },
            new Part { PartNo = "PT-6002", Name = "서버 랙 냉각팬", Category = "지휘통제", Stock = 6, MinStock = 2 },
            new Part { PartNo = "PT-9001", Name = "범용 O-링 세트", Category = "공통", Stock = 40, MinStock = 10 },
            new Part { PartNo = "PT-9002", Name = "전원공급 모듈", Category = "공통", Stock = 6, MinStock = 3 },
        },
        Records =
        {
            new ReplacementRecord { Timestamp = new DateTime(2026, 10, 2, 14, 10, 0),  Kind = RecordKinds.PartReplace, EquipmentId = "EQ-001", EquipmentName = "천궁-II 발사대 1호기", PartNo = "PT-1001", PartName = "발사관 커넥터", QuantityChange = -1, Note = "점검 중 접점 부식 발견" },
            new ReplacementRecord { Timestamp = new DateTime(2026, 10, 19, 9, 30, 0),  Kind = RecordKinds.Maintenance, EquipmentId = "EQ-014", EquipmentName = "국지방공레이더 5호기", Note = "가동 200h 시점 정비 완료" },
            new ReplacementRecord { Timestamp = new DateTime(2026, 10, 20, 16, 45, 0), Kind = RecordKinds.PartReplace, EquipmentId = "EQ-013", EquipmentName = "천경-II 대포병탐지레이더 2호기", PartNo = "PT-2003", PartName = "RF 커넥터", QuantityChange = -2, Note = "접촉 불량" },
            new ReplacementRecord { Timestamp = new DateTime(2026, 11, 3, 11, 0, 0),   Kind = RecordKinds.Restock, PartNo = "PT-2002", PartName = "T/R 모듈", QuantityChange = 5, Note = "정기 보급" },
        }
    };
}
