using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace MassageSaas.IntegrationTests;

/// <summary>HTTP 管线集成测试：健康检查、认证拦截、匿名接口、路由。</summary>
public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/health");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        // database 检查项应在明细里
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(e => e.GetProperty("name").GetString())
            .ToList();
        checks.Should().Contain("database");
    }

    [Fact]
    public async Task Protected_endpoint_returns_401_without_token()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/tenants");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_storefront_endpoint_is_reachable()
    {
        var client = _factory.CreateClient();

        // 匿名可达：请求穿过认证/租户中间件进入控制器；门店不存在 → 400
        var resp = await client.GetAsync("/api/storefront/services?storeId=999999");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/this-route-does-not-exist");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_with_bad_credentials_returns_error_not_500()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsync("/api/auth/login",
            new StringContent("{\"username\":\"nobody\",\"password\":\"wrong\"}",
                System.Text.Encoding.UTF8, "application/json"));

        // 关键是没有 500：凭证错误应是 4xx
        ((int)resp.StatusCode).Should().BeInRange(400, 499);
    }
}
