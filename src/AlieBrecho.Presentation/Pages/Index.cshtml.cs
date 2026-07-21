using AlieBrecho.Application.Catalog;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages;

public class IndexModel(CatalogService catalogService) : PageModel
{
    public CatalogView Catalog { get; private set; } = new([], [], null);
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(string? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var catalog = await catalogService.GetCatalogAsync(null, cancellationToken);
            var selectedCategoryId = catalog.Categories.Any(category =>
                category.IsActive && category.Id == categoryId)
                ? categoryId
                : null;

            Catalog = catalog with { SelectedCategoryId = selectedCategoryId };
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Nao foi possivel carregar os produtos da API. Verifique a URL em appsettings.json.";
        }
    }
}
