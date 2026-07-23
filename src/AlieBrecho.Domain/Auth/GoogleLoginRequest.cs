namespace AlieBrecho.Domain.Auth;

public sealed record GoogleLoginRequest
{
    public string Credential { get; init; } = string.Empty;
    public bool RememberMe { get; init; } = true;
}
