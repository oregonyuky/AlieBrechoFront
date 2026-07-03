using AlieBrecho.Application.Abstractions;
using AlieBrecho.Application.Cart;
using AlieBrecho.Application.Catalog;
using AlieBrecho.Application.Checkout;
using AlieBrecho.Domain.Auth;
using AlieBrecho.Infrastructure;
using AlieBrecho.Presentation.Infrastructure;
using AlieBrecho.Presentation.Instagram;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using AppAuthenticationService = AlieBrecho.Application.Auth.AuthenticationService;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Error");
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var sessionToken = context.HttpContext.Session.GetString(AuthSessionKeys.AccessToken);
                var cookieToken = context.Principal?.FindFirst(AuthSessionKeys.AccessToken)?.Value;
                var accessToken = !string.IsNullOrWhiteSpace(sessionToken)
                    ? sessionToken
                    : cookieToken;

                if (context.Principal?.Identity?.IsAuthenticated == true &&
                    (string.IsNullOrWhiteSpace(accessToken) || JwtTokenReader.IsExpired(accessToken)))
                {
                    context.HttpContext.Session.Remove(AuthSessionKeys.AccessToken);
                    context.HttpContext.Session.Remove(AuthSessionKeys.RefreshToken);
                    context.HttpContext.Session.Remove(AuthSessionKeys.UserId);
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });
builder.Services.AddScoped<ICartStore, SessionCartStore>();
builder.Services.AddScoped<AppAuthenticationService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.Configure<InstagramOptions>(
    builder.Configuration.GetSection(InstagramOptions.SectionName));
builder.Services.AddHttpClient<InstagramFeedService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/api/instagram/latest", async (
    InstagramFeedService instagramFeedService,
    CancellationToken cancellationToken) =>
{
    var posts = await instagramFeedService.GetLatestPostsAsync(cancellationToken);
    return Results.Ok(posts);
}).AllowAnonymous();
app.MapGet("/api/drop-config/active", async (
    IDropConfigGateway dropConfigGateway,
    CancellationToken cancellationToken) =>
{
    var dropConfig = await dropConfigGateway.GetActiveDropConfigAsync(cancellationToken);
    return dropConfig is null ? Results.NotFound() : Results.Ok(dropConfig);
}).AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
