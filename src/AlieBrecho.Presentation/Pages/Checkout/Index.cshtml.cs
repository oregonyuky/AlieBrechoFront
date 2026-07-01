using AlieBrecho.Application.Cart;
using AlieBrecho.Application.Checkout;
using AlieBrecho.Domain.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Checkout;

public class IndexModel(CheckoutService checkoutService, CartService cartService) : PageModel
{
    [BindProperty]
    public CheckoutRequest Input { get; set; } = new();

    public Domain.Orders.Cart Cart { get; private set; } = new([]);

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await checkoutService.CreateOrderAsync(Input, cancellationToken);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                Cart = await cartService.GetCartAsync(cancellationToken);
                return Page();
            }

            return RedirectToPage("/Checkout/Success", new { orderId = result.OrderId });
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Nao foi possivel criar o pedido na API. Confira a conexao e os caminhos dos endpoints.";
            Cart = await cartService.GetCartAsync(cancellationToken);
            return Page();
        }
    }
}
