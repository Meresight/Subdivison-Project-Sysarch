using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.ViewModels
{
    public class PayPeriodDetailsViewModel
    {
        public PayPeriod PayPeriod { get; set; } = new PayPeriod();
        public IList<ApplicationUser> StaffMembers { get; set; } = new List<ApplicationUser>();
    }
}
