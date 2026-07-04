using System.Net.Http.Headers;
using AlieBrecho.Domain.Auth;
using Microsoft.AspNetCore.Http;

namespace AlieBrecho.Infrastructure.Api;

internal sealed class AlieBrechoApiAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    internal static readonly HttpRequestOptionsKey<bool> SkipAuthorizationOption =
        new("AlieBrecho.SkipAuthorization");

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(SkipAuthorizationOption, out var skipAuthorization) &&
            skipAuthorization)
        {
            return base.SendAsync(request, cancellationToken);
        }

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
