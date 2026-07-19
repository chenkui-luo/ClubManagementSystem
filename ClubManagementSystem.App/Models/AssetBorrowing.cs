using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("AssetBorrowings")]
    public class AssetBorrowing
    {
        [Key]
        public int Id { get; set; }
        public int AssetId { get; set; }
        public int UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime BorrowTime { get; set; } = DateTime.Now;
        public DateTime? ReturnTime { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "借用中";

        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
