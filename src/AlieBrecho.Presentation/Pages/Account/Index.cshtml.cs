using System.Security.Claims;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Domain.Bags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Account;

[Authorize]
public class IndexModel(IBagGateway bagGateway) : PageModel
{
    public string? Email { get; private set; }
    public BagSummary? ActiveBag { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Email = User.FindFirst(ClaimTypes.Email)?.Value;
        ActiveBag = await GetActiveBagAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostFinalizeBagAsync(string bagId, CancellationToken cancellationToken)
    {
        await bagGateway.FinalizeBagAsync(bagId, cancellationToken);
        return RedirectToPage();
    }

    private async Task<BagSummary?> GetActiveBagAsync(CancellationToken cancellationToken)
    {
        var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            customerId = HttpContext.Session.GetString(AuthSessionKeys.UserId);
        }

        return string.IsNullOrWhiteSpace(customerId)
            ? null
            : await bagGateway.GetActiveBagAsync(customerId, cancellationToken);
    }
}
