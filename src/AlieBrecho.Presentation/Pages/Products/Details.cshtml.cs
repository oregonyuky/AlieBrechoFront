using AlieBrecho.Application.Cart;
using AlieBrecho.Application.Catalog;
using AlieBrecho.Domain.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Products;

public class DetailsModel(CatalogService catalogService, CartService cartService) : PageModel
{
    public Product? Product { get; private set; }
    public IReadOnlyList<Product> RelatedProducts { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        Product = await catalogService.GetProductAsync(id, cancellationToken);
        if (Product is null)
        {
            return NotFound();
        }

        var catalog = await catalogService.GetCatalogAsync(Product.CategoryId, cancellationToken);
        RelatedProducts = catalog.Products
            .Where(product => product.Id != Product.Id)
            .Take(4)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string id, CancellationToken cancellationToken)
    {
        await cartService.AddAsync(id, cancellationToken);
        return RedirectToPage("/Cart/Index");
    }
}
