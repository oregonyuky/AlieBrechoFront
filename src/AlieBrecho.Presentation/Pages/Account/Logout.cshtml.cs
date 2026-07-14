using AlieBrecho.Domain.Auth;
using AlieBrecho.Presentation.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Account;

public class LogoutModel : PageModel
{
    public string? RedirectUrl { get; private set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Remove user-scoped state before invalidating the identity. The browser-side
        // sessionStorage is cleared by the page before the final redirect.
        HttpContext.Session.Remove(SessionCartStore.SessionKey);
        HttpContext.Session.Remove(AuthSessionKeys.AccessToken);
        HttpContext.Session.Remove(AuthSessionKeys.RefreshToken);
        HttpContext.Session.Remove(AuthSessionKeys.UserId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        RedirectUrl = Url.Page("/Account/Login") ?? "/Account/Login";
        return Page();
    }
}
