using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Domain.Marketing;
using AlieBrecho.Domain.Orders;
using AlieBrecho.Domain.Products;
using Microsoft.Extensions.Options;

namespace AlieBrecho.Infrastructure.Api;

internal sealed class AlieBrechoApiClient(HttpClient httpClient, IOptions<AlieBrechoApiOptions> options)
    : IProductCatalogGateway, IOrderGateway, IAuthenticationGateway, ICustomerGateway, IDropConfigGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AlieBrechoApiOptions _options = options.Value;

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
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        var payload = new CreateCustomerPayload
        {
            Name = GetFullName(firstName, lastName),
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

    public async Task<PaymentCheckoutResult> CreateInfinitePayCheckoutAsync(
        string orderId,
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        var payload = new InfinitePayCheckoutPayload
        {
            OrderId = orderId,
            PaymentMethod = paymentMethod
        };

        using var response = await httpClient.PostAsJsonAsync(
            _options.InfinitePayCheckoutPath,
            payload,
            JsonOptions,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var result = await ReadWrappedAsync<InfinitePayCheckoutDto>(response, cancellationToken);
        return new PaymentCheckoutResult(
            result?.Data?.PaymentUrl,
            result?.Data?.PixQrCode,
            result?.Data?.PixCode);
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

    private sealed record InfinitePayCheckoutPayload
    {
        public string? OrderId { get; init; }
        public string? PaymentMethod { get; init; }
    }

    private sealed record InfinitePayCheckoutDto
    {
        public string? PaymentUrl { get; init; }
        public string? PixQrCode { get; init; }
        public string? PixCode { get; init; }
    }

    private sealed record CustomerDto
    {
        public string? Id { get; init; }
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
