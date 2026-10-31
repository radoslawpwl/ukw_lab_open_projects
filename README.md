# Sklep usług RP-bit

Projekt edukacyjny aplikacji internetowej z katalogiem usług IT.
Pozwala przeglądać ofertę, dodawać usługi do koszyka i wysyłać
zapytania przez formularz kontaktowy.

Projekt uruchomiony pod adresem: services.rp-bit.net

## Funkcje

- Katalog usług z filtrowaniem kategorii i szczegółami oferty.
- Koszyk przechowywany w sesji.
- Rejestracja, logowanie i edycja danych klienta.
- Potwierdzanie konta i odzyskiwanie hasła przez e-mail.
- Uwierzytelnianie dwuskładnikowe.
- Formularz kontaktowy z wysyłaniem wiadomości.
- Polska i angielska wersja językowa.
- Wygląd dostosowany do komputerów i telefonów.

## Technologie

C#, ASP.NET Core MVC (.NET 10), Razor, Bootstrap,
Entity Framework Core, PostgreSQL, ASP.NET Core Identity i MailKit.

Aplikacja i baza danych są uruchamiane przez Docker Compose.
Do sprawdzania wiadomości e-mail lokalnie służy Mailpit.

Projekt obsługuje zapytania o usługi, ale nie realizuje zamówień
ani płatności internetowych.