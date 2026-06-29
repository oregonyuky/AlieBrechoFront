using AlieBrecho.Application.Cart;
using AlieBrecho.Application.Catalog;
using AlieBrecho.Domain.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Products;

public class DetailsModel(CatalogService catalogService, CartService cartService) : PageModel
{
    public Product? Product { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        Product = await catalogService.GetProductAsync(id, cancellationToken);
        return Product is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string id, int quantity, CancellationToken cancellationToken)
    {
        await cartService.AddAsync(id, quantity, cancellationToken);
        return RedirectToPage("/Cart/Index");
    }
}
