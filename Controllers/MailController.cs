using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Net.Http.Headers;

namespace Scheduler.Controllers;


[Route("/mail")]
public class MailController : ControllerBase
{
    private readonly EmailConfig? emailConfig;

    public MailController(IConfiguration configuration)
    {
        emailConfig = configuration.GetRequiredSection(nameof(EmailConfig)).Get<EmailConfig>();
    }

    [Route("/contact")]
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
    {
        if (emailConfig == null 
            || string.IsNullOrEmpty(emailConfig.SmtpServer) 
            || string.IsNullOrEmpty(emailConfig.UserName) 
            || string.IsNullOrEmpty(emailConfig.Password)
            || string.IsNullOrEmpty(emailConfig.Receiver)
            )
        {
            return Problem("No email configuration");
        }

        var fullMessage = messageDto.Message;

        if (!string.IsNullOrEmpty(messageDto.Name)) {
            fullMessage += $"\nName:{messageDto.Name}";
        }

        if (!string.IsNullOrEmpty(messageDto.Email))
        {
            fullMessage += $"\nEmail:{messageDto.Email}";
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(emailConfig.UserName, emailConfig.UserName));
        message.To.Add(new MailboxAddress("", emailConfig.Receiver));
        message.Subject = "Planevent.me Feedback";
        message.Body = new TextPart("plain")
        {
            Text = fullMessage
        };

        using (var client = new SmtpClient()) {
            await client.ConnectAsync(emailConfig.SmtpServer, 465, true);

            if (!string.IsNullOrEmpty(emailConfig.UserName) && !string.IsNullOrEmpty(emailConfig.Password))
            {
                await client.AuthenticateAsync(emailConfig.UserName, emailConfig.Password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(false);
        }

        return Ok();
    }
}

public record MessageDto(string? Name, string? Email, string Message);
public record EmailConfig(string SmtpServer, string? UserName, string? Password, string Receiver);