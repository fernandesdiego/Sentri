namespace Sentri.Api.Features.Notifications;

public interface IEmailService
{
    Task SendAlertEmailAsync(string toName, string toEmail, string subject, string htmlContent, CancellationToken ct = default);
}
