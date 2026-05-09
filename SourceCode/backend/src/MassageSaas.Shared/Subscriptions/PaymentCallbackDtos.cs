namespace MassageSaas.Shared.Subscriptions;

/// <summary>
/// 支付回调内部统一结构。真实通道（微信 V3 / 支付宝）的报文先在
/// CallbackController 内做签名校验、字段提取，归一为该结构后驱动业务。
/// </summary>
public record PaymentCallbackPayload(
    string OutTradeNo,
    string ThirdTradeNo,
    decimal AmountYuan,
    bool Success,
    string Channel,
    string? RawJson);

public record CallbackAck(bool Ok, string? Code, string? Message);
