using System.Security.Claims;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Application.Cart;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Domain.Bags;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Cart;

public class IndexModel(CartService cartService, IBagGateway bagGateway, IOrderGateway orderGateway) : PageModel
{
    public Domain.Orders.Cart Cart { get; private set; } = new([]);
    public BagSummary? ActiveBag { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);
        ActiveBag = await GetActiveBagAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRemoveAsync(string productId, CancellationToken cancellationToken)
    {
        await cartService.RemoveAsync(productId, cancellationToken);
        return WantsJson() ? await CartJsonAsync(cancellationToken) : RedirectToPage();
    }

    public async Task<IActionResult> OnGetSummaryAsync(CancellationToken cancellationToken)
    {
        return await CartJsonAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetShippingAsync(string postCode, CancellationToken cancellationToken)
    {
        var cart = await cartService.GetCartAsync(cancellationToken);
        if (cart.IsEmpty)
        {
            return BadRequest(new { message = "Seu carrinho esta vazio." });
        }

        var quote = await orderGateway.CalculateAutomaticShippingAsync(postCode, cart, cancellationToken);
        if (!quote.Success)
        {
            return BadRequest(new { message = quote.Message });
        }

        return new JsonResult(new
        {
            shippingCost = quote.ShippingCost,
            shippingCostText = quote.ShippingCost.ToString("C"),
            packageName = quote.PackageName,
            occupationPoints = quote.OccupationPoints,
            capacityPoints = quote.CapacityPoints,
            carrierName = quote.CarrierName
        });
    }

    public async Task<IActionResult> OnPostFinalizeBagAsync(string bagId, CancellationToken cancellationToken)
    {
        var result = await bagGateway.FinalizeBagAsync(bagId, cancellationToken);
        if (result is null)
        {
            return BadRequest(new { message = "Nao foi possivel finalizar a sacolinha." });
        }

        if (!WantsJson())
        {
            return RedirectToPage();
        }

        return new JsonResult(new
        {
            bagId = result.BagId,
            status = result.Status,
            shippingCost = result.ShippingCost,
            shippingCostText = result.ShippingCost?.ToString("C"),
            totalAmount = result.TotalAmount,
            totalAmountText = result.TotalAmount?.ToString("C")
        });
    }

    public async Task<IActionResult> OnPostAddAsync(string productId, CancellationToken cancellationToken)
    {
        await cartService.AddAsync(productId, cancellationToken);
        return WantsJson() ? await CartJsonAsync(cancellationToken) : RedirectToPage();
    }

    public async Task<IActionResult> OnPostDecrementAsync(string productId, CancellationToken cancellationToken)
    {
        await cartService.DecrementAsync(productId, cancellationToken);
        return WantsJson() ? await CartJsonAsync(cancellationToken) : RedirectToPage();
    }

    private async Task<JsonResult> CartJsonAsync(CancellationToken cancellationToken)
    {
        var cart = await cartService.GetCartAsync(cancellationToken);

        return new JsonResult(new
        {
            itemCount = cart.ItemCount,
            subtotal = cart.Subtotal,
            subtotalText = cart.Subtotal.ToString("C"),
            isEmpty = cart.IsEmpty,
            activeBag = MapBag(await GetActiveBagAsync(cancellationToken)),
            items = cart.Items.Select(item => new
            {
                id = item.Product.Id,
                name = item.Product.Name,
                size = item.Product.Sizes.FirstOrDefault()?.Size,
                imageUrl = item.Product.MainImageUrl,
                price = item.Product.DisplayPrice,
                priceText = item.Product.DisplayPrice.ToString("C"),
                quantity = item.Quantity,
                total = item.Total,
                totalText = item.Total.ToString("C")
            })
        });
    }

    private async Task<BagSummary?> GetActiveBagAsync(CancellationToken cancellationToken)
    {
        var customerId = GetAuthenticatedCustomerId();
        var bag = string.IsNullOrWhiteSpace(customerId)
            ? null
            : await bagGateway.GetActiveBagAsync(customerId, cancellationToken);

        if (bag is null || bag.Items.Count > 0 || string.IsNullOrWhiteSpace(bag.Id))
        {
            return bag;
        }

        try
        {
            return bag with { Items = await orderGateway.GetOrderItemsAsync(bag.Id, cancellationToken) };
        }
        catch (HttpRequestException)
        {
            return bag;
        }
        catch (InvalidOperationException)
        {
            return bag;
        }
    }

    private static object? MapBag(BagSummary? bag)
    {
        if (bag is null)
        {
            return null;
        }

        return new
        {
            id = bag.Id,
            status = bag.Status,
            itemCount = bag.ItemCount,
            expirationDate = bag.ExpirationDate,
            expirationDateText = bag.ExpirationDate?.ToString("dd/MM/yyyy HH:mm"),
            totalItemsValue = bag.TotalItemsValue,
            totalItemsValueText = bag.TotalItemsValue.ToString("C"),
            shippingCost = bag.ShippingCost,
            shippingCostText = bag.ShippingCost?.ToString("C"),
            items = bag.Items.Select(item => new
            {
                productId = item.ProductId,
                name = item.Name,
                imageUrl = item.ImageUrl,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                unitPriceText = item.UnitPrice.ToString("C"),
                total = item.Total,
                totalText = item.Total.ToString("C")
            })
        };
    }

    private string? GetAuthenticatedCustomerId()
    {
        var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            return customerId;
        }

        return HttpContext.Session.GetString(AuthSessionKeys.UserId);
    }

    private bool WantsJson()
    {
        return Request.Headers.Accept.Any(value => value?.Contains("application/json") == true) ||
            Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
