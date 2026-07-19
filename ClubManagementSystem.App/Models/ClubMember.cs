using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("ClubMembers")]
    public class ClubMember
    {
        [Key]
        public int Id { get; set; }

        public int ClubId { get; set; }
        public int UserId { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "成员";

        [MaxLength(50)]
        public string Position { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
