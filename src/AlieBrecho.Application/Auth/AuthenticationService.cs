using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Auth;

namespace AlieBrecho.Application.Auth;

public sealed class AuthenticationService(IAuthenticationGateway gateway)
{
    public async Task<LoginSession?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        return await gateway.LoginAsync(request, cancellationToken);
    }

    public async Task<LoginSession?> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            return null;
        }

        return await gateway.LoginWithGoogleAsync(request, cancellationToken);
    }

    public async Task<LoginSession?> RegisterAndLoginAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password != request.ConfirmPassword ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return null;
        }

        var register = await gateway.RegisterAsync(request, cancellationToken);
        if (register is null)
        {
            throw new InvalidOperationException("A API nao retornou os dados da conta criada.");
        }

        var session = await gateway.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe
        }, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("Conta criada, mas nao foi possivel entrar automaticamente. Tente fazer login com o e-mail e a senha cadastrados.");
        }

        return session;
    }
}
