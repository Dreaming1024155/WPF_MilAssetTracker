using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace WPF_프로젝트;

public static class RecordKinds
{
    public const string PartReplace = "부품 교체";
    public const string Maintenance = "정기 정비";
    public const string Restock = "입고";
}

/// <summary>정비/부품 교체/입고 이력 1건</summary>
public class ReplacementRecord
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Kind { get; set; } = RecordKinds.PartReplace;
    public string EquipmentId { get; set; } = "";
    public string EquipmentName { get; set; } = "-";
    public string PartNo { get; set; } = "";
    public string PartName { get; set; } = "";

    /// <summary>재고 증감량 (교체 = 음수, 입고 = 양수)</summary>
    public int QuantityChange { get; set; }

    public string Note { get; set; } = "";

    [JsonIgnore]
    public string Summary
    {
        get
        {
            var text = string.IsNullOrEmpty(PartName)
                ? Note
                : $"{PartName} {Math.Abs(QuantityChange)}개" + (string.IsNullOrWhiteSpace(Note) ? "" : $" · {Note}");
            return string.IsNullOrWhiteSpace(text) ? Kind : text;
        }
    }
}
