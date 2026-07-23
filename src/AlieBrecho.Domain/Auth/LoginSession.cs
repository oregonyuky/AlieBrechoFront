namespace AlieBrecho.Domain.Auth;

public sealed record LoginSession
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PictureUrl { get; init; }
    public string? AuthenticationProvider { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];

    public string DisplayName
    {
        get
        {
            var fullName = string.Join(' ', new[] { FirstName, LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return string.IsNullOrWhiteSpace(fullName) ? Email : fullName;
        }
    }
}
