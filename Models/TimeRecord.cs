// Models/TimeRecord.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class TimeRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = new ApplicationUser();

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public string CheckInMethod { get; set; } = "FaceRecognition"; // Could be "Manual", "FaceRecognition", etc.

        public string? CheckOutMethod { get; set; }

        public string? CheckInLocation { get; set; }

        public string? CheckOutLocation { get; set; }

        public string? Notes { get; set; }

        public bool IsComplete => CheckOutTime.HasValue;

        // Computed duration in hours (not stored in database)
        [NotMapped]
        public double Duration => IsComplete && CheckOutTime.HasValue ?
     (CheckOutTime.Value - CheckInTime).TotalHours : 0;

    }
}

