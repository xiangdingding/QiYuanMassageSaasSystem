using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

/// <summary>
/// 官网业务咨询。访客在官网填写联系电话 + 咨询内容提交（匿名，提交者还不是租户），
/// 平台端客服在运营平台查看与处理。属平台级数据，<b>不做租户隔离</b>。
/// </summary>
public class BusinessConsultation : BaseEntity
{
    /// <summary>联系人姓名/称呼（选填）。</summary>
    public string? ContactName { get; set; }

    /// <summary>联系电话（必填，回访用）。</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>咨询内容（必填）。</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>来源渠道，默认 website；便于后续多入口区分。</summary>
    public string? Source { get; set; } = "website";

    /// <summary>提交者 IP（风控/去重参考，可空）。</summary>
    public string? SubmitIp { get; set; }

    /// <summary>处理状态。</summary>
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;

    /// <summary>平台客服处理备注（跟进记录/结论）。</summary>
    public string? ProcessNote { get; set; }

    /// <summary>处理人（平台账号）Id。</summary>
    public long? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }

    /// <summary>最近一次处理时间。</summary>
    public DateTime? ProcessedAt { get; set; }
}
