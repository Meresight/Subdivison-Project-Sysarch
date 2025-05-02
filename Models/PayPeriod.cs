using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.Models
{
    public class PayPeriod
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Open"; // "Open", "Processing", "Closed"
        public string? Notes { get; set; }

        // Add this property to resolve the error  
        public ICollection<EmployeePayment> EmployeePayments { get; set; } = new List<EmployeePayment>(); // Initialize with an empty collection
    }
}
