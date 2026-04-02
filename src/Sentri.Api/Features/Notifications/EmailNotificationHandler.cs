using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentri.Api.Domain;
using Sentri.Api.Domain.Events;
using Sentri.Api.Infrastructure;

namespace Sentri.Api.Features.Notifications;

public class EmailNotificationHandler(IEmailService emailService, ILogger<EmailNotificationHandler> logger, AppDbContext dbContext) : INotificationHandler<WarningThresholdReachedDomainEvent>
{
    public async Task Handle(WarningThresholdReachedDomainEvent domainEvent, CancellationToken ct = default)
    {
        logger.LogInformation("Preparing to send threshold warning email for Provider '{ProviderName}'", domainEvent.ProviderName);

        var subject = $"Alert: {domainEvent.ProviderName} reached its budget threshold";
        var htmlContent = EmailTemplates.GetWarningThresholdEmailContent(domainEvent);

        var provider = await dbContext.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == domainEvent.ProviderId, ct);

        if (provider?.User is null)
        {
            logger.LogWarning("Owner not found for Provider '{ProviderId}'. Cannot send threshold warning email.", domainEvent.ProviderId);
            return;
        }

        var toName = provider.User.Name;
        var toEmail = provider.User.Email;

        await emailService.SendAlertEmailAsync(toName, toEmail, subject, htmlContent, ct);
    }
}
