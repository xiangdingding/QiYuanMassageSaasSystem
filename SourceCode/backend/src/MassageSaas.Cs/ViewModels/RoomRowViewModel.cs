using MassageSaas.Shared.Rooms;

namespace MassageSaas.Cs.ViewModels;

/// <summary>
/// 房间列表行：包一间 <see cref="RoomDto"/> + 其当前 Open 的计时 session（可空），
/// 供「类型/计时单价/状态/计时进度/操作」各列展示。逻辑与 BS 端 RoomsView 一致：
/// 计时房显示「计时房」标签与 ¥/时；计时中显示进度并可「取消计时」；有进行中计时时禁止删除。
/// </summary>
public class RoomRowViewModel
{
    public RoomDto Dto { get; }
    public TimedRoomSessionDto? Session { get; }

    public RoomRowViewModel(RoomDto dto, TimedRoomSessionDto? session)
    {
        Dto = dto;
        Session = session;
    }

    public long Id => Dto.Id;
    public string RoomNo => Dto.RoomNo;
    public int Capacity => Dto.Capacity;
    public bool IsTimedRoom => Dto.IsTimedRoom;
    public bool IsActive => Dto.IsActive;
    public string? Remark => Dto.Remark;

    /// <summary>类型列：计时房显示"计时房"，否则显示（规整后的）房型，空则 —。</summary>
    public string TypeDisplay => IsTimedRoom ? "计时房" : RoomTypeLabel(Dto.RoomType);

    /// <summary>计时单价列：计时房显示 ¥X/时，否则 —。</summary>
    public string HourlyRateDisplay => IsTimedRoom ? $"¥{Dto.HourlyRate:F2}/时" : "—";

    public bool HasOpenSession => Session is not null;

    /// <summary>状态列：已停用 / 计时中 / 可用。</summary>
    public string StatusDisplay =>
        !IsActive ? "已停用"
        : IsTimedRoom && HasOpenSession ? "计时中"
        : "可用";

    /// <summary>计时进度列：计时中显示已计时分钟 + 客人，否则 —。</summary>
    public string ProgressDisplay =>
        IsTimedRoom && Session is not null
            ? $"已计时 {Session.ElapsedMinutes} 分钟 · 客 {CustomerName}"
            : "—";

    private string CustomerName =>
        Session?.CustomerName is { Length: > 0 } c ? c
        : Session?.MemberName is { Length: > 0 } m ? m
        : "散客";

    /// <summary>仅"已停用且无进行中计时"的房间可删除：必须先停用才能删。</summary>
    public bool CanDelete => !IsActive && !HasOpenSession;

    /// <summary>仅计时房且计时中显示"取消计时"。</summary>
    public bool ShowCancelTiming => IsTimedRoom && HasOpenSession;

    /// <summary>停用/启用按钮文案：已启用显示"停用"，已停用显示"启用"。</summary>
    public string ToggleActiveLabel => IsActive ? "停用" : "启用";

    /// <summary>停用/启用按钮无障碍朗读名。</summary>
    public string ToggleActiveAutomationName => $"{ToggleActiveLabel}房间 {RoomNo}";

    /// <summary>历史英文房型值映射成中文展示。</summary>
    private static string RoomTypeLabel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "—";
        return raw.Trim().ToLowerInvariant() switch
        {
            "standard" => "标准间",
            "vip" => "VIP",
            "couple" => "情侣间",
            _ => raw.Trim()
        };
    }
}
