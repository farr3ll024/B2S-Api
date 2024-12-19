namespace B2S_Api.Models;

public class EmailRequest(string recipientEmail, string subject, string plainTextContent, string htmlContent)
{
    public string RecipientEmail { get; } = recipientEmail;
    public string Subject { get; } = subject;
    public string PlainTextContent { get; } = plainTextContent;
    public string HtmlContent { get; } = htmlContent;
}