using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class DocumentModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Type { get; set; }

        [Required]
        public required string FilePath { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        public required string UserId { get; set; }

        [ForeignKey("UserId")]
        public required ApplicationUser User { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; } // Made nullable to fix CS8618  
    }
}