using System.Net;
using System.Net.Mail;
using BrandHandlerWebApp.Models;
using Microsoft.Extensions.Configuration;

namespace BrandHandlerWebApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            var subject = "Confirm your email address";
            var body = $@"<html>
                        <body>
                            <h2>Email Confirmation</h2>
                            <p>Thank you for registering with Brand Handler. Please confirm your email address by clicking the link below:</p>
                            <p><a href='{confirmationLink}'>Confirm Email</a></p>
                            <p>If you did not register for this account, please ignore this email.</p>
                        </body>
                      </html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendMeetingConfirmationAsync(Meeting meeting)
        {
            var subject = $"Meeting Confirmation: {meeting.Title}";
            var body = $@"<html>
                        <body>
                            <h2>Meeting Confirmation</h2>
                            <p>Your meeting request has been approved!</p>
                            <p><strong>Title:</strong> {meeting.Title}</p>
                            <p><strong>Description:</strong> {meeting.Description}</p>
                            <p><strong>Date and Time:</strong> {meeting.ConfirmedDateTime?.ToString("f")}</p>
                            <p><strong>Meeting Link:</strong> <a href='{meeting.MeetingLink}'>{meeting.MeetingLink}</a></p>
                            <p>Please make sure to join the meeting on time.</p>
                        </body>
                      </html>";

            await SendEmailAsync(meeting.BrandUser.Email, subject, body);
        }

        private async Task SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var smtpServer = smtpSettings["Server"];
                var smtpPort = int.Parse(smtpSettings["Port"]);
                var smtpUsername = smtpSettings["Username"];
                var smtpPassword = smtpSettings["Password"];
                var senderEmail = smtpSettings["SenderEmail"];
                var senderName = smtpSettings["SenderName"];

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {email}: {ex.Message}");
                throw;
            }
        }

        Task IEmailService.SendEmailAsync(string? email, string emailSubject, string emailBody)
        {
            return SendEmailAsync(email, emailSubject, emailBody);
        }
    }
}