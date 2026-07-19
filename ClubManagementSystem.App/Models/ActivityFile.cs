using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubManagementSystem.App.Models
{
    [Table("ActivityFiles")]
    public class ActivityFile
    {
        [Key]
        public int Id { get; set; }
        public int ActivityId { get; set; }

        [MaxLength(50)]
        public string FileType { get; set; }   // "image" / "document"

        [MaxLength(500)]
        public string FilePath { get; set; }    // 相对路径

        [MaxLength(200)]
        public string OriginalName { get; set; }

        public long FileSize { get; set; }      // 字节

        [NotMapped]
        public double FileSizeKB => FileSize / 1024.0;

        [MaxLength(200)]
        public string Description { get; set; }

        [ForeignKey("ActivityId")]
        public virtual Activity Activity { get; set; }
    }
}
