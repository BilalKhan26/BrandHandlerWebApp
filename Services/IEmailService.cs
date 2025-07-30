using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendMeetingConfirmationAsync(Meeting meeting);
    }
}