using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("VenueApplications")]
    public class VenueApplication
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }

        [Required, MaxLength(100)]
        public string Venue { get; set; }

        [MaxLength(200)]
        public string Purpose { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "待审批";

        public int ApplicantId { get; set; }
        public DateTime ApplyTime { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("ApplicantId")]
        public virtual User Applicant { get; set; }
    }
}
