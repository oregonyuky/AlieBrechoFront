using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AlieBrecho.Domain.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AppAuthenticationService = AlieBrecho.Application.Auth.AuthenticationService;

namespace AlieBrecho.Presentation.Pages.Account;

[AllowAnonymous]
public class RegisterModel(AppAuthenticationService authenticationService) : PageModel
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var session = await authenticationService.RegisterAndLoginAsync(new RegisterRequest
            {
                Email = Input.Email,
                Password = Input.Password,
                ConfirmPassword = Input.ConfirmPassword,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                CompanyName = Input.CompanyName,
                RememberMe = Input.RememberMe
            }, cancellationToken);

            if (session is null)
            {
                ErrorMessage = "Nao foi possivel criar a conta. Confira os dados e tente novamente.";
                return Page();
            }

            await SignInAsync(session);
            SaveApiTokens(session);

            return LocalRedirect(GetSafeReturnUrl());
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Nao foi possivel cadastrar na API. Confira a conexao e tente novamente."
                : ex.Message;
            return Page();
        }
    }

    private async Task SignInAsync(LoginSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId ?? string.Empty),
            new(ClaimTypes.Name, session.DisplayName),
            new(ClaimTypes.Email, session.Email),
            new(AuthSessionKeys.AccessToken, session.AccessToken)
        };

        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            claims.Add(new Claim(AuthSessionKeys.RefreshToken, session.RefreshToken));
        }

        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = GetCookieExpirationUtc(session.AccessToken)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }

    private DateTimeOffset? GetCookieExpirationUtc(string accessToken)
    {
        var tokenExpirationUtc = JwtTokenReader.GetExpirationUtc(accessToken);
        if (tokenExpirationUtc is not null)
        {
            return tokenExpirationUtc;
        }

        return Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null;
    }

    private void SaveApiTokens(LoginSession session)
    {
        HttpContext.Session.SetString(AuthSessionKeys.AccessToken, session.AccessToken);

        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            HttpContext.Session.SetString(AuthSessionKeys.RefreshToken, session.RefreshToken);
        }

        if (!string.IsNullOrWhiteSpace(session.UserId))
        {
            HttpContext.Session.SetString(AuthSessionKeys.UserId, session.UserId);
        }
    }

    private string GetSafeReturnUrl()
    {
        return !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : Url.Page("/Index") ?? "/";
    }

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "Informe o nome.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o sobrenome.")]
        public string LastName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare(nameof(Password), ErrorMessage = "As senhas nao coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
