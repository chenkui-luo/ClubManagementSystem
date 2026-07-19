using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ClubManagementSystem.App.Models
{
    [Table("Clubs")]
    public class Club
    {
        [Key]
        public int ClubId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int? CategoryId { get; set; }

        [MaxLength(200)]
        public string Logo { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Contact { get; set; }

        public int FounderId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "正常";

        public bool CanRecruit { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 计算属性：当前社长名称（非数据库字段）
        [NotMapped]
        public string PresidentName => ClubMembers?.FirstOrDefault(m => m.Role == "社长")?.User?.Name ?? "未设置";

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("FounderId")]
        public virtual User Founder { get; set; }

        public virtual ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public virtual ICollection<FinanceRecord> FinanceRecords { get; set; } = new List<FinanceRecord>();
        public virtual ICollection<Notice> Notices { get; set; } = new List<Notice>();
        public virtual ICollection<ClubFile> Files { get; set; } = new List<ClubFile>();
        public virtual ICollection<ClubPendingEdit> PendingEdits { get; set; } = new List<ClubPendingEdit>();
    }
}
