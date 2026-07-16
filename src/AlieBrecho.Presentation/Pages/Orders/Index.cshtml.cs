using AlieBrecho.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Orders;

[Authorize]
public class IndexModel(IBagGateway bagGateway) : PageModel
{
    public async Task<IActionResult> OnGetBagExistsAsync(
        string? bagId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bagId))
        {
            return BadRequest(new { message = "Sacolinha nao informada." });
        }

        try
        {
            var bag = await bagGateway.GetBagAsync(bagId, cancellationToken);
            return new JsonResult(new { exists = bag is not null });
        }
        catch (HttpRequestException)
        {
            return new JsonResult(new { exists = true });
        }
    }
}
