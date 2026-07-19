using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("ClubFiles")]
    public class ClubFile
    {
        [Key]
        public int Id { get; set; }
        public int ClubId { get; set; }

        [MaxLength(50)]
        public string FileType { get; set; }

        [MaxLength(500)]
        public string FilePath { get; set; }

        [MaxLength(200)]
        public string OriginalName { get; set; }

        public long FileSize { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }

        [NotMapped]
        public double FileSizeKB => FileSize / 1024.0;

        [ForeignKey("ClubId")]
        public virtual Club Club { get; set; }
    }
}
