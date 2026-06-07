using System.Text.Json;
using System.Text.Json.Serialization;
using MassageSaas.Api.Json;
using MassageSaas.Shared.Auth;
using Xunit;
using Xunit.Abstractions;

namespace MassageSaas.UnitTests.Auth;

/// <summary>临时探针：API 真实序列化（Web 默认 + 北京时间转换器）→ CS 反序列化，验证 User 是否丢失。</summary>
public class LoginRoundTripProbe
{
    private readonly ITestOutputHelper _o;
    public LoginRoundTripProbe(ITestOutputHelper o) => _o = o;

    [Fact]
    public void RoundTrip_Api_To_Cs()
    {
        // 1) API 端配置：AddControllers 默认是 Web（camelCase + 大小写不敏感），再加我的转换器
        var apiOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        apiOpts.Converters.Add(new BeijingDateTimeConverter());
        apiOpts.Converters.Add(new BeijingNullableDateTimeConverter());

        var resp = new LoginResponse(
            "access-token", "refresh-token",
            System.DateTime.UtcNow.AddMinutes(120),
            new UserInfo(1, "mgr", "张三", "StoreManager", 2, 3, false));

        var json = JsonSerializer.Serialize(resp, apiOpts);
        _o.WriteLine("API 下发 JSON: " + json);

        // 2) CS 端配置（Refit 同款）
        var csOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        var back = JsonSerializer.Deserialize<LoginResponse>(json, csOpts);
        _o.WriteLine($"CS 反序列化: User={(back?.User is null ? "NULL" : back.User.Username)}, ExpiresAt={back?.ExpiresAt:o}");

        Assert.NotNull(back);
        Assert.NotNull(back!.User);
    }
}
