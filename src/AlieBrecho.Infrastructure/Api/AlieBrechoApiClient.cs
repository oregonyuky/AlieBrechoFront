using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Domain.Bags;
using AlieBrecho.Domain.Contact;
using AlieBrecho.Domain.Marketing;
using AlieBrecho.Domain.Orders;
using AlieBrecho.Domain.Products;
using Microsoft.Extensions.Options;

namespace AlieBrecho.Infrastructure.Api;

internal sealed class AlieBrechoApiClient(HttpClient httpClient, IOptions<AlieBrechoApiOptions> options)
    : IProductCatalogGateway, IOrderGateway, IBagGateway, IAuthenticationGateway, ICustomerGateway, IDropConfigGateway, IContactMessageGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AlieBrechoApiOptions _options = options.Value;

    public async Task SendAsync(ContactMessageRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            _options.ContactMessagesPath,
            request,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<LoginSession?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var payload = new LoginPayload
        {
            Email = request.Email,
            Password = request.Password
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.LoginPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var login = await ReadWrappedAsync<LoginDto>(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(login.Data?.AccessToken))
        {
            return null;
        }

        return new LoginSession
        {
            AccessToken = login.Data.AccessToken,
            RefreshToken = login.Data.RefreshToken,
            UserId = login.Data.UserId,
            Email = login.Data.Email ?? request.Email,
            FirstName = login.Data.FirstName,
            LastName = login.Data.LastName,
            Roles = login.Data.Roles ?? []
        };
    }

    public async Task<RegisterResult?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new RegisterPayload
        {
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.RegisterPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var register = await ReadWrappedAsync<RegisterDto>(response, cancellationToken);
        if (register.Data is null)
        {
            return null;
        }

        return new RegisterResult
        {
            UserId = register.Data.UserId,
            Email = string.IsNullOrWhiteSpace(register.Data.Email) ? request.Email : register.Data.Email,
            FirstName = register.Data.FirstName,
            LastName = register.Data.LastName,
            CompanyName = register.Data.CompanyName
        };
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var response = await GetCatalogListAsync<ProductDto>(
            _options.ProductsPath,
            [_options.PublicProductsPath, "products"],
            cancellationToken);

        return response.Select(MapProduct).ToList();
    }

    public async Task CreateCustomerForRegistrationAsync(
        string firstName,
        string lastName,
        string? instagram,
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        var payload = new CreateCustomerPayload
        {
            Name = GetFullName(firstName, lastName),
            Instagram = instagram,
            EmailAddress = email,
            Password = password,
            ConfirmPassword = confirmPassword,
            Country = "Brasil",
            CustomerStatus = "Active"
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.CreateCustomerPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<CustomerProfile?> GetCustomerProfileAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        var customer = await GetCustomerAsync(customerId, cancellationToken);
        return customer is null ? null : MapCustomerProfile(customer);
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken)
    {
        var detailPath = _options.ProductDetailPathTemplate.Replace("{id}", Uri.EscapeDataString(productId));

        try
        {
            var product = await GetWrappedAsync<ProductDto>(detailPath, cancellationToken);
            return product is null ? null : MapProduct(product);
        }
        catch (HttpRequestException)
        {
            var products = await GetProductsAsync(cancellationToken);
            return products.FirstOrDefault(product => product.Id == productId);
        }
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await GetCatalogListAsync<CategoryDto>(
                _options.CategoriesPath,
                [_options.PublicCategoriesPath, "categories"],
                cancellationToken);

            return response.Select(category => new Category(
                category.Id,
                category.Name ?? string.Empty,
                category.Description,
                category.IsActive)).ToList();
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<DropConfig?> GetActiveDropConfigAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(_options.DropConfigActivePath, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<DropConfigDto>(response, cancellationToken);
        return result.Data is null ? null : MapDropConfig(result.Data);
    }

    public async Task<string?> CreateOrderAsync(
        CheckoutRequest request,
        Cart cart,
        CancellationToken cancellationToken)
    {
        var customerId = await SaveCustomerFromCheckoutAsync(request, cancellationToken);
        var payload = new CreateOrderPayload
        {
            CustomerId = customerId,
            Status = "Pending",
            Notes = request.Notes,
            ShippingDetail = new ShippingDetailPayload
            {
                FirstName = GetFirstName(request.CustomerName),
                LastName = GetLastName(request.CustomerName),
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Street = request.Street,
                Number = request.Number,
                Complement = request.Complement,
                Neighborhood = request.Neighborhood,
                City = request.City,
                State = request.State,
                PostCode = request.PostCode
            },
            OrderDetails = cart.Items.Select(item => new OrderDetailPayload
            {
                ProductId = item.Product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.Product.UnitPrice
            }).ToList()
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.OrdersPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<OrderDto>(response, cancellationToken);
        return result?.Data?.Id;
    }

    public async Task<PaymentCheckoutResult> CreateMercadoPagoPixPaymentAsync(
        string orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new MercadoPagoPixPaymentPayload
        {
            OrderId = orderId,
            Description = $"Pedido {orderId}",
            PayerFirstName = request.FirstName,
            PayerLastName = request.LastName,
            PayerEmail = request.Email,
            PayerCpf = request.Cpf
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.MercadoPagoPixPaymentPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = ReadMercadoPagoPixPayment(content);
        var qrImage = string.IsNullOrWhiteSpace(result.QrCodeBase64)
            ? null
            : $"data:image/png;base64,{result.QrCodeBase64}";

        if (string.IsNullOrWhiteSpace(qrImage) && string.IsNullOrWhiteSpace(result.QrCode))
        {
            var details = string.Join(
                " ",
                new[]
                {
                    string.IsNullOrWhiteSpace(result.PaymentId) ? null : $"Pagamento: {result.PaymentId}.",
                    string.IsNullOrWhiteSpace(result.Status) ? null : $"Status: {result.Status}."
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            throw new HttpRequestException(string.IsNullOrWhiteSpace(details)
                ? "Mercado Pago nao retornou QR Code nem codigo Pix."
                : $"Mercado Pago nao retornou QR Code nem codigo Pix. {details}");
        }

        return new PaymentCheckoutResult(
            null,
            qrImage,
            result.QrCode,
            result.PaymentId);
    }

    public async Task<BagCheckoutResult?> CreateBagCheckoutAsync(
        CheckoutRequest request,
        Cart cart,
        CancellationToken cancellationToken)
    {
        var customerId = await SaveCustomerFromCheckoutAsync(request, cancellationToken);
        var payload = new BagCheckoutPayload
        {
            CustomerId = customerId,
            Notes = request.Notes,
            PayerCpf = request.Cpf,
            ShippingDetail = new ShippingDetailPayload
            {
                FirstName = GetFirstName(request.CustomerName),
                LastName = GetLastName(request.CustomerName),
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Street = request.Street,
                Number = request.Number,
                Complement = request.Complement,
                Neighborhood = request.Neighborhood,
                City = request.City,
                State = request.State,
                PostCode = request.PostCode
            },
            Items = cart.Items.Select(item => new BagCheckoutItemPayload
            {
                ProductId = item.Product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.Product.UnitPrice
            }).ToList()
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.BagCheckoutPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<BagCheckoutDto>(response, cancellationToken);
        if (result.Data is null)
        {
            return null;
        }

        return new BagCheckoutResult(
            result.Data.BagId ?? result.Data.Id,
            result.Data.PaymentUrl,
            BuildQrImage(result.Data.PixQrCodeBase64) ?? result.Data.PixQrCode,
            result.Data.PixCode,
            result.Data.PaymentId);
    }

    public async Task<BagSummary?> GetActiveBagAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        var path = _options.ActiveBagPathTemplate.Replace(
            "{customerId}",
            Uri.EscapeDataString(customerId));

        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var result = await ReadWrappedAsync<BagSummaryDto>(response, cancellationToken);
            if (result.Data is null)
            {
                return null;
            }

            var summary = MapBagSummary(result.Data);
            if (summary.Items.Count > 0 || string.IsNullOrWhiteSpace(summary.Id))
            {
                return summary;
            }

            var detailPath = _options.BagDetailPathTemplate.Replace(
                "{id}",
                Uri.EscapeDataString(summary.Id));
            var detail = await GetWrappedAsync<BagSummaryDto>(detailPath, cancellationToken);
            return MapBagSummary(detail);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<BagFinalizeResult?> FinalizeBagAsync(
        string bagId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bagId))
        {
            return null;
        }

        using var response = await httpClient.PostAsJsonAsync(
            _options.FinalizeBagPath,
            new FinalizeBagPayload { BagId = bagId },
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<BagFinalizeDto>(response, cancellationToken);
        return result.Data is null
            ? null
            : new BagFinalizeResult(
                result.Data.BagId ?? result.Data.Id,
                result.Data.Status,
                result.Data.ShippingCost,
                result.Data.TotalAmount);
    }

    public async Task<PixPaymentStatusResult?> GetMercadoPagoPixPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var path = _options.MercadoPagoPixStatusPathTemplate
            .Replace("{paymentId}", Uri.EscapeDataString(paymentId));

        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<MercadoPagoPixStatusDto>(response, cancellationToken);
        return result.Data is null
            ? null
            : new PixPaymentStatusResult(
                result.Data.PaymentId,
                result.Data.Status,
                result.Data.StatusDetail,
                result.Data.OrderStatus);
    }

    public async Task<OrderSummary?> GetOrderSummaryAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        var path = _options.OrderDetailPathTemplate.Replace("{id}", Uri.EscapeDataString(orderId));

        var order = await GetWrappedAsync<OrderSummaryDto>(path, cancellationToken);
        return MapOrderSummary(order);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersByCustomerAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return [];
        }

        var path = _options.OrderListPathTemplate.Replace(
            "{customerId}",
            Uri.EscapeDataString(customerId));
        var orders = await GetWrappedAsync<List<OrderSummaryDto>>(path, cancellationToken);

        return orders.Select(MapOrderSummary).ToArray();
    }

    public async Task<BagSummary?> GetBagAsync(
        string bagId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bagId))
        {
            return null;
        }

        var path = _options.BagDetailPathTemplate.Replace(
            "{id}",
            Uri.EscapeDataString(bagId));

        try
        {
            var bag = await GetWrappedAsync<BagSummaryDto>(path, cancellationToken);
            return bag is null ? null : MapBagSummary(bag);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<BagItemSummary>> GetOrderItemsAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return [];
        }

        var path = _options.OrderDetailPathTemplate.Replace("{id}", Uri.EscapeDataString(orderId));
        var order = await GetWrappedAsync<OrderSummaryDto>(path, cancellationToken);
        return MapBagItems(order.OrderDetails.Count > 0 ? order.OrderDetails : order.Items);
    }

    private async Task<string?> SaveCustomerFromCheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            await UpdateCustomerAsync(request, request.CustomerId, cancellationToken);
            return request.CustomerId;
        }

        var customer = await CreateCustomerAsync(request, cancellationToken);
        return customer.Id;
    }

    private async Task<CustomerDto> CreateCustomerAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var payload = BuildCustomerPayload(request, existingCustomer: null);

        using var response = await httpClient.PostAsJsonAsync(
            _options.CreateCustomerPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var customer = await ReadWrappedAsync<CustomerDto>(response, cancellationToken);
        return customer?.Data ?? throw new InvalidOperationException("A API nao retornou o cliente criado.");
    }

    private async Task UpdateCustomerAsync(
        CheckoutRequest request,
        string customerId,
        CancellationToken cancellationToken)
    {
        var existingCustomer = await GetCustomerAsync(customerId, cancellationToken);
        var payload = BuildCustomerPayload(request, existingCustomer) with
        {
            Id = customerId
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.UpdateCustomerPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<CustomerDto?> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken)
    {
        var detailPath = _options.CustomerDetailPathTemplate.Replace("{id}", Uri.EscapeDataString(customerId));

        try
        {
            var customer = await GetWrappedAsync<CustomerDto>(detailPath, cancellationToken);
            return customer;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static CreateCustomerPayload BuildCustomerPayload(
        CheckoutRequest request,
        CustomerDto? existingCustomer)
    {
        return new CreateCustomerPayload
        {
            Name = request.CustomerName,
            Description = request.Notes,
            Cpf = request.Cpf,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.Email,
            Street = request.Street,
            Number = request.Number,
            Neighborhood = request.Neighborhood,
            Complement = request.Complement,
            City = request.City,
            State = request.State,
            PostalCode = request.PostCode,
            Country = "Brasil",
            Website = existingCustomer?.Website,
            Instagram = existingCustomer?.Instagram,
            TwitterX = existingCustomer?.TwitterX,
            TikTok = existingCustomer?.TikTok,
            CustomerStatus = string.IsNullOrWhiteSpace(existingCustomer?.CustomerStatus)
                ? "Active"
                : existingCustomer.CustomerStatus
        };
    }

    private static CustomerProfile MapCustomerProfile(CustomerDto customer)
    {
        var (firstName, lastName) = SplitCustomerName(customer);

        return new CustomerProfile
        {
            Id = customer.Id,
            Name = customer.Name,
            FirstName = string.IsNullOrWhiteSpace(customer.FirstName) ? firstName : customer.FirstName,
            LastName = string.IsNullOrWhiteSpace(customer.LastName) ? lastName : customer.LastName,
            Cpf = customer.Cpf,
            PhoneNumber = customer.PhoneNumber,
            EmailAddress = customer.EmailAddress,
            Street = customer.Street,
            Number = customer.Number,
            Neighborhood = customer.Neighborhood,
            Complement = customer.Complement,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode
        };
    }

    private static (string? FirstName, string? LastName) SplitCustomerName(CustomerDto customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.FirstName) || !string.IsNullOrWhiteSpace(customer.LastName))
        {
            return (customer.FirstName, customer.LastName);
        }

        var parts = (customer.Name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return (null, null);
        }

        return (parts[0], parts.Length == 1 ? null : string.Join(' ', parts.Skip(1)));
    }

    private async Task<T> GetWrappedAsync<T>(
        string path,
        CancellationToken cancellationToken,
        bool skipAuthorization = false)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (skipAuthorization)
        {
            request.Options.Set(AlieBrechoApiAuthorizationHandler.SkipAuthorizationOption, true);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<T>(response, cancellationToken);
        return result?.Data ?? throw new InvalidOperationException($"Resposta vazia da API em '{path}'.");
    }

    private static async Task<ApiResponse<T>> ReadWrappedAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiSuccess = JsonSerializer.Deserialize<ApiSuccessResponse<ApiResponse<T>>>(content, JsonOptions);
        if (apiSuccess?.Content?.Data is not null)
        {
            return apiSuccess.Content;
        }

        var legacySuccess = JsonSerializer.Deserialize<ApiSuccessResponse<T>>(content, JsonOptions);
        if (legacySuccess?.Content is not null)
        {
            return new ApiResponse<T>(legacySuccess.Content);
        }

        var wrapped = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
        if (wrapped?.Data is not null)
        {
            return wrapped;
        }

        var direct = JsonSerializer.Deserialize<T>(content, JsonOptions);
        return direct is null
            ? new ApiResponse<T>(default)
            : new ApiResponse<T>(direct);
    }

    private static MercadoPagoPixPaymentDto ReadMercadoPagoPixPayment(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new MercadoPagoPixPaymentDto();
        }

        using var document = JsonDocument.Parse(content);
        var apiError = FindMessage(document.RootElement);
        if (!string.IsNullOrWhiteSpace(apiError) &&
            (document.RootElement.TryGetProperty("error", out _) ||
                document.RootElement.TryGetProperty("Error", out _) ||
                document.RootElement.TryGetProperty("code", out _) ||
                document.RootElement.TryGetProperty("Code", out _)))
        {
            throw new HttpRequestException(apiError);
        }

        var payload = UnwrapPayload(document.RootElement);
        var transactionData = GetObject(payload, "pointOfInteraction", "transactionData")
            ?? GetObject(payload, "point_of_interaction", "transaction_data");

        return new MercadoPagoPixPaymentDto
        {
            PaymentId = GetString(payload, "paymentId", "id"),
            QrCodeBase64 = GetString(payload, "qrCodeBase64", "qr_code_base64")
                ?? (transactionData is null ? null : GetString(transactionData.Value, "qrCodeBase64", "qr_code_base64")),
            QrCode = GetString(payload, "qrCode", "qr_code")
                ?? (transactionData is null ? null : GetString(transactionData.Value, "qrCode", "qr_code")),
            Status = GetString(payload, "status")
        };
    }

    private static JsonElement UnwrapPayload(JsonElement element)
    {
        var current = element;
        while (current.ValueKind == JsonValueKind.Object)
        {
            if (current.TryGetProperty("content", out var content) ||
                current.TryGetProperty("Content", out content) ||
                current.TryGetProperty("data", out content) ||
                current.TryGetProperty("Data", out content))
            {
                current = content;
                continue;
            }

            break;
        }

        return current;
    }

    private static JsonElement? GetObject(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var propertyName in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(propertyName, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.Object ? current : null;
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                var value = property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<T>> GetCatalogListAsync<T>(
        string primaryPath,
        IReadOnlyList<string> publicPaths,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await GetWrappedAsync<List<T>>(primaryPath, cancellationToken);
        }
        catch (HttpRequestException primaryException)
        {
            foreach (var publicPath in GetDistinctPublicPaths(primaryPath, publicPaths))
            {
                try
                {
                    return await GetWrappedAsync<List<T>>(publicPath, cancellationToken, skipAuthorization: true);
                }
                catch (HttpRequestException)
                {
                }
            }

            ExceptionDispatchInfo.Capture(primaryException).Throw();
            throw;
        }
    }

    private static IReadOnlyList<string> GetDistinctPublicPaths(
        string primaryPath,
        IReadOnlyList<string> publicPaths)
    {
        return publicPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => !string.Equals(primaryPath, path, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = GetApiErrorMessage(content);
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"A API retornou erro {(int)response.StatusCode} ({response.ReasonPhrase}).";
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static string? GetApiErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return FindMessage(document.RootElement);
        }
        catch (JsonException)
        {
            return content.Length > 300 ? content[..300] : content;
        }
    }

    private static string? FindMessage(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "message", "Message", "detail", "Detail", "title", "Title" })
            {
                if (element.TryGetProperty(propertyName, out var property) &&
                    property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }

            foreach (var propertyName in new[] { "error", "Error", "errors", "Errors", "content", "Content" })
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    var nestedMessage = FindMessage(property);
                    if (!string.IsNullOrWhiteSpace(nestedMessage))
                    {
                        return nestedMessage;
                    }
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var messages = element.EnumerateArray()
                .Select(FindMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message));

            return string.Join(" ", messages);
        }

        return null;
    }

    private Product MapProduct(ProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            CategoryId = dto.CategoryId,
            UnitPrice = dto.UnitPrice,
            OldPrice = dto.OldPrice,
            DiscountPercent = dto.DiscountPercent,
            ProductAvailable = dto.ProductAvailable ?? true,
            MainImageUrl = ResolveImageUrl(dto.MainImageUrl),
            AltText = dto.AltText,
            ShortDescription = dto.ShortDescription,
            LongDescription = dto.LongDescription,
            Sizes = dto.Sizes?.Select(size => new ProductSize(
                size.Id,
                size.Size,
                size.Bust,
                size.Sleeve,
                size.Length)).ToList() ?? []
        };
    }

    private static DropConfig MapDropConfig(DropConfigDto dto)
    {
        return new DropConfig
        {
            Id = dto.Id,
            Titulo = dto.Titulo ?? string.Empty,
            Subtitulo = dto.Subtitulo,
            DataLiberacao = dto.DataLiberacaoUtc,
            Ativo = dto.Ativo,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    private BagSummary MapBagSummary(BagSummaryDto dto)
    {
        var items = dto.Items.Count > 0
            ? dto.Items
            : dto.BagItems.Count > 0
                ? dto.BagItems
                : dto.OrderDetails;

        var mappedItems = MapBagItems(items)
            .Where(item => item.IsPaid)
            .ToArray();

        return new BagSummary
        {
            Id = dto.Id,
            Status = dto.Status,
            ExpirationDate = dto.ExpirationDate,
            TotalItemsValue = mappedItems.Sum(item => item.Total),
            ShippingCost = dto.ShippingCost,
            ItemCount = mappedItems.Sum(item => item.Quantity),
            Items = mappedItems
        };
    }

    private IReadOnlyList<BagItemSummary> MapBagItems(IEnumerable<BagItemSummaryDto> items)
    {
        return items.Select(item => new BagItemSummary
        {
            ProductId = item.ProductId ?? item.Product?.Id,
            Name = item.Name ?? item.ProductName ?? item.Product?.Name ?? "Produto",
            ImageUrl = ResolveImageUrl(item.ProductImageUrl ?? item.ImageUrl ?? item.MainImageUrl ?? item.Product?.MainImageUrl),
            Quantity = item.Quantity > 0 ? item.Quantity : 1,
            UnitPrice = item.UnitPrice ?? item.Price ?? item.Product?.UnitPrice ?? 0m,
            IsPaid = item.IsPaid
        }).ToArray();
    }

    private OrderSummary MapOrderSummary(OrderSummaryDto order)
    {
        var items = order.Items.Count > 0
            ? order.Items
            : order.OrderDetails;

        return new OrderSummary(
            order.Id,
            order.Status,
            order.TotalAmount,
            order.ShippingCost,
            order.Payment?.Amount,
            string.IsNullOrWhiteSpace(order.Payment?.PaymentDetail?.PaymentMethod)
                ? "Pix"
                : order.Payment.PaymentDetail.PaymentMethod,
            MapBagItems(items));
    }

    private static string? BuildQrImage(string? qrCodeBase64)
    {
        return string.IsNullOrWhiteSpace(qrCodeBase64)
            ? null
            : $"data:image/png;base64,{qrCodeBase64}";
    }

    private string? ResolveImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var normalizedImageUrl = imageUrl.Replace('\\', '/').Trim();
        var imageName = Path.GetFileName(normalizedImageUrl);
        if (string.IsNullOrWhiteSpace(imageName) || string.IsNullOrWhiteSpace(Path.GetExtension(imageName)))
        {
            return new Uri(_options.BaseUrl, normalizedImageUrl.TrimStart('/')).ToString();
        }

        var escapedImageName = Uri.EscapeDataString(imageName);
        return new Uri(_options.BaseUrl, $"api/FileImage/GetImage?imageName={escapedImageName}").ToString();
    }

    private static string GetFirstName(string fullName)
    {
        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;
    }

    private static string GetLastName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1 ? string.Empty : string.Join(' ', parts.Skip(1));
    }

    private static string GetFullName(string firstName, string lastName)
    {
        return string.Join(' ', new[] { firstName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim()));
    }

    private sealed record ApiResponse<T>(T? Data)
        where T : class;

    private sealed record ApiSuccessResponse<T>(T? Content)
        where T : class;

    private sealed record LoginPayload
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
    }

    private sealed record LoginDto
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public string? UserId { get; init; }
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public List<string>? Roles { get; init; }
    }

    private sealed record RegisterPayload
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
        public string? ConfirmPassword { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? CompanyName { get; init; }
    }

    private sealed record RegisterDto
    {
        public string? UserId { get; init; }
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? CompanyName { get; init; }
    }

    private sealed record ProductDto
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? CategoryId { get; init; }
        public decimal? UnitPrice { get; init; }
        public decimal? OldPrice { get; init; }
        public decimal? DiscountPercent { get; init; }
        public bool? ProductAvailable { get; init; }
        public string? MainImageUrl { get; init; }
        public string? AltText { get; init; }
        public string? ShortDescription { get; init; }
        public string? LongDescription { get; init; }
        public List<ProductSizeDto>? Sizes { get; init; }
    }

    private sealed record ProductSizeDto
    {
        public string? Id { get; init; }
        public string? Size { get; init; }
        public decimal? Bust { get; init; }
        public decimal? Sleeve { get; init; }
        public decimal? Length { get; init; }
    }

    private sealed record CategoryDto
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }

    private sealed record DropConfigDto
    {
        public string? Id { get; init; }
        public string? Titulo { get; init; }
        public string? Subtitulo { get; init; }
        public DateTime DataLiberacaoUtc { get; init; }
        public bool Ativo { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }

    private sealed record OrderDto
    {
        public string? Id { get; init; }
    }

    private sealed record BagCheckoutPayload
    {
        public string? CustomerId { get; init; }
        public string? Notes { get; init; }
        public string? PayerCpf { get; init; }
        public ShippingDetailPayload? ShippingDetail { get; init; }
        public List<BagCheckoutItemPayload> Items { get; init; } = [];
    }

    private sealed record BagCheckoutItemPayload
    {
        public string? ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
    }

    private sealed record BagCheckoutDto
    {
        public string? Id { get; init; }
        public string? BagId { get; init; }
        public string? PaymentUrl { get; init; }
        public string? PixQrCodeBase64 { get; init; }
        public string? PixQrCode { get; init; }
        public string? PixCode { get; init; }
        public string? PaymentId { get; init; }
    }

    private sealed record BagSummaryDto
    {
        public string? Id { get; init; }
        public string? Status { get; init; }
        public DateTime? ExpirationDate { get; init; }
        public decimal TotalItemsValue { get; init; }
        public decimal? ShippingCost { get; init; }
        public int ItemCount { get; init; }
        public List<BagItemSummaryDto> Items { get; init; } = [];
        public List<BagItemSummaryDto> BagItems { get; init; } = [];
        public List<BagItemSummaryDto> OrderDetails { get; init; } = [];
    }

    private sealed record BagItemSummaryDto
    {
        public string? ProductId { get; init; }
        public string? Name { get; init; }
        public string? ProductName { get; init; }
        public string? ImageUrl { get; init; }
        public string? ProductImageUrl { get; init; }
        public string? MainImageUrl { get; init; }
        public int Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
        public decimal? Price { get; init; }
        public bool IsPaid { get; init; }
        public ProductDto? Product { get; init; }
    }

    private sealed record FinalizeBagPayload
    {
        public string? BagId { get; init; }
    }

    private sealed record BagFinalizeDto
    {
        public string? Id { get; init; }
        public string? BagId { get; init; }
        public string? Status { get; init; }
        public decimal? ShippingCost { get; init; }
        public decimal? TotalAmount { get; init; }
    }

    private sealed record MercadoPagoPixPaymentPayload
    {
        public string? OrderId { get; init; }
        public string? Description { get; init; }
        public string? PayerFirstName { get; init; }
        public string? PayerLastName { get; init; }
        public string? PayerEmail { get; init; }
        public string? PayerCpf { get; init; }
    }

    private sealed record MercadoPagoPixPaymentDto
    {
        public string? PaymentId { get; init; }
        public string? QrCodeBase64 { get; init; }
        public string? QrCode { get; init; }
        public string? Status { get; init; }
    }

    private sealed record MercadoPagoPixStatusDto
    {
        public string? PaymentId { get; init; }
        public string? Status { get; init; }
        public string? StatusDetail { get; init; }
        public string? OrderStatus { get; init; }
    }

    private sealed record OrderSummaryDto
    {
        public string? Id { get; init; }
        public string? Status { get; init; }
        public decimal? TotalAmount { get; init; }
        public decimal? ShippingCost { get; init; }
        public OrderPaymentSummaryDto? Payment { get; init; }
        public List<BagItemSummaryDto> OrderDetails { get; init; } = [];
        public List<BagItemSummaryDto> Items { get; init; } = [];
    }

    private sealed record OrderPaymentSummaryDto
    {
        public decimal? Amount { get; init; }
        public OrderPaymentDetailSummaryDto? PaymentDetail { get; init; }
    }

    private sealed record OrderPaymentDetailSummaryDto
    {
        public string? PaymentMethod { get; init; }
    }

    private sealed record CustomerDto
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Cpf { get; init; }
        public string? PhoneNumber { get; init; }
        public string? EmailAddress { get; init; }
        public string? Street { get; init; }
        public string? Number { get; init; }
        public string? Neighborhood { get; init; }
        public string? Complement { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? Website { get; init; }
        public string? Instagram { get; init; }
        public string? TwitterX { get; init; }
        public string? TikTok { get; init; }
        public string? CustomerStatus { get; init; }
    }

    private sealed record CreateCustomerPayload
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Cpf { get; init; }
        public string? PhoneNumber { get; init; }
        public string? EmailAddress { get; init; }
        public string? Password { get; init; }
        public string? ConfirmPassword { get; init; }
        public string? Street { get; init; }
        public string? Number { get; init; }
        public string? Neighborhood { get; init; }
        public string? Complement { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public string? Website { get; init; }
        public string? Instagram { get; init; }
        public string? TwitterX { get; init; }
        public string? TikTok { get; init; }
        public string? CustomerStatus { get; init; }
    }

    private sealed record CreateOrderPayload
    {
        public string? CustomerId { get; init; }
        public string? Status { get; init; }
        public string? Notes { get; init; }
        public ShippingDetailPayload? ShippingDetail { get; init; }
        public List<OrderDetailPayload> OrderDetails { get; init; } = [];
    }

    private sealed record ShippingDetailPayload
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Street { get; init; }
        public string? Number { get; init; }
        public string? Neighborhood { get; init; }
        public string? Complement { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostCode { get; init; }
    }

    private sealed record OrderDetailPayload
    {
        public string? ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal? UnitPrice { get; init; }
    }
}
