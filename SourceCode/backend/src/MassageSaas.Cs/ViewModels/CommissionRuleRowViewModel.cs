using System.ComponentModel;
using System.Globalization;
using MassageSaas.Shared.Commissions;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 提成规则列表行：把"规则类型中文化、数值展示（双值轮钟/点钟 vs 单值）、状态"算到 VM。
/// 展示策略对齐 BS CommissionsView：FixedAmount/Percentage 按双值两行渲染（旧规则未设双值时用 Amount 兜底），
/// Timed 显示「¥X/小时」，Tiered 显示默认 Amount。
/// </summary>
public class CommissionRuleRowViewModel : INotifyPropertyChanged
{
    public CommissionRuleDto Dto { get; }

    public CommissionRuleRowViewModel(CommissionRuleDto dto) => Dto = dto;

    /// <summary>列表多选勾选状态（批量启用/禁用用）。</summary>
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked != value) { _isChecked = value; PropertyChanged?.Invoke(this, new(nameof(IsChecked))); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string RuleTypeLabel => Dto.RuleType switch
    {
        "FixedAmount" => "固定金额",
        "Percentage" => "百分比",
        "Tiered" => "阶梯",
        "Timed" => "按时计费",
        _ => Dto.RuleType
    };

    public string ServiceDisplay => Dto.ServiceName ?? "全部服务";
    public string TechnicianDisplay => Dto.TechnicianName ?? "全部技师";

    /// <summary>FixedAmount / Percentage 走双值两行展示。</summary>
    public bool IsDual => Dto.RuleType is "FixedAmount" or "Percentage";

    private bool IsPercent => Dto.RuleType == "Percentage";

    private string FormatPart(decimal v) =>
        IsPercent ? $"{v.ToString("0.####", CultureInfo.InvariantCulture)}%" : $"¥{v:F2}";

    public string RotationText => $"轮钟 {FormatPart(Dto.RotationAmount ?? Dto.Amount)}";
    public string DesignationText => $"点钟 {FormatPart(Dto.DesignationAmount ?? Dto.Amount)}";

    /// <summary>单值展示（Timed / Tiered）。</summary>
    public string ValueText => Dto.RuleType switch
    {
        "Timed" => $"¥{Dto.Amount.ToString("0.####", CultureInfo.InvariantCulture)}/小时",
        "Percentage" => $"{Dto.Amount.ToString("0.####", CultureInfo.InvariantCulture)}%",
        _ => $"¥{Dto.Amount:F2}"
    };

    public int Priority => Dto.Priority;
    public bool IsActive => Dto.IsActive;
    public string StatusLabel => Dto.IsActive ? "启用" : "停用";

    /// <summary>仅停用规则可删除（对齐 BS：启用中的规则需先禁用）。</summary>
    public bool CanDelete => !Dto.IsActive;
}
