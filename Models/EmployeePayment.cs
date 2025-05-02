using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.Models
{
    public class EmployeePayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public required ApplicationUser User { get; set; }

        public int PayPeriodId { get; set; }

        [ForeignKey("PayPeriodId")]
        public required PayPeriod PayPeriod { get; set; }

        public int DaysWorked { get; set; }

        public decimal TotalHours { get; set; }

        public decimal RegularHours { get; set; }

        public decimal OvertimeHours { get; set; }

        public decimal GrossPay { get; set; }

        public decimal Deductions { get; set; }

        public decimal NetPay { get; set; }

        public DateTime ProcessedDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Paid"

        public string? Notes { get; set; }
    }
}
