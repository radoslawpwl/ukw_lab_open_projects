using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MimeKit;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Services;

public sealed class SmtpIdentityEmailSender(
    SmtpMailTransport transport,
    IOptions<ContactMailOptions> options,
    IStringLocalizer<SharedResource> text,
    IHttpContextAccessor accessor) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, text["Potwierdź adres e-mail"], confirmationLink);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, text["Zresetuj hasło"], resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, text["Zresetuj hasło"], resetCode);

    private async Task SendAsync(string email, string subject, string value)
    {
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.Value.SenderAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "RP-bit / " + subject;
        message.Body = new TextPart("plain") { Text = $"RP-bit\n{subject}\n\n{WebUtility.HtmlDecode(value)}" };
        await transport.SendAsync(message, accessor.HttpContext?.RequestAborted ?? CancellationToken.None);
    }
}