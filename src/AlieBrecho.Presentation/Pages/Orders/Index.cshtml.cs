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
        var bags = await bagGateway.GetPurchaseHistoryAsync(customerId, cancellationToken);

        var purchases = orders.Select(order => new
        {
            orderId = order.OrderId,
            purchaseType = "order",
            status = order.Status,
            totalAmount = order.TotalAmount,
            shippingCost = order.ShippingCost,
            amountPaid = order.AmountPaid,
            paidAt = (DateTime?)null,
            items = order.Items
        }).Concat(bags.Select(bag => new
        {
            orderId = bag.Id,
            purchaseType = "bag",
            status = bag.Status,
            totalAmount = (decimal?)(bag.TotalItemsValue + (bag.ShippingCost ?? 0m)),
            shippingCost = bag.ShippingCost,
            amountPaid = (decimal?)bag.TotalItemsValue,
            paidAt = bag.PaidAt,
            items = bag.Items
        })).OrderByDescending(x => x.paidAt).ToArray();

        return new JsonResult(purchases);
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
