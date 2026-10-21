using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Data;

namespace SklepInternetowyDocker.Controllers;

public class ProductsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(int? categoryId)
    {
        var products = context.Products
            .Include(product => product.Category)
            .Where(product => product.IsAvailable)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        ViewBag.Categories = await context.Categories
            .OrderBy(category => category.Name)
            .ToListAsync();

        ViewBag.SelectedCategoryId = categoryId;
        return View(await products.OrderBy(product => product.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await context.Products
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsAvailable);

        return product is null ? NotFound() : View(product);
    }
}