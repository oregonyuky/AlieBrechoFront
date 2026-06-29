using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Checkout;

public class SuccessModel : PageModel
{
    public string? OrderId { get; private set; }

    public void OnGet(string? orderId)
    {
        OrderId = orderId;
    }
}
