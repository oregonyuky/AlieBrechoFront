using AlieBrecho.Application.Cart;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Cart;

public class IndexModel(CartService cartService) : PageModel
{
    public Domain.Orders.Cart Cart { get; private set; } = new([]);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);
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

    private bool WantsJson()
    {
        return Request.Headers.Accept.Any(value => value?.Contains("application/json") == true) ||
            Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
