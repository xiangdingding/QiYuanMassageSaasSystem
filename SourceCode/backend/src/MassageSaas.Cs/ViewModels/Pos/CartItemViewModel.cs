using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Shared.Rooms;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels.Pos;

public partial class CartItemViewModel : ObservableObject
{
    /// <summary>数量上下限：与 BS 端一致（最少 1 钟、最多 20 钟）。</summary>
    private const int MinQty = 1;
    private const int MaxQty = 20;

    /// <summary>行类型：service=普通服务项（选技师/房间/数量）；roomCharge=计时房费快照（无技师/数量）。与 BS 端一致。</summary>
    public string Kind { get; init; } = "service";
    public bool IsService => Kind == "service";
    public bool IsRoomCharge => Kind == "roomCharge";

    public long ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }

    // ---- 计时房费专属（roomCharge 行）----
    /// <summary>计时 session id；结账时并入 CreateOrderRequest.RoomSessionIds。</summary>
    public long SessionId { get; init; }
    public long RoomId { get; init; }
    public string? RoomNo { get; init; }
    public int ElapsedMinutes { get; init; }
    public decimal HourlyRate { get; init; }
    /// <summary>开台时绑定的会员（按"人"判定）；null=散客开台无约束。结算前校验"绑定会员=结算会员"。</summary>
    public long? BoundMemberId { get; init; }
    public string? BoundMemberName { get; init; }

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private int quantity = 1;

    [ObservableProperty]
    private StaffDto? technician;

    [ObservableProperty]
    private RoomDto? room;

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));

    partial void OnQuantityChanged(int value)
    {
        // 钳制到 [1,20]：手输或步进越界时自动拉回（赋值会再次触发本方法，落在范围内即停止）
        if (value < MinQty) { Quantity = MinQty; return; }
        if (value > MaxQty) { Quantity = MaxQty; return; }
        OnPropertyChanged(nameof(LineTotal));
    }

    /// <summary>数量 +1（购物车行内"＋"按钮）。</summary>
    [RelayCommand]
    private void Increment() => Quantity++;

    /// <summary>数量 −1（购物车行内"－"按钮）。</summary>
    [RelayCommand]
    private void Decrement() => Quantity--;
}
