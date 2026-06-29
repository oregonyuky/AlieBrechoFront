using AlieBrecho.Application.Checkout;
using AlieBrecho.Domain.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Checkout;

public class IndexModel(CheckoutService checkoutService) : PageModel
{
    [BindProperty]
    public CheckoutRequest Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
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
                return Page();
            }

            return RedirectToPage("/Checkout/Success", new { orderId = result.OrderId });
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Nao foi possivel criar o pedido na API. Confira a conexao e os caminhos dos endpoints.";
            return Page();
        }
    }
}
