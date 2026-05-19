using System.Net;
using System.Text;
using FluentAssertions;
using MassageSaas.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MassageSaas.UnitTests.Notifications;

public class WeChatMiniProgramClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _responder;
        public string? LastUrl { get; private set; }

        public StubHandler(Func<HttpRequestMessage, string> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUrl = request.RequestUri!.ToString();
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

    private static WeChatMiniProgramClient NewClient(HttpMessageHandler handler, bool withCredentials = true)
    {
        var options = new WeChatOptions();
        if (withCredentials) { options.AppId = "wxapp"; options.AppSecret = "secret"; }
        return new WeChatMiniProgramClient(
            new StubHttpClientFactory(handler),
            Options.Create(options),
            NullLogger<WeChatMiniProgramClient>.Instance);
    }

    [Fact]
    public async Task Code2Session_ReturnsOpenId_OnSuccess()
    {
        var handler = new StubHandler(_ => "{\"openid\":\"OPENID-1\",\"session_key\":\"sk\"}");
        var client = NewClient(handler);

        var session = await client.Code2SessionAsync("code-1", default);

        session.Should().NotBeNull();
        session!.OpenId.Should().Be("OPENID-1");
        handler.LastUrl.Should().Contain("sns/jscode2session");
        handler.LastUrl.Should().Contain("js_code=code-1");
    }

    [Fact]
    public async Task Code2Session_ReturnsNull_OnWeChatError()
    {
        var handler = new StubHandler(_ => "{\"errcode\":40029,\"errmsg\":\"invalid code\"}");
        var client = NewClient(handler);

        var session = await client.Code2SessionAsync("bad-code", default);
        session.Should().BeNull();
    }

    [Fact]
    public async Task Code2Session_ReturnsNull_WhenNotConfigured()
    {
        var handler = new StubHandler(_ => "{\"openid\":\"X\"}");
        var client = NewClient(handler, withCredentials: false);

        client.IsConfigured.Should().BeFalse();
        var session = await client.Code2SessionAsync("code-1", default);
        session.Should().BeNull();
    }

    [Fact]
    public async Task Code2Session_ReturnsNull_OnBlankCode()
    {
        var handler = new StubHandler(_ => "{\"openid\":\"X\"}");
        var client = NewClient(handler);

        var session = await client.Code2SessionAsync("  ", default);
        session.Should().BeNull();
    }
}
