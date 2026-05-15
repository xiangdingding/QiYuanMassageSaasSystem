using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 通知出箱：所有要发出去的通知（生日 / 卡到期 / 预约提醒 / 充值到账）都先写到这里，
/// 由 BackgroundService 周期取出来调通道（微信/短信/日志）。
/// 失败可重试，状态扭转记录到 Status / RetryCount / ErrorMessage。
/// 幂等通过 (TenantId + DedupKey) 唯一索引保证同一来源不重复入箱。
/// </summary>
public class NotificationOutbox : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public NotificationKind Kind { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>幂等键。例如 "Birthday:{memberId}:{yyyy-MM-dd}"、"Pkg:{packageId}:{expireDay}"、"Apt:{aptId}"。</summary>
    public string DedupKey { get; set; } = null!;

    public long? MemberId { get; set; }
    public Member? Member { get; set; }
    /// <summary>订阅消息推送目标 OpenId（小程序）；可空。</summary>
    public string? RecipientOpenId { get; set; }
    /// <summary>短信目标手机号；可空。</summary>
    public string? RecipientPhone { get; set; }
    /// <summary>展示用标题（不要把全部 payload 塞这里，由模板渲染）。</summary>
    public string Title { get; set; } = null!;
    /// <summary>展示用正文，纯文本（≤ 500 字）。模板渲染好的最终文案。</summary>
    public string Body { get; set; } = null!;
    /// <summary>原始 payload JSON，留给后续具体通道做模板字段映射。</summary>
    public string? PayloadJson { get; set; }

    /// <summary>预约/到期类需要的关联 Id（用 long? 占一位，不挂导航；具体看 Kind）。</summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>计划发送时间（UtcNow 之后才会被 dispatcher 取走）。</summary>
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
