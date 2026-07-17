using AlieBrecho.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AlieBrecho.Presentation.Pages.Orders;

[Authorize]
public class IndexModel(IBagGateway bagGateway, IOrderGateway orderGateway) : PageModel
{
    public async Task<IActionResult> OnGetPurchasesAsync(CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Unauthorized();
        }

        var orders = await orderGateway.GetOrdersByCustomerAsync(customerId, cancellationToken);
        return new JsonResult(orders);
    }

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
