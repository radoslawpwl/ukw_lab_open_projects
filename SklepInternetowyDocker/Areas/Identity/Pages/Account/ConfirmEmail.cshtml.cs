using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public bool Succeeded { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId is null || code is null)
        {
            return RedirectToPage("./Login");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return Page();
        }

        var result = await userManager.ConfirmEmailAsync(user, code);
        Succeeded = result.Succeeded;
        return Page();
    }
}