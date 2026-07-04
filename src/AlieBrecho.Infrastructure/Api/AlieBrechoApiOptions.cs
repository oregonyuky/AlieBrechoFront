namespace AlieBrecho.Infrastructure.Api;

public sealed class AlieBrechoApiOptions
{
    public const string SectionName = "AlieBrechoApi";

    public Uri BaseUrl { get; init; } = new("http://localhost:5000/");
    public string ProductsPath { get; init; } = "api/Product/GetProductList";
    public string PublicProductsPath { get; init; } = "api/products";
    public string ProductDetailPathTemplate { get; init; } = "api/Product/GetProductSingle?id={id}";
    public string CategoriesPath { get; init; } = "api/Category/GetCategoryList";
    public string PublicCategoriesPath { get; init; } = "api/categories";
    public string CreateCustomerPath { get; init; } = "api/Customer/CreateCustomer";
    public string UpdateCustomerPath { get; init; } = "api/Customer/UpdateCustomer";
    public string CustomerDetailPathTemplate { get; init; } = "api/Customer/GetCustomerSingle?id={id}";
    public string OrdersPath { get; init; } = "api/Order/CreateOrder";
    public string LoginPath { get; init; } = "api/Security/Login";
    public string RegisterPath { get; init; } = "api/Security/Register";
    public string DropConfigActivePath { get; init; } = "api/drop-config/active";
}
