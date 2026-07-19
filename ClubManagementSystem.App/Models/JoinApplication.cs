using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("JoinApplications")]
    public class JoinApplication
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }
        public int UserId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "待审批";

        public DateTime ApplyTime { get; set; } = DateTime.Now;
        public DateTime? ProcessTime { get; set; }

        [MaxLength(200)]
        public string Remark { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
