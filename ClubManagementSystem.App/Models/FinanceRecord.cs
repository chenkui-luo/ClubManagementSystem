using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("FinanceRecords")]
    public class FinanceRecord
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }

        [Required, MaxLength(10)]
        public string Type { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        public int? HandlerId { get; set; }
        public DateTime RecordTime { get; set; } = DateTime.Now;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        [ForeignKey("HandlerId")]
        public virtual User Handler { get; set; }
    }
}
