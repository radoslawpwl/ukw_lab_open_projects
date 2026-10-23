using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Services;

public class SmtpContactEmailSender(
    IOptions<ContactMailOptions> mailOptions,
    IWebHostEnvironment environment) : IContactEmailSender
{
    private readonly ContactMailOptions options = mailOptions.Value;

    private SecureSocketOptions? Security => options.Security switch
    {
        "StartTls" => SecureSocketOptions.StartTls,
        "SslOnConnect" => SecureSocketOptions.SslOnConnect,
        "None" when environment.IsDevelopment() => SecureSocketOptions.None,
        _ => null
    };

    public bool IsTestDelivery => options.TestMode;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Host)
        && options.Port is > 0 and <= 65535
        && Security.HasValue
        && (!IsTestDelivery || environment.IsDevelopment())
        && string.IsNullOrEmpty(options.Username) == string.IsNullOrEmpty(options.Password)
        && MailboxAddress.TryParse(options.SenderAddress, out _)
        && MailboxAddress.TryParse(options.RecipientAddress, out _);

    public async Task SendAsync(ContactViewModel contact, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Contact SMTP is not configured.");
        }

        var topic = typeof(ContactTopic).GetField(contact.Topic?.ToString() ?? string.Empty)
            ?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? "Kontakt";

        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.SenderAddress));
        message.To.Add(MailboxAddress.Parse(options.RecipientAddress));
        message.ReplyTo.Add(new MailboxAddress(string.Empty, contact.Email));
        message.Subject = "RP-bit / " + topic;
        message.Body = new TextPart("plain")
        {
            Text = $"Nadawca: {contact.Name.Trim()}\nAdres e-mail: {contact.Email}\nTemat: {topic}\n\n{contact.Message.Trim()}"
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var client = new SmtpClient { Timeout = 15000 };
        await client.ConnectAsync(options.Host, options.Port, Security!.Value, timeout.Token);
        if (!string.IsNullOrEmpty(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password, timeout.Token);
        }

        await client.SendAsync(message, timeout.Token);
    }
}