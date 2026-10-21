using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SklepInternetowyDocker.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa produktu jest wymagana.")]
    [StringLength(150)]
    [Display(Name = "Nazwa")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Opis")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    [Column(TypeName = "numeric(10,2)")]
    [Display(Name = "Cena")]
    public decimal Price { get; set; }

    [Display(Name = "Adres zdjęcia")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Dostępny")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Kategoria")]
    public int CategoryId { get; set; }

    public Category? Category { get; set; }
}