using System.Net.Http.Headers;
using AlieBrecho.Domain.Auth;
using Microsoft.AspNetCore.Http;

namespace AlieBrecho.Infrastructure.Api;

internal sealed class AlieBrechoApiAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext?.Session.GetString(AuthSessionKeys.AccessToken);
        accessToken ??= httpContext?.User.FindFirst(AuthSessionKeys.AccessToken)?.Value;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
