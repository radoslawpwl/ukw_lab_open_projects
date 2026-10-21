using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var electronics = new Category { Name = "Elektronika" };
        var books = new Category { Name = "Książki" };
        var toys = new Category { Name = "Zabawki" };

        context.Categories.AddRange(electronics, books, toys);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product
            {
                Name = "Słuchawki",
                Description = "Wygodne słuchawki do muzyki.",
                Price = 149.99m,
                CategoryId = electronics.Id
            },
            new Product
            {
                Name = "Klawiatura",
                Description = "Klawiatura USB do komputera.",
                Price = 99.00m,
                CategoryId = electronics.Id
            },
            new Product
            {
                Name = "Nauka C#",
                Description = "Książka dla początkujących.",
                Price = 79.90m,
                CategoryId = books.Id
            },
            new Product
            {
                Name = "Klocki",
                Description = "Kolorowy zestaw 200 klocków.",
                Price = 119.50m,
                CategoryId = toys.Id
            });

        await context.SaveChangesAsync();
    }
}