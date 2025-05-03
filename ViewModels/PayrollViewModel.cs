using GreenMeadowsPortal.Models; // Ensure this namespace is correct

namespace GreenMeadowsPortal.ViewModels
{
    public class PayrollViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        public List<PayPeriod> PayPeriods { get; set; } = new List<PayPeriod>();
    }
}
