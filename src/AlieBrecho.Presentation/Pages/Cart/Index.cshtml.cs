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
        return RedirectToPage();
    }
}
