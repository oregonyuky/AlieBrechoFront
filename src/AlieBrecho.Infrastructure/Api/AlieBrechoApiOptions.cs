namespace AlieBrecho.Infrastructure.Api;

public sealed class AlieBrechoApiOptions
{
    public const string SectionName = "AlieBrechoApi";

    public Uri BaseUrl { get; init; } = new("http://localhost:5000/");
    public string ProductsPath { get; init; } = "api/Product/GetProductList";
    public string ProductDetailPathTemplate { get; init; } = "api/Product/GetProductSingle?id={id}";
    public string CategoriesPath { get; init; } = "api/Category/GetCategoryList";
    public string CreateCustomerPath { get; init; } = "api/Customer/CreateCustomer";
    public string OrdersPath { get; init; } = "api/Order/CreateOrder";
    public string LoginPath { get; init; } = "api/Security/Login";
    public string RegisterPath { get; init; } = "api/Security/Register";
}
