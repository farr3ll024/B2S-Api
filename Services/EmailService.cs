using Azure;
using Azure.Communication.Email;

namespace B2S_Api.Services;

public class EmailService
{
    private readonly EmailClient _emailClient;
    private readonly string? _senderEmail;

    public EmailService(IConfiguration configuration)
    {
        var connectionString = configuration["EmailServiceConnectionString"];
        _senderEmail = configuration["EmailServiceSenderEmail"];
        _emailClient = new EmailClient(connectionString);
    }

    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string plainTextContent,
        string htmlContent)
    {
        try
        {
            var emailMessage = new EmailMessage(
                _senderEmail,
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                },
                recipients: new EmailRecipients(new List<EmailAddress> { new(recipientEmail) })
            );

            var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);

            return emailSendOperation.HasCompleted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }
}