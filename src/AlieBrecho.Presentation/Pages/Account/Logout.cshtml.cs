using AlieBrecho.Domain.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Account;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        HttpContext.Session.Remove(AuthSessionKeys.AccessToken);
        HttpContext.Session.Remove(AuthSessionKeys.RefreshToken);
        HttpContext.Session.Remove(AuthSessionKeys.UserId);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToPage("/Account/Login");
    }
}
