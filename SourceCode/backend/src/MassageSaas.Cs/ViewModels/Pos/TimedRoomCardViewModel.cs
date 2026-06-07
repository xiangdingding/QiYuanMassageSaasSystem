using CommunityToolkit.Mvvm.ComponentModel;
using MassageSaas.Shared.Rooms;

namespace MassageSaas.Cs.ViewModels.Pos;

/// <summary>
/// 收银台「计时房费」区里的一间计时房卡片：包一间 <see cref="RoomDto"/> + 其当前 Open 的计时 session（可空）
/// + 该 session 是否已加入购物车。逻辑与 BS 端 PosView 的 timedRooms / openSessionOf / cartRoomSessionIds 一致。
/// </summary>
public class TimedRoomCardViewModel
{
    public RoomDto Room { get; }
    public TimedRoomSessionDto? Session { get; }
    /// <summary>该房当前 session 是否已被加入购物车（避免重复加入）。</summary>
    public bool InCart { get; }

    public TimedRoomCardViewModel(RoomDto room, TimedRoomSessionDto? session, bool inCart)
    {
        Room = room;
        Session = session;
        InCart = inCart;
    }

    public string RoomNo => Room.RoomNo;
    public decimal HourlyRate => Room.HourlyRate;

    public bool HasOpenSession => Session is not null;
    public bool IsIdle => Session is null;

    public int ElapsedMinutes => Session?.ElapsedMinutes ?? 0;

    /// <summary>按已计时分钟估算的房费（与加入购物车时的快照算法一致）。</summary>
    public decimal EstimatedAmount =>
        Session is null ? 0m : System.Math.Round(Session.ElapsedMinutes / 60m * Session.HourlyRateSnapshot, 2);

    /// <summary>客人称呼：散客姓名 → 会员名 → "散客"。</summary>
    public string CustomerLabel =>
        Session?.CustomerName is { Length: > 0 } c ? c
        : Session?.MemberName is { Length: > 0 } m ? m
        : "散客";

    public bool CanAddToCart => HasOpenSession && !InCart;
    public string AddButtonLabel => InCart ? "已加入" : "加入订单";
}
