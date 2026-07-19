using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("ClubPendingEdits")]
    public class ClubPendingEdit
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }
        public int SubmittedBy { get; set; }

        [MaxLength(100)]
        public string NewName { get; set; }

        public int? CategoryId { get; set; }

        [MaxLength(500)]
        public string NewDescription { get; set; }

        [MaxLength(100)]
        public string NewContact { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "待审批";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("SubmittedBy")]
        public virtual User Submitter { get; set; }
    }
}
