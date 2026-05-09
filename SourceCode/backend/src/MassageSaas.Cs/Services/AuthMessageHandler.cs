using System.Net.Http;
using System.Net.Http.Headers;

namespace MassageSaas.Cs.Services;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly SessionService _session;

    public AuthMessageHandler(SessionService session) => _session = session;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
