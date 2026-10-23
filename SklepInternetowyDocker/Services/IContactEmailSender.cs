using SklepInternetowyDocker.Models;

namespace SklepInternetowyDocker.Services;

public interface IContactEmailSender
{
    bool IsConfigured { get; }
    bool IsTestDelivery { get; }
    Task SendAsync(ContactViewModel message, CancellationToken cancellationToken);
}