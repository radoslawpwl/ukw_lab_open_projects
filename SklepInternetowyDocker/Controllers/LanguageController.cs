using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SklepInternetowyDocker.Controllers;

public class LanguageController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Change(string culture, string? returnUrl)
    {
        var selectedCulture = culture switch
        {
            "pl" => "pl-PL",
            "en" => "en-GB",
            _ => null
        };

        if (selectedCulture is null)
        {
            return BadRequest();
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = Request.PathBase.HasValue ? Request.PathBase.Value : "/"
            });

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction("Index", "Products");
    }
}