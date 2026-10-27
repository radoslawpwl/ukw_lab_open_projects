using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Data;

namespace SklepInternetowyDocker.Controllers;

public class ProductsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(int? categoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 24;
        if (!ModelState.IsValid || page is < 1 or > 10000)
        {
            return BadRequest();
        }

        var products = context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsAvailable)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        ViewBag.Categories = await context.Categories.AsNoTracking()
            .Where(category => context.Products.Any(product => product.CategoryId == category.Id && product.IsAvailable))
            .OrderBy(category => category.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        ViewBag.SelectedCategoryId = categoryId;
        var items = await products.OrderBy(product => product.Name).ThenBy(product => product.Id)
            .Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync(cancellationToken);
        ViewBag.Page = page;
        ViewBag.HasNextPage = items.Count > pageSize;
        return View(items.Take(pageSize).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await context.Products
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsAvailable);

        return product is null ? NotFound() : View(product);
    }
}