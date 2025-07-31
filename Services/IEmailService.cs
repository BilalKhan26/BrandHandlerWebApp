using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string? email, string emailSubject, string emailBody);
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendMeetingConfirmationAsync(Meeting meeting);
    }
}