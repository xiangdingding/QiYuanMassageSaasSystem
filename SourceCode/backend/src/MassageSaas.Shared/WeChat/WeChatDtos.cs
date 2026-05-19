namespace MassageSaas.Shared.WeChat;

/// <summary>小程序 wx.login 拿到的临时 code，换 OpenId。</summary>
public record WeChatSessionRequest(string Code);

/// <summary>换得的稳定 OpenId。session_key 不下发给前端。</summary>
public record WeChatSessionResponse(string OpenId);

/// <summary>把当前小程序用户（OpenId）绑定到某门店下手机号对应的会员卡。</summary>
public record BindMemberRequest(long StoreId, string Phone, string OpenId);

/// <summary>绑定成功后回给小程序的会员概要，用于"我的"页展示。</summary>
public record BoundMemberDto(
    long MemberId,
    string CardNo,
    string? Name,
    decimal Balance,
    string Level);

/// <summary>面向顾客的订阅消息模板：NotificationKind 名 → 微信模板 ID。供 wx.requestSubscribeMessage 申请授权。</summary>
public record SubscribeTemplatesResponse(Dictionary<string, string> Templates);
