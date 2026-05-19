using System.Net;
using System.Text;
using FluentAssertions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using MassageSaas.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MassageSaas.UnitTests.Notifications;

public class WeChatDispatcherTests
{
    /// <summary>按请求 URL 关键字返回预置响应的假 handler。</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _responder;
        public int CallCount { get; private set; }

        public StubHandler(Func<HttpRequestMessage, string> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responder(request), Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubTokenProvider : IWeChatAccessTokenProvider
    {
        private readonly string? _token;
        public StubTokenProvider(string? token) => _token = token;
        public Task<string?> GetTokenAsync(CancellationToken ct) => Task.FromResult(_token);
    }

    private static WeChatOptions Options(bool withTemplate = true)
    {
        var o = new WeChatOptions
        {
            Enabled = true, AppId = "wxapp", AppSecret = "secret",
            Page = "pages/index/index", MiniProgramState = "formal"
        };
        if (withTemplate) o.Templates["RechargeArrived"] = "TMPL_RECHARGE";
        return o;
    }

    private static NotificationOutbox Notification(string? openId) => new()
    {
        TenantId = 1, Kind = NotificationKind.RechargeArrived,
        Status = NotificationStatus.Pending, DedupKey = "k1",
        RecipientOpenId = openId, Title = "充值到账", Body = "充值 100 元已到账",
        ScheduledAt = DateTime.UtcNow
    };

    private static WeChatNotificationDispatcher NewDispatcher(
        HttpMessageHandler handler, IWeChatAccessTokenProvider tokenProvider, WeChatOptions options) =>
        new(new StubHttpClientFactory(handler), tokenProvider,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<WeChatNotificationDispatcher>.Instance);

    [Fact]
    public async Task Send_FailsWhenNoOpenId()
    {
        var handler = new StubHandler(_ => "{\"errcode\":0}");
        var d = NewDispatcher(handler, new StubTokenProvider("TOKEN"), Options());
        var result = await d.SendAsync(Notification(openId: null), default);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("OpenId");
        handler.CallCount.Should().Be(0, "无 OpenId 时不应调用微信");
    }

    [Fact]
    public async Task Send_FailsWhenTemplateNotConfigured()
    {
        var handler = new StubHandler(_ => "{\"errcode\":0}");
        var d = NewDispatcher(handler, new StubTokenProvider("TOKEN"), Options(withTemplate: false));
        var result = await d.SendAsync(Notification("openid-1"), default);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("模板");
    }

    [Fact]
    public async Task Send_FailsWhenTokenUnavailable()
    {
        var handler = new StubHandler(_ => "{\"errcode\":0}");
        var d = NewDispatcher(handler, new StubTokenProvider(null), Options());
        var result = await d.SendAsync(Notification("openid-1"), default);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("access_token");
    }

    [Fact]
    public async Task Send_SucceedsOnErrcodeZero()
    {
        var handler = new StubHandler(_ => "{\"errcode\":0,\"errmsg\":\"ok\"}");
        var d = NewDispatcher(handler, new StubTokenProvider("TOKEN"), Options());
        var result = await d.SendAsync(Notification("openid-1"), default);

        result.Success.Should().BeTrue();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_FailsOnWeChatErrcode()
    {
        var handler = new StubHandler(_ => "{\"errcode\":43101,\"errmsg\":\"user refuse to accept the msg\"}");
        var d = NewDispatcher(handler, new StubTokenProvider("TOKEN"), Options());
        var result = await d.SendAsync(Notification("openid-1"), default);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("43101");
    }

    [Fact]
    public async Task Send_PostsToSubscribeSendEndpoint()
    {
        string? calledUrl = null;
        var handler = new StubHandler(req => { calledUrl = req.RequestUri!.ToString(); return "{\"errcode\":0}"; });
        var d = NewDispatcher(handler, new StubTokenProvider("TOKEN"), Options());
        await d.SendAsync(Notification("openid-1"), default);

        calledUrl.Should().Contain("message/subscribe/send");
        calledUrl.Should().Contain("access_token=TOKEN");
    }

    [Fact]
    public async Task AccessTokenProvider_CachesToken_AcrossCalls()
    {
        var handler = new StubHandler(_ => "{\"access_token\":\"AT-123\",\"expires_in\":7200}");
        var provider = new WeChatAccessTokenProvider(
            new StubHttpClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(Options()),
            NullLogger<WeChatAccessTokenProvider>.Instance);

        var t1 = await provider.GetTokenAsync(default);
        var t2 = await provider.GetTokenAsync(default);

        t1.Should().Be("AT-123");
        t2.Should().Be("AT-123");
        handler.CallCount.Should().Be(1, "token 在有效期内只拉一次");
    }

    [Fact]
    public async Task AccessTokenProvider_ReturnsNullOnWeChatError()
    {
        var handler = new StubHandler(_ => "{\"errcode\":40013,\"errmsg\":\"invalid appid\"}");
        var provider = new WeChatAccessTokenProvider(
            new StubHttpClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(Options()),
            NullLogger<WeChatAccessTokenProvider>.Instance);

        var token = await provider.GetTokenAsync(default);
        token.Should().BeNull();
    }
}
