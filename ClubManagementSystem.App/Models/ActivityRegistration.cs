using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("ActivityRegistrations")]
    public class ActivityRegistration
    {
        [Key]
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int UserId { get; set; }
        public DateTime? SignInTime { get; set; }
        public bool SignedIn { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "待审批";   // 待审批 / 已通过 / 已拒绝
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
