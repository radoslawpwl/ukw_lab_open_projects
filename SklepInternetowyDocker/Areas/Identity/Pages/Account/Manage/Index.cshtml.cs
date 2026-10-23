using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    public string? Username { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        private const string LengthError = "Maksymalna długość to {1} znaków.";

        [Display(Name = "Numer telefonu")]
        [Phone(ErrorMessage = "Podaj poprawny numer telefonu.")]
        [StringLength(32, ErrorMessage = LengthError)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Imię i nazwisko odbiorcy"), StringLength(150, ErrorMessage = LengthError)]
        public string? ShippingRecipient { get; set; }

        [Display(Name = "Ulica, numer budynku i lokalu"), StringLength(200, ErrorMessage = LengthError)]
        public string? ShippingStreet { get; set; }

        [Display(Name = "Kod pocztowy"), StringLength(20, ErrorMessage = LengthError)]
        public string? ShippingPostalCode { get; set; }

        [Display(Name = "Miejscowość"), StringLength(100, ErrorMessage = LengthError)]
        public string? ShippingCity { get; set; }

        [Display(Name = "Kraj"), StringLength(100, ErrorMessage = LengthError)]
        public string? ShippingCountry { get; set; }

        [Display(Name = "Imię i nazwisko lub nazwa firmy"), StringLength(200, ErrorMessage = LengthError)]
        public string? InvoiceRecipient { get; set; }

        [Display(Name = "NIP / numer podatkowy"), StringLength(32, ErrorMessage = LengthError)]
        public string? InvoiceTaxNumber { get; set; }

        [Display(Name = "Adres do faktury taki sam jak adres dostawy")]
        public bool InvoiceUsesShippingAddress { get; set; } = true;

        [Display(Name = "Ulica, numer budynku i lokalu"), StringLength(200, ErrorMessage = LengthError)]
        public string? InvoiceStreet { get; set; }

        [Display(Name = "Kod pocztowy"), StringLength(20, ErrorMessage = LengthError)]
        public string? InvoicePostalCode { get; set; }

        [Display(Name = "Miejscowość"), StringLength(100, ErrorMessage = LengthError)]
        public string? InvoiceCity { get; set; }

        [Display(Name = "Kraj"), StringLength(100, ErrorMessage = LengthError)]
        public string? InvoiceCountry { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        Username = user.UserName;
        Input = new InputModel
        {
            PhoneNumber = user.PhoneNumber,
            ShippingRecipient = user.ShippingRecipient,
            ShippingStreet = user.ShippingStreet,
            ShippingPostalCode = user.ShippingPostalCode,
            ShippingCity = user.ShippingCity,
            ShippingCountry = user.ShippingCountry,
            InvoiceRecipient = user.InvoiceRecipient,
            InvoiceTaxNumber = user.InvoiceTaxNumber,
            InvoiceUsesShippingAddress = user.InvoiceUsesShippingAddress,
            InvoiceStreet = user.InvoiceStreet,
            InvoicePostalCode = user.InvoicePostalCode,
            InvoiceCity = user.InvoiceCity,
            InvoiceCountry = user.InvoiceCountry
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        Username = user.UserName;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var phoneNumber = Normalize(Input.PhoneNumber);
        if (user.PhoneNumber != phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = false;
        }

        user.ShippingRecipient = Normalize(Input.ShippingRecipient);
        user.ShippingStreet = Normalize(Input.ShippingStreet);
        user.ShippingPostalCode = Normalize(Input.ShippingPostalCode);
        user.ShippingCity = Normalize(Input.ShippingCity);
        user.ShippingCountry = Normalize(Input.ShippingCountry);
        user.InvoiceRecipient = Normalize(Input.InvoiceRecipient);
        user.InvoiceTaxNumber = Normalize(Input.InvoiceTaxNumber);
        user.InvoiceUsesShippingAddress = Input.InvoiceUsesShippingAddress;
        user.InvoiceStreet = Input.InvoiceUsesShippingAddress ? user.ShippingStreet : Normalize(Input.InvoiceStreet);
        user.InvoicePostalCode = Input.InvoiceUsesShippingAddress ? user.ShippingPostalCode : Normalize(Input.InvoicePostalCode);
        user.InvoiceCity = Input.InvoiceUsesShippingAddress ? user.ShippingCity : Normalize(Input.InvoiceCity);
        user.InvoiceCountry = Input.InvoiceUsesShippingAddress ? user.ShippingCountry : Normalize(Input.InvoiceCountry);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Dane profilu zostały zapisane.";
        return RedirectToPage();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}