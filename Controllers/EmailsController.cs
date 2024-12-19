using B2S_Api.Models;
using B2S_Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2S_Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailsController(EmailService emailService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
    {
        var success = await emailService.SendEmailAsync(
            emailRequest.RecipientEmail,
            emailRequest.Subject,
            emailRequest.PlainTextContent,
            emailRequest.HtmlContent
        );

        return success
            ? Ok(new { Message = "Email sent successfully!" })
            : StatusCode(500, new { Error = "Failed to send email." });
    }
}