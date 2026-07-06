using AlieBrecho.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages;

public class ObrigadoModel(IOrderGateway orderGateway) : PageModel
{
    public OrderSummary? Order { get; private set; }

    public string? Pedido { get; private set; }

    public async Task OnGetAsync(string? pedido, CancellationToken cancellationToken)
    {
        Pedido = pedido;
        if (string.IsNullOrWhiteSpace(pedido))
        {
            return;
        }

        try
        {
            Order = await orderGateway.GetOrderSummaryAsync(pedido, cancellationToken);
        }
        catch (HttpRequestException)
        {
            Order = null;
        }
        catch (InvalidOperationException)
        {
            Order = null;
        }
    }
}
