using AlieBrecho.Domain.Auth;

namespace AlieBrecho.Application.Abstractions;

public interface IAuthenticationGateway
{
    Task<LoginSession?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<LoginSession?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken);
    Task<RegisterResult?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}
