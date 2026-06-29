namespace AlieBrecho.Domain.Auth;

public sealed record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public bool RememberMe { get; init; }
}

