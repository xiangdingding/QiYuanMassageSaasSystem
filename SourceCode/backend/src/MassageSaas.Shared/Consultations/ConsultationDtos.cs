namespace MassageSaas.Shared.Consultations;

/// <summary>官网访客提交业务咨询（匿名）。</summary>
public record CreateConsultationRequest(
    string? ContactName,
    string Phone,
    string Content,
    string? Source);

/// <summary>平台端列表/详情展示的业务咨询。</summary>
public record ConsultationDto(
    long Id,
    string? ContactName,
    string Phone,
    string Content,
    string? Source,
    string Status,
    string? ProcessNote,
    string? ProcessedByName,
    DateTime? ProcessedAt,
    DateTime CreatedAt);

/// <summary>平台端处理业务咨询：更新状态 + 备注。</summary>
public record ProcessConsultationRequest(
    string Status,
    string? ProcessNote);
