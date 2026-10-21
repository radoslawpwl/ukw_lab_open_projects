using System.ComponentModel.DataAnnotations;

namespace SklepInternetowyDocker.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa kategorii jest wymagana.")]
    [StringLength(100)]
    [Display(Name = "Nazwa")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}