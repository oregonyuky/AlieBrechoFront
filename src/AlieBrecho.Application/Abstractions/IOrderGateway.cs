using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Abstractions;

public interface IOrderGateway
{
    Task<string?> CreateOrderAsync(
        CheckoutRequest request,
        AlieBrecho.Domain.Orders.Cart cart,
        CancellationToken cancellationToken);
}
