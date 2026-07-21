using System.Security.Claims;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Application.Cart;
using AlieBrecho.Application.Checkout;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Domain.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Checkout;

public class IndexModel(
    CheckoutService checkoutService,
    CartService cartService,
    ICustomerGateway customerGateway,
    IOrderGateway orderGateway) : PageModel
{
    [BindProperty]
    public CheckoutRequest Input { get; set; } = new();

    public Domain.Orders.Cart Cart { get; private set; } = new([]);

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);

        var authenticatedEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var customerId = GetAuthenticatedCustomerId();
        var customer = string.IsNullOrWhiteSpace(customerId)
            ? null
            : await customerGateway.GetCustomerProfileAsync(customerId, cancellationToken);

        if (customer is not null)
        {
            Input = new CheckoutRequest
            {
                CustomerId = customer.Id ?? customerId,
                FirstName = customer.FirstName ?? string.Empty,
                LastName = customer.LastName ?? string.Empty,
                Cpf = customer.Cpf,
                Email = FirstFilled(customer.EmailAddress, authenticatedEmail),
                PhoneNumber = customer.PhoneNumber ?? string.Empty,
                Street = customer.Street ?? string.Empty,
                Number = customer.Number ?? string.Empty,
                Complement = customer.Complement,
                Neighborhood = customer.Neighborhood ?? string.Empty,
                City = customer.City ?? string.Empty,
                State = customer.State ?? string.Empty,
                PostCode = customer.PostalCode ?? string.Empty
            };

            return;
        }

        if (!string.IsNullOrWhiteSpace(authenticatedEmail))
        {
            Input = Input with { Email = authenticatedEmail };
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Cart = await cartService.GetCartAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new { message = "Confira os dados do checkout." });
            }

            return Page();
        }

        try
        {
            var input = Input with { CustomerId = GetAuthenticatedCustomerId() };
            var result = await checkoutService.CreateOrderAsync(input, cancellationToken);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                Cart = await cartService.GetCartAsync(cancellationToken);
                if (IsAjaxRequest())
                {
                    return BadRequest(new { message = ErrorMessage });
                }

                return Page();
            }

            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    orderId = result.OrderId,
                    paymentUrl = result.PaymentUrl,
                    pixQrCode = result.PixQrCode,
                    pixCode = result.PixCode,
                    paymentId = result.PaymentId
                });
            }

            return string.IsNullOrWhiteSpace(result.PaymentUrl)
                ? RedirectToPage("/Payment/Pix", new { orderId = result.OrderId })
                : Redirect(result.PaymentUrl);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Nao foi possivel criar o pedido na API. Confira a conexao e os caminhos dos endpoints."
                : ex.Message;
            Cart = await cartService.GetCartAsync(cancellationToken);
            if (IsAjaxRequest())
            {
                return BadRequest(new { message = ErrorMessage });
            }

            return Page();
        }
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
            return BadRequest(new { message = quote.Message ?? "Nao foi possivel calcular o frete." });
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

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers.XRequestedWith,
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
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

    private static string FirstFilled(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
