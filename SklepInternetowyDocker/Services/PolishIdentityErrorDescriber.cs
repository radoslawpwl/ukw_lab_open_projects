using Microsoft.AspNetCore.Identity;

namespace SklepInternetowyDocker.Services;

public class PolishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Dane zostały zmienione. Odśwież stronę i spróbuj ponownie.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Kod jest nieprawidłowy lub wygasł.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Hasło jest nieprawidłowe.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "To konto zewnętrzne jest już powiązane z innym użytkownikiem.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), "Adres użyty jako nazwa użytkownika zawiera niedozwolone znaki.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "Podaj poprawny adres e-mail.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), "Konto z tym adresem e-mail już istnieje.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), "Konto z tym adresem e-mail już istnieje.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Hasło musi mieć co najmniej {length} znaków.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"Hasło musi zawierać co najmniej {uniqueChars} różnych znaków.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Hasło musi zawierać co najmniej jeden znak specjalny.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Hasło musi zawierać co najmniej jedną cyfrę.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Hasło musi zawierać co najmniej jedną małą literę.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Hasło musi zawierać co najmniej jedną wielką literę.");

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };
}