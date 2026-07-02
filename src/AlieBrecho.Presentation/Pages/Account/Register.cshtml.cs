using System.ComponentModel.DataAnnotations;
using AlieBrecho.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Account;

[AllowAnonymous]
public class RegisterModel(ICustomerGateway customerGateway) : PageModel
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await customerGateway.CreateCustomerForRegistrationAsync(
                Input.FirstName,
                Input.LastName,
                Input.Email,
                Input.Password,
                Input.ConfirmPassword,
                cancellationToken);

            StatusMessage = "Cadastro realizado com sucesso.";
            return RedirectToPage("/Account/Register");
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Nao foi possivel cadastrar o cliente. Confira a conexao e tente novamente."
                : ex.Message;
            return Page();
        }
    }

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "Informe o nome.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o sobrenome.")]
        public string LastName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos {2} caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [Compare(nameof(Password), ErrorMessage = "As senhas nao conferem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
