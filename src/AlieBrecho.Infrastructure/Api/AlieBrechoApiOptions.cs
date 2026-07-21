namespace AlieBrecho.Infrastructure.Api;

public sealed class AlieBrechoApiOptions
{
    public const string SectionName = "AlieBrechoApi";

    public Uri BaseUrl { get; init; } = new("http://localhost:5000/");
    public string PublicProductsPath { get; init; } = "api/Product/GetProductList";
    public string ProductDetailPathTemplate { get; init; } = "api/Product/GetProductSingle?id={id}";
    public string PublicCategoriesPath { get; init; } = "api/Category/GetCategoryList";
    public TimeSpan CategoriesCacheDuration { get; init; } = TimeSpan.FromMinutes(10);
    public string CreateCustomerPath { get; init; } = "api/Customer/CreateCustomer";
    public string UpdateCustomerPath { get; init; } = "api/Customer/UpdateCustomer";
    public string CustomerDetailPathTemplate { get; init; } = "api/Customer/GetCustomerSingle?id={id}";
    public string OrdersPath { get; init; } = "api/Order/CreateOrder";
    public string OrderListPathTemplate { get; init; } = "api/Order/GetOrderList?customerId={customerId}";
    public string OrderDetailPathTemplate { get; init; } = "api/Order/GetOrderSingle?id={id}";
    public string BagCheckoutPath { get; init; } = "api/Bag/CheckoutBag";
    public string ActiveBagPathTemplate { get; init; } = "api/Bag/GetActiveBag?customerId={customerId}";
    public string BagPurchaseHistoryPathTemplate { get; init; } = "api/Bag/GetPurchaseHistory?customerId={customerId}";
    public string BagDetailPathTemplate { get; init; } = "api/Bag/GetBagSingle?id={id}";
    public string FinalizeBagPath { get; init; } = "api/Bag/FinalizeBag";
    public string MercadoPagoPixPaymentPath { get; init; } = "api/pix/criar-pagamento";
    public string MercadoPagoPixStatusPathTemplate { get; init; } = "api/pix/status/{paymentId}";
    public string LoginPath { get; init; } = "api/Security/Login";
    public string RegisterPath { get; init; } = "api/Security/Register";
    public string DropConfigActivePath { get; init; } = "api/drop-config/active";
    public string ContactMessagesPath { get; init; } = "api/contact-messages";
    public string SiteSettingsPath { get; init; } = "api/SiteSettings";
}
