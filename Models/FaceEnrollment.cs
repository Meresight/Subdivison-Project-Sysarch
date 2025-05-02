// Models/FaceEnrollment.cs
using GreenMeadowsPortal.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class FaceEnrollment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public required ApplicationUser User { get; set; }

    [Required]
    public string FaceDataJson { get; set; } = string.Empty;// Stores facial feature data as JSON

    public string? FaceImageUrl { get; set; } // Optional reference to stored image

    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
