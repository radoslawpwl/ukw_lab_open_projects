using System.ComponentModel.DataAnnotations;

namespace SklepInternetowyDocker.Models;

public enum ContactTopic
{
    [Display(Name = "Zapytanie o usługę")]
    Service,
    [Display(Name = "Audyty i bezpieczeństwo")]
    Security,
    [Display(Name = "Testy wydajnościowe")]
    Performance,
    [Display(Name = "Infrastruktura i AI")]
    Infrastructure,
    [Display(Name = "Inne pytanie")]
    Other
}

public class ContactViewModel
{
    [Required(ErrorMessage = "Podaj imię i nazwisko lub nazwę firmy.")]
    [StringLength(150, ErrorMessage = "Maksymalna długość to {1} znaków.")]
    [Display(Name = "Imię i nazwisko lub firma")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj adres e-mail do odpowiedzi.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
    [StringLength(254, ErrorMessage = "Maksymalna długość to {1} znaków.")]
    [Display(Name = "Adres e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wybierz temat wiadomości.")]
    [EnumDataType(typeof(ContactTopic), ErrorMessage = "Wybierz poprawny temat.")]
    [Display(Name = "Temat")]
    public ContactTopic? Topic { get; set; }

    [Required(ErrorMessage = "Wpisz treść wiadomości.")]
    [StringLength(10000, MinimumLength = 10, ErrorMessage = "Wiadomość musi mieć od {2} do {1} znaków.")]
    [Display(Name = "Wiadomość")]
    public string Message { get; set; } = string.Empty;
}