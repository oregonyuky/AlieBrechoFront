using AlieBrecho.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Payment;

public class PixModel(IOrderGateway orderGateway) : PageModel
{
    public string? OrderId { get; private set; }

    public void OnGet(string? orderId)
    {
        OrderId = orderId;
    }

    public async Task<IActionResult> OnGetStatusAsync(
        string? paymentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return BadRequest(new { message = "Pagamento nao informado." });
        }

        var status = await orderGateway.GetMercadoPagoPixPaymentStatusAsync(paymentId, cancellationToken);
        if (status is null)
        {
            return NotFound(new { message = "Pagamento nao encontrado." });
        }

        return new JsonResult(new
        {
            paymentId = status.PaymentId,
            status = status.Status,
            statusDetail = status.StatusDetail,
            orderStatus = status.OrderStatus
        });
    }
}
