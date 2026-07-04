using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Payment;

public class PixModel : PageModel
{
    public string? OrderId { get; private set; }

    public void OnGet(string? orderId)
    {
        OrderId = orderId;
    }
}
