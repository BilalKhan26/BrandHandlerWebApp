namespace BrandHandlerWebApp.Helpers
{
    public static class Constants
    {
        // Roles
        public const string AdminRole = "Admin";
        public const string BrandRole = "User";
        
        // Meeting Status
        public const string MeetingStatusPending = "Pending";
        public const string MeetingStatusApproved = "Approved";
        public const string MeetingStatusRejected = "Rejected";
        
        // Email Templates
        public const string EmailConfirmationSubject = "Confirm your email address";
        public const string MeetingConfirmationSubject = "Meeting Confirmation";
    }
}