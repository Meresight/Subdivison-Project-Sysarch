using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.ViewModels
{
    public class EmployeeTimeRecordsViewModel
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<TimeRecord> TimeRecords { get; set; } = new List<TimeRecord>();

        public int TotalDays => TimeRecords
            .Select(tr => tr.CheckInTime.Date)
            .Distinct()
            .Count();

        public double TotalHours => TimeRecords
            .Where(tr => tr.CheckOutTime.HasValue)
            .Sum(tr => tr.Duration);
    }
}
