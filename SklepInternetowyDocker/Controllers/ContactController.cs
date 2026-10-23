using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SklepInternetowyDocker.Data;
using SklepInternetowyDocker.Extensions;
using SklepInternetowyDocker.Models;
using SklepInternetowyDocker.Services;

namespace SklepInternetowyDocker.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ContactController(
    ApplicationDbContext dbContext,
    IContactEmailSender emailSender,
    ILogger<ContactController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? productId, bool fromCart = false,
        CancellationToken cancellationToken = default)
    {
        var model = new ContactViewModel();
        var currency = CultureInfo.GetCultureInfo("pl-PL");

        if (fromCart)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? [];
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            model.Topic = ContactTopic.Service;
            model.Message = "Dzień dobry, proszę o potwierdzenie zakresu i terminu realizacji usług:\n\n"
                + string.Join("\n", cart.Select(item =>
                    $"{item.ProductName} - {item.Quantity} pakiet(y), {item.Total.ToString("N2", currency)} zł netto"))
                + $"\n\nSuma orientacyjna: {cart.Sum(item => item.Total).ToString("N2", currency)} zł netto.";
        }
        else if (productId.HasValue)
        {
            var product = await dbContext.Products.AsNoTracking()
                .SingleOrDefaultAsync(product => product.Id == productId && product.IsAvailable, cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            model.Topic = ContactTopic.Service;
            model.Message = $"Dzień dobry, proszę o potwierdzenie zakresu i terminu usługi: {product.Name}.\n\n"
                + $"Cena orientacyjna: {product.Price.ToString("N2", currency)} zł netto / pakiet.";
        }

        PrepareView();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("contact")]
    [RequestSizeLimit(131072)]
    public async Task<IActionResult> Index(ContactViewModel model, CancellationToken cancellationToken)
    {
        PrepareView();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!MailboxAddress.TryParse(model.Email, out var replyTo)
            || !string.Equals(replyTo.Address, model.Email, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Email), "Podaj pojedynczy, poprawny adres e-mail.");
            return View(model);
        }

        if (!emailSender.IsConfigured)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(model);
        }

        try
        {
            await emailSender.SendAsync(model, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Contact SMTP failed ({ExceptionType}).", exception.GetType().Name);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty,
                "Nie udało się potwierdzić wysyłki wiadomości. Treść pozostała w formularzu. Spróbuj ponownie później.");
            return View(model);
        }

        TempData["ContactDelivery"] = emailSender.IsTestDelivery ? "test" : "smtp";
        return RedirectToAction(nameof(Sent));
    }

    [HttpGet]
    public IActionResult Sent()
    {
        if (TempData["ContactDelivery"] is not string delivery)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["IsTestDelivery"] = delivery == "test";
        return View();
    }

    private void PrepareView()
    {
        ViewData["CanSend"] = emailSender.IsConfigured;
        ViewData["IsTestDelivery"] = emailSender.IsTestDelivery;
    }
}