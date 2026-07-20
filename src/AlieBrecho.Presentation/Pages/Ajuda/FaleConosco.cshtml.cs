using System.ComponentModel.DataAnnotations;
using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlieBrecho.Presentation.Pages.Ajuda;

[AllowAnonymous]
public sealed class FaleConoscoModel(IContactMessageGateway contactMessageGateway) : PageModel
{
    [BindProperty]
    public ContactInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string? ErrorMessage { get; private set; }

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
            await contactMessageGateway.SendAsync(
                new ContactMessageRequest(
                    Input.Name.Trim(),
                    Input.Email.Trim(),
                    Input.Phone.Trim(),
                    Input.Subject.Trim(),
                    Input.Message.Trim()),
                cancellationToken);

            StatusMessage = "Mensagem enviada com sucesso. Em breve entraremos em contato.";
            return RedirectToPage();
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? "Não foi possível enviar sua mensagem. Tente novamente."
                : ex.Message;
            return Page();
        }
    }

    public sealed class ContactInput
    {
        [Required(ErrorMessage = "Informe seu nome.")]
        [StringLength(120, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
        [Display(Name = "Nome")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(180, ErrorMessage = "O e-mail deve ter no máximo {1} caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu telefone.")]
        [Phone(ErrorMessage = "Informe um telefone válido.")]
        [StringLength(30, ErrorMessage = "O telefone deve ter no máximo {1} caracteres.")]
        [Display(Name = "Telefone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o assunto.")]
        [StringLength(160, ErrorMessage = "O assunto deve ter no máximo {1} caracteres.")]
        [Display(Name = "Assunto")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escreva sua mensagem.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "A mensagem deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Mensagem")]
        public string Message { get; set; } = string.Empty;
    }
}
