namespace AlieBrecho.Application.Checkout;

public sealed record CheckoutResult(bool Success, string? OrderId, string? ErrorMessage)
{
    public static CheckoutResult Succeeded(string? orderId)
    {
        return new CheckoutResult(true, orderId, null);
    }

    public static CheckoutResult Failed(string message)
    {
        return new CheckoutResult(false, null, message);
    }
}
