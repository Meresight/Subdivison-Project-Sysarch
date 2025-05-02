using GreenMeadowsPortal.Models;
using System.Collections.Generic;

namespace GreenMeadowsPortal.ViewModels
{
    public class AdminDashboardViewModel
    {
        public ApplicationUser AdminUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int ActiveReservations { get; set; }

        // Added properties for notifications and profile
        public int NotificationCount { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Dashboard statistics
        public int PendingRequests { get; set; }
        public int PendingRegistrations { get; set; }
        public decimal OutstandingDues { get; set; }
        public int UpcomingEvents { get; set; }

        // Recent activities for dashboard
        public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();
    }

    // Activity view model for the dashboard
    public class ActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
    }
}