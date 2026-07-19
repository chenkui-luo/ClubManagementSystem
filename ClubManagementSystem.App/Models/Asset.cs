using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("Assets")]
    public class Asset
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int Quantity { get; set; }
        public int Available { get; set; }

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }

        public virtual ICollection<AssetBorrowing> Borrowings { get; set; } = new List<AssetBorrowing>();
    }
}
