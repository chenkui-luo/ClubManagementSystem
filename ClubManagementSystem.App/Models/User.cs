using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(20)]
        [Index(IsUnique = true)]
        public string StudentId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Password { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(200)]
        public string Avatar { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "普通成员";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<ClubMember> ClubMembers { get; set; } = new List<ClubMember>();
        public virtual ICollection<JoinApplication> JoinApplications { get; set; } = new List<JoinApplication>();
        public virtual ICollection<ActivityRegistration> ActivityRegistrations { get; set; } = new List<ActivityRegistration>();
    }
}
