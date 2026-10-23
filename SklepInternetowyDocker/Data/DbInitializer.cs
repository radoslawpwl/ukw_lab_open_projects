using Microsoft.EntityFrameworkCore;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        (string Category, string Name, string Description, decimal Price)[] services =
        [
            ("Doradztwo", "Konsultacja IT & Security",
                "Dwie godziny konsultacji online z przeglądem architektury lub problemu bezpieczeństwa. W pakiecie podsumowanie i lista rekomendowanych działań; bez prac wdrożeniowych.", 1200m),
            ("Bezpieczeństwo", "Audyt bezpieczeństwa IT",
                "Przegląd konfiguracji do 10 serwerów, uprawnień i kopii zapasowych. Raport ryzyk, priorytety napraw i spotkanie podsumowujące. Pakiet nie obejmuje testów penetracyjnych ani certyfikacji zgodności.", 4900m),
            ("Bezpieczeństwo", "Pentest aplikacji webowej",
                "Manualne testy jednej aplikacji, do 30 endpointów, w oparciu o OWASP WSTG. Raport, rekomendacje i jeden retest w ciągu 30 dni. Testy wyłącznie na podstawie pisemnej zgody właściciela systemu.", 8900m),
            ("Bezpieczeństwo", "Testy bezpieczeństwa API",
                "Ocena jednego API, do 20 endpointów: uwierzytelnianie, autoryzacja, walidacja danych i ograniczenia dostępu. Raport oraz jeden retest. Wymagane uzgodnienie zakresu i pisemna zgoda na testy.", 5900m),
            ("Wydajność", "Testy wydajnościowe",
                "Do trzech scenariuszy obciążenia aplikacji HTTP lub API w k6 albo JMeter. Analiza czasów odpowiedzi, błędów i wąskich gardeł oraz raport. Infrastruktura testowa i koszty chmury poza pakietem.", 5900m),
            ("Infrastruktura i AI", "Hardening serwerów Linux",
                "Utwardzenie konfiguracji do trzech serwerów Linux: SSH, firewall, aktualizacje, uprawnienia i logowanie zdarzeń. Dokumentacja zmian i lista zaleceń. Licencje i utrzymanie poza pakietem.", 6900m),
            ("Infrastruktura i AI", "Wdrożenie prywatnego serwera AI",
                "Uruchomienie środowiska LLM na jednym serwerze klienta: Ollama lub vLLM, interfejs użytkownika, kontrola dostępu i instrukcja utrzymania. Sprzęt, chmura, licencje i integracje z systemami poza pakietem.", 14900m),
            ("Infrastruktura i AI", "Asystent AI z wiedzą firmową",
                "Pilotaż rozwiązania RAG dla jednego uzgodnionego źródła dokumentów: indeksowanie, wyszukiwanie i interfejs rozmowy. Odbiór według uzgodnionych scenariuszy. Infrastruktura, API modeli i dalsze utrzymanie poza pakietem.", 19900m)
        ];

        (string Name, string Description, decimal Price)[] demoProducts =
        [
            ("Słuchawki", "Wygodne słuchawki do muzyki.", 149.99m),
            ("Klawiatura", "Klawiatura USB do komputera.", 99m),
            ("Nauka C#", "Książka dla początkujących.", 79.90m),
            ("Klocki", "Kolorowy zestaw 200 klocków.", 119.50m)
        ];

        await using var transaction = await context.Database.BeginTransactionAsync();
        var categories = await context.Categories.ToListAsync();
        var names = services.Select(service => service.Name)
            .Concat(demoProducts.Select(product => product.Name)).ToArray();
        var existingProducts = await context.Products
            .Where(product => names.Contains(product.Name))
            .ToListAsync();

        foreach (var demo in demoProducts)
        {
            foreach (var product in existingProducts.Where(product =>
                         product.Name == demo.Name && product.Description == demo.Description && product.Price == demo.Price))
            {
                product.IsAvailable = false;
            }
        }

        foreach (var service in services)
        {
            if (existingProducts.Any(product => product.Name == service.Name))
            {
                continue;
            }

            var category = categories.FirstOrDefault(category => category.Name == service.Category);
            if (category is null)
            {
                category = new Category { Name = service.Category };
                categories.Add(category);
                context.Categories.Add(category);
            }

            context.Products.Add(new Product
            {
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                ImageUrl = service.Category switch
                {
                    "Bezpieczeństwo" => "/service-code.jpg",
                    "Infrastruktura i AI" => "/service-infrastructure.jpg",
                    _ => "/service-hardware.jpg"
                },
                Category = category
            });
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}