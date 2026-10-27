using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;
using MimeKit;
using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Services;

public class SmtpContactEmailSender(
    IOptions<ContactMailOptions> mailOptions,
    SmtpMailTransport transport) : IContactEmailSender
{
    private readonly ContactMailOptions options = mailOptions.Value;

    public bool IsTestDelivery => options.TestMode;

    public bool IsConfigured => transport.IsConfigured
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

        await transport.SendAsync(message, cancellationToken);
    }
}