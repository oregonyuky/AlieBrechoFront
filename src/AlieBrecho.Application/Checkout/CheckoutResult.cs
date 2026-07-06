namespace AlieBrecho.Application.Checkout;

public sealed record CheckoutResult(
    bool Success,
    string? OrderId,
    string? PaymentUrl,
    string? PixQrCode,
    string? PixCode,
    string? PaymentId,
    string? ErrorMessage)
{
    public static CheckoutResult Succeeded(
        string? orderId,
        string? paymentUrl,
        string? pixQrCode,
        string? pixCode,
        string? paymentId)
    {
        return new CheckoutResult(true, orderId, paymentUrl, pixQrCode, pixCode, paymentId, null);
    }

    public static CheckoutResult Failed(string message)
    {
        return new CheckoutResult(false, null, null, null, null, null, message);
    }
}
