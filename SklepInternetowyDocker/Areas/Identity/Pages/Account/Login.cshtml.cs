using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public string ReturnUrl { get; private set; } = "/";
    public IList<AuthenticationScheme> ExternalLogins { get; private set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Podaj adres e-mail.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        [Display(Name = "Adres e-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Zapamiętaj mnie")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, "Nie udało się zalogować. Spróbuj ponownie.");
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        await PrepareAsync(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await PrepareAsync(returnUrl);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            logger.LogInformation("User logged in.");
            return LocalRedirect(ReturnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            ModelState.AddModelError(string.Empty, "Konto jest zablokowane. Spróbuj ponownie później.");
            return Page();
        }

        ModelState.AddModelError(string.Empty,
            "Nie udało się zalogować. Sprawdź adres e-mail, hasło i potwierdzenie konta.");
        return Page();
    }

    private async Task PrepareAsync(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }
}