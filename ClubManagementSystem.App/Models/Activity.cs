using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ClubManagementSystem.App.Models
{
    [Table("Activities")]
    public class Activity
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Location { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? MaxPeople { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "报名中";

        [MaxLength(1000)]
        public string Summary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public int ApprovedCount => Registrations?.Count(r => r.Status == "已通过") ?? 0;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<ActivityRegistration> Registrations { get; set; } = new List<ActivityRegistration>();
        public virtual ICollection<ActivityFile> Files { get; set; } = new List<ActivityFile>();
    }
}
