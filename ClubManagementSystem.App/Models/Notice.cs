using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("Notices")]
    public class Notice
    {
        [Key]
        public int Id { get; set; }
        public int? ClubId { get; set; }   // null = 全局通知

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required, MaxLength(2000)]
        public string Content { get; set; }

        public int PublisherId { get; set; }
        public DateTime PublishTime { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("PublisherId")]
        public virtual User Publisher { get; set; }

        public virtual ICollection<NoticeFile> Files { get; set; } = new List<NoticeFile>();
    }
}
