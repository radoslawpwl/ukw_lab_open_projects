using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SklepInternetowyDocker.Services;

public sealed class SmtpMailTransport(IOptions<ContactMailOptions> mailOptions, IWebHostEnvironment environment)
{
    private readonly ContactMailOptions options = mailOptions.Value;

    private SecureSocketOptions? Security => options.Security switch
    {
        "StartTls" => SecureSocketOptions.StartTls,
        "SslOnConnect" => SecureSocketOptions.SslOnConnect,
        "None" when environment.IsDevelopment() => SecureSocketOptions.None,
        _ => null
    };

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Host)
        && options.Port is > 0 and <= 65535
        && Security.HasValue
        && (!options.TestMode || environment.IsDevelopment())
        && string.IsNullOrEmpty(options.Username) == string.IsNullOrEmpty(options.Password)
        && MailboxAddress.TryParse(options.SenderAddress, out _);

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMTP is not configured securely.");
        }

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