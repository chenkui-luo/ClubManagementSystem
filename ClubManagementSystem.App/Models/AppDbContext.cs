using System.Data.Entity;

namespace ClubManagementSystem.App.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=ClubManagementDB")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ClubMember> ClubMembers { get; set; }
        public DbSet<JoinApplication> JoinApplications { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
        public DbSet<FinanceRecord> FinanceRecords { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetBorrowing> AssetBorrowings { get; set; }
        public DbSet<VenueApplication> VenueApplications { get; set; }
        public DbSet<ActivityFile> ActivityFiles { get; set; }
        public DbSet<NoticeFile> NoticeFiles { get; set; }
        public DbSet<ClubFile> ClubFiles { get; set; }
        public DbSet<ClubPendingEdit> ClubPendingEdits { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // ClubMember: composite unique
            modelBuilder.Entity<ClubMember>()
                .HasIndex(cm => new { cm.ClubId, cm.UserId })
                .IsUnique();

            // ActivityRegistration: composite unique
            modelBuilder.Entity<ActivityRegistration>()
                .HasIndex(ar => new { ar.ActivityId, ar.UserId })
                .IsUnique();

            // User: unique StudentId
            modelBuilder.Entity<User>()
                .HasIndex(u => u.StudentId)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
