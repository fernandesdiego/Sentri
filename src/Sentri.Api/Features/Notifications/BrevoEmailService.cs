using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sentri.Api.Features.Notifications;

public class BrevoEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailService> logger) : IEmailService
{
    public async Task SendAlertEmailAsync(string toName, string toEmail, string subject, string htmlContent, CancellationToken ct = default)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        var senderEmail = configuration["Brevo:SenderEmail"] ?? "contato@diegofernandes.dev";
        var senderName = configuration["Brevo:SenderName"] ?? "Sentri Alerts";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Brevo:ApiKey is not configured. Email to {ToEmail} was not sent.", toEmail);
            return;
        }

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { name = toName, email = toEmail } },
            subject,
            htmlContent
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = content
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Failed to send email via Brevo. Status: {StatusCode}, Details: {Error}", response.StatusCode, errorBody);
            }
            else
            {
                logger.LogInformation("Successfully sent alert email to {ToEmail} via Brevo.", toEmail);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception thrown while sending email via Brevo.");
        }
    }
}
