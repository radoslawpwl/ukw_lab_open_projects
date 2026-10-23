using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Data;
using SklepInternetowyDocker.Extensions;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Controllers;

public class CartController(ApplicationDbContext context) : Controller
{
    private const string CartKey = "Cart";

    public IActionResult Index()
    {
        return View(GetCart());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsAvailable);

        if (product is null)
        {
            return NotFound();
        }

        var cart = GetCart();
        var item = cart.FirstOrDefault(entry => entry.ProductId == product.Id);

        if (item is null)
        {
            cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }
        else
        {
            item.Quantity++;
        }

        SaveCart(cart);
        TempData["Message"] = "Produkt został dodany do koszyka.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(item => item.ProductId == productId);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartKey);
        return RedirectToAction(nameof(Index));
    }

    private List<CartItem> GetCart()
    {
        return HttpContext.Session.GetObject<List<CartItem>>(CartKey) ?? [];
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetObject(CartKey, cart);
    }
}