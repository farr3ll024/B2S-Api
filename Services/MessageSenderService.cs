using System.Text.Json.Serialization;

namespace B2S_Api.Services;

public class MessageSenderService : BackgroundService
{
    private readonly CosmosDbService _cosmosDbService;
    private readonly EmailService _emailService;
    private readonly ILogger<MessageSenderService> _logger;

    public MessageSenderService(CosmosDbService cosmosDbService, ILogger<MessageSenderService> logger,
        EmailService emailService)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageSenderService has started.");

        // Check if the application is running in debugging mode
        if (IsDebugging())
        {
            _logger.LogInformation("Debugging mode detected. Sending one message immediately...");
            await SendFirstMessageAsync(stoppingToken);
        }

        // Normal execution: Send messages at 8 AM CST daily
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // Calculate the first day of the next month
            var firstOfNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);

            // Set the time to 8 AM CST on the first of the month
            var nextRunTime = TimeZoneInfo.ConvertTime(
                new DateTimeOffset(firstOfNextMonth.AddHours(14)), // 8 AM CST
                TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")
            );
            var delay = nextRunTime - now;

            if (!(delay.TotalMilliseconds > 0)) continue;

            _logger.LogInformation($"Waiting until {nextRunTime} to send the next message...");
            await Task.Delay(delay, stoppingToken);
            await SendFirstMessageAsync(stoppingToken);
        }
    }

    private async Task SendFirstMessageAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        try
        {
            // Retrieve the first message from Cosmos DB
            const string query =
                "SELECT * FROM c WHERE NOT IS_NULL(c.id) AND NOT IS_NULL(c.partitionKey) AND IS_NULL(c.sent)";
            var messages = await _cosmosDbService.GetItemsAsync<Message>(query);

            var firstMessage = messages.FirstOrDefault();

            if (firstMessage != null)
            {
                _logger.LogInformation(
                    $"Sending message to: {firstMessage.RecipientEmail}, Subject: {firstMessage.Subject}");

                await _emailService.SendEmailAsync(firstMessage.RecipientEmail, firstMessage.Subject,
                    firstMessage.PlainTextContent, firstMessage.HtmlContent);

                var updatedMessage = new
                {
                    id = firstMessage.Id,
                    key = firstMessage.Key,
                    partitionKey = firstMessage.PartitionKey,
                    recipientEmail = firstMessage.RecipientEmail,
                    subject = firstMessage.Subject,
                    plainTextContent = firstMessage.PlainTextContent,
                    htmlContent = firstMessage.HtmlContent,
                    sent = DateTime.Now
                };

                await _cosmosDbService.UpdateItemAsync(firstMessage.Id, updatedMessage, firstMessage.PartitionKey);
                // _logger.LogInformation("Message sent and marked as sent in the database.");
            }
            else
            {
                _logger.LogInformation("No messages found to send.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while sending the message: {ex.Message}");
        }
    }

    private bool IsDebugging()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}

// Model representing the message structure
public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;

    [JsonPropertyName("partitionKey")] public string PartitionKey { get; set; } = string.Empty;

    [JsonPropertyName("recipientEmail")] public string RecipientEmail { get; set; } = string.Empty;

    [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("plainTextContent")] public string PlainTextContent { get; set; } = string.Empty;

    [JsonPropertyName("htmlContent")] public string HtmlContent { get; set; } = string.Empty;

    [JsonPropertyName("sent")] public DateTime? Sent { get; set; } = null;
}